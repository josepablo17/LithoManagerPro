using System.Text.Json;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Payroll;
using LithoManager.Application.Features.Payroll.Runs;
using LithoManager.Application.Features.Payroll.Runs.Calculation;
using LithoManager.UnitTests.TestDoubles.Persistence;
using Xunit;
using static LithoManager.UnitTests.Features.Payroll.Runs.PayrollTestData;

namespace LithoManager.UnitTests.Features.Payroll.Runs;

public sealed class PayrollRunServiceTests
{
    private static readonly byte[] RowVersion = [0, 0, 0, 0, 0, 0, 0, 1];

    private static readonly AuthenticationRequestContext RequestContext =
        new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "127.0.0.1",
            "tests",
            "/api/payroll/runs/1/calculate");

    [Fact]
    public async Task CalculateAsync_RegularRun_SavesResultsWithTheJsonShapeTheProcedureReads()
    {
        FakePayrollRunRepository repository = new()
        {
            Run = CreateRun(PayrollRunTypes.Regular, PayrollRunStatuses.Open),
            RegularInputs = Inputs(
                new DateTime(2026, 11, 1),
                [Employee(baseSalary: 600_000m)]),
        };

        PayrollRunResult result =
            await new PayrollRunService(repository).CalculateAsync(
                CalculateCommand(),
                CancellationToken.None);

        Assert.True(result.IsSuccessful);
        Assert.True(repository.RegularInputsRequested);
        Assert.False(repository.AguinaldoInputsRequested);

        // Payroll.SavePayrollRunResults reads these exact property names
        // with OPENJSON.
        using JsonDocument json = JsonDocument.Parse(repository.SavedResultsJson!);
        JsonElement employee = Assert.Single(json.RootElement.EnumerateArray());

        foreach (string property in new[]
                 {
                     "employeeId", "identificationNumber", "employeeName",
                     "departmentName", "jobTitle", "monthlyBaseSalary", "paidDays",
                     "grossAmount", "employeeContributionsAmount", "incomeTaxAmount",
                     "netAmount", "employerContributionsAmount", "lines",
                 })
        {
            Assert.True(
                employee.TryGetProperty(property, out _),
                $"Missing property '{property}'.");
        }

        JsonElement line = employee.GetProperty("lines").EnumerateArray().First();

        foreach (string property in new[]
                 {
                     "payrollConceptId", "referenceCode", "description", "quantity",
                     "rate", "baseAmount", "amount", "sortOrder",
                 })
        {
            Assert.True(
                line.TryGetProperty(property, out _),
                $"Missing line property '{property}'.");
        }

        Assert.Equal(600_000m, employee.GetProperty("grossAmount").GetDecimal());
    }

    [Fact]
    public async Task CalculateAsync_AguinaldoRun_UsesTheAguinaldoCalculator()
    {
        FakePayrollRunRepository repository = new()
        {
            Run = CreateRun(PayrollRunTypes.Aguinaldo, PayrollRunStatuses.Open),
            AguinaldoInputs = new AguinaldoCalculationInputs
            {
                WindowStartDate = new DateTime(2025, 12, 1),
                WindowEndDate = new DateTime(2026, 11, 30),
                Rule = new AguinaldoRule { AguinaldoRuleCode = "CR", Divisor = 12, IncludesOrdinarySalary = true },
                Concepts = Concepts,
            },
        };

        PayrollRunResult result =
            await new PayrollRunService(repository).CalculateAsync(
                CalculateCommand(),
                CancellationToken.None);

        Assert.True(result.IsSuccessful);
        Assert.True(repository.AguinaldoInputsRequested);
        Assert.Contains(result.Warnings, w => w.Contains("No hay planillas regulares cerradas"));
    }

    [Fact]
    public async Task CalculateAsync_ClosedRun_ReturnsInvalidStateWithoutCalculating()
    {
        FakePayrollRunRepository repository = new()
        {
            Run = CreateRun(PayrollRunTypes.Regular, PayrollRunStatuses.Closed),
        };

        PayrollRunResult result =
            await new PayrollRunService(repository).CalculateAsync(
                CalculateCommand(),
                CancellationToken.None);

        Assert.Equal(PayrollErrorCode.InvalidState, result.ErrorCode);
        Assert.False(repository.RegularInputsRequested);
        Assert.Null(repository.SavedResultsJson);
    }

    [Fact]
    public async Task CalculateAsync_UnknownRun_ReturnsNotFound()
    {
        PayrollRunResult result =
            await new PayrollRunService(new FakePayrollRunRepository())
                .CalculateAsync(CalculateCommand(), CancellationToken.None);

        Assert.Equal(PayrollErrorCode.PayrollRunNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task CalculateAsync_MissingConfiguration_ReturnsConfigurationNotFound()
    {
        FakePayrollRunRepository repository = new()
        {
            Run = CreateRun(PayrollRunTypes.Regular, PayrollRunStatuses.Calculated),
            RegularInputs = Inputs(
                new DateTime(2026, 11, 1),
                [Employee()],
                concepts: []),
        };

        PayrollRunResult result =
            await new PayrollRunService(repository).CalculateAsync(
                CalculateCommand(),
                CancellationToken.None);

        Assert.Equal(PayrollErrorCode.ConfigurationNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task CalculateAsync_ConcurrentChange_ReturnsConcurrencyConflict()
    {
        FakePayrollRunRepository repository = new()
        {
            Run = CreateRun(PayrollRunTypes.Regular, PayrollRunStatuses.Open),
            RegularInputs = Inputs(new DateTime(2026, 11, 1), [Employee()]),
            SaveException = new PayrollPersistenceException(
                PayrollErrorCode.ConcurrencyConflict,
                "changed",
                new InvalidOperationException()),
        };

        PayrollRunResult result =
            await new PayrollRunService(repository).CalculateAsync(
                CalculateCommand(),
                CancellationToken.None);

        Assert.Equal(PayrollErrorCode.ConcurrencyConflict, result.ErrorCode);
    }

    [Theory]
    [InlineData("Regular", 2026, null)]
    [InlineData("Regular", 2026, 13)]
    [InlineData("Bonus", 2026, 1)]
    [InlineData("Regular", 1999, 1)]
    public async Task CreateAsync_InvalidRequest_IsRejectedBeforeThePersistenceLayer(
        string runType,
        int year,
        int? month)
    {
        // The fake throws if CreatePayrollRunAsync is reached.
        PayrollRunResult result =
            await new PayrollRunService(new FakePayrollRunRepository())
                .CreateAsync(
                    new CreatePayrollRunCommand(runType, year, month, null, 1, RequestContext),
                    CancellationToken.None);

        Assert.Equal(PayrollErrorCode.InvalidRequest, result.ErrorCode);
    }

    private static CalculatePayrollRunCommand CalculateCommand()
    {
        return new CalculatePayrollRunCommand(1, RowVersion, 1, RequestContext);
    }

    private static PayrollRunData CreateRun(string runType, string status)
    {
        return new PayrollRunData
        {
            PayrollRunId = 1,
            RunType = runType,
            PeriodYear = 2026,
            PeriodMonth = runType == PayrollRunTypes.Aguinaldo ? 12 : 11,
            PeriodStartDate = runType == PayrollRunTypes.Aguinaldo
                ? new DateTime(2025, 12, 1)
                : new DateTime(2026, 11, 1),
            PeriodEndDate = new DateTime(2026, 11, 30),
            PaymentDate = new DateTime(2026, 11, 30),
            Status = status,
            RowVersion = RowVersion,
        };
    }
}
