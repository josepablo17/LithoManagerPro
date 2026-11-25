using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Payroll;
using LithoManager.Application.Features.Payroll.Runs;
using LithoManager.Application.Features.Payroll.Runs.Calculation;

namespace LithoManager.UnitTests.TestDoubles.Persistence;

internal sealed class FakePayrollRunRepository : IPayrollRunRepository
{
    public PayrollRunData? Run { get; set; }

    public PayrollCalculationInputs RegularInputs { get; set; } = new();

    public AguinaldoCalculationInputs AguinaldoInputs { get; set; } = new();

    public PayrollPersistenceException? SaveException { get; set; }

    public string? SavedResultsJson { get; private set; }

    public bool RegularInputsRequested { get; private set; }

    public bool AguinaldoInputsRequested { get; private set; }

    public Task<IReadOnlyList<PayrollRunData>> GetPayrollRunsAsync(
        int? periodYear,
        string? runType,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PayrollRunData> runs = Run is null ? [] : [Run];
        return Task.FromResult(runs);
    }

    public Task<PayrollRunData?> GetPayrollRunByIdAsync(
        int payrollRunId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Run?.PayrollRunId == payrollRunId ? Run : null);
    }

    public Task<IReadOnlyList<PayrollRunEmployeeData>> GetPayrollRunEmployeesAsync(
        int payrollRunId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PayrollRunEmployeeData>>([]);
    }

    public Task<IReadOnlyList<PayrollRunLineData>> GetPayrollRunLinesAsync(
        int payrollRunId,
        int? employeeId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PayrollRunLineData>>([]);
    }

    public Task<PayrollCalculationInputs> GetPayrollCalculationInputsAsync(
        PayrollRunData payrollRun,
        CancellationToken cancellationToken)
    {
        RegularInputsRequested = true;
        return Task.FromResult(RegularInputs);
    }

    public Task<AguinaldoCalculationInputs> GetAguinaldoCalculationInputsAsync(
        PayrollRunData payrollRun,
        CancellationToken cancellationToken)
    {
        AguinaldoInputsRequested = true;
        return Task.FromResult(AguinaldoInputs);
    }

    public Task<PayrollRunData> CreatePayrollRunAsync(
        string runType,
        int periodYear,
        int? periodMonth,
        DateTime? paymentDate,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<PayrollRunData> SavePayrollRunResultsAsync(
        int payrollRunId,
        byte[] expectedRowVersion,
        string resultsJson,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        if (SaveException is not null)
        {
            throw SaveException;
        }

        SavedResultsJson = resultsJson;
        return Task.FromResult(Run!);
    }

    public Task<PayrollRunData> ClosePayrollRunAsync(
        int payrollRunId,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}
