using System.Text.Json;
using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Payroll.Runs.Calculation;

namespace LithoManager.Application.Features.Payroll.Runs;

/// <summary>
/// Orchestrates the payroll run lifecycle: Open -> Calculated -> Closed.
/// The calculation itself is done by the pure calculators; this service
/// loads their inputs and persists their output.
/// </summary>
public sealed class PayrollRunService : IPayrollRunService
{
    private static readonly JsonSerializerOptions ResultsJsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IPayrollRunRepository _repository;

    public PayrollRunService(IPayrollRunRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        _repository = repository;
    }

    public async Task<PayrollItemsResult<PayrollRunData>> GetRunsAsync(
        int? periodYear,
        string? runType,
        CancellationToken cancellationToken)
    {
        string? normalizedRunType = NormalizeRunType(runType);

        if ((periodYear is not null && !IsValidYear(periodYear.Value))
            || (runType is not null && normalizedRunType is null))
        {
            return PayrollItemsResult<PayrollRunData>.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        IReadOnlyList<PayrollRunData> runs =
            await _repository.GetPayrollRunsAsync(
                periodYear,
                normalizedRunType,
                cancellationToken);

        return PayrollItemsResult<PayrollRunData>.Success(runs);
    }

    public async Task<PayrollRunResult> GetRunAsync(
        int payrollRunId,
        CancellationToken cancellationToken)
    {
        if (payrollRunId <= 0)
        {
            return PayrollRunResult.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        PayrollRunData? run =
            await _repository.GetPayrollRunByIdAsync(
                payrollRunId,
                cancellationToken);

        if (run is null)
        {
            return PayrollRunResult.Failure(
                PayrollErrorCode.PayrollRunNotFound);
        }

        IReadOnlyList<PayrollRunEmployeeData> employees =
            await _repository.GetPayrollRunEmployeesAsync(
                payrollRunId,
                cancellationToken);

        return PayrollRunResult.Success(run, employees);
    }

    public async Task<PayrollItemsResult<PayrollRunLineData>> GetEmployeeLinesAsync(
        int payrollRunId,
        int employeeId,
        CancellationToken cancellationToken)
    {
        if (payrollRunId <= 0 || employeeId <= 0)
        {
            return PayrollItemsResult<PayrollRunLineData>.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        PayrollRunData? run =
            await _repository.GetPayrollRunByIdAsync(
                payrollRunId,
                cancellationToken);

        if (run is null)
        {
            return PayrollItemsResult<PayrollRunLineData>.Failure(
                PayrollErrorCode.PayrollRunNotFound);
        }

        IReadOnlyList<PayrollRunLineData> lines =
            await _repository.GetPayrollRunLinesAsync(
                payrollRunId,
                employeeId,
                cancellationToken);

        return PayrollItemsResult<PayrollRunLineData>.Success(lines);
    }

    public async Task<PayrollRunResult> CreateAsync(
        CreatePayrollRunCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.RequestContext);

        string? runType = NormalizeRunType(command.RunType);

        bool isValid =
            runType is not null
            && IsValidYear(command.PeriodYear)
            && (runType == PayrollRunTypes.Aguinaldo
                || command.PeriodMonth is >= 1 and <= 12)
            && PayrollValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext);

        if (!isValid)
        {
            return PayrollRunResult.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        try
        {
            PayrollRunData run =
                await _repository.CreatePayrollRunAsync(
                    runType!,
                    command.PeriodYear,
                    runType == PayrollRunTypes.Regular
                        ? command.PeriodMonth
                        : null,
                    command.PaymentDate?.Date,
                    command.ActorUserId,
                    command.RequestContext,
                    cancellationToken);

            return PayrollRunResult.Success(run);
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollRunResult.Failure(exception.ErrorCode);
        }
    }

    public async Task<PayrollRunResult> CalculateAsync(
        CalculatePayrollRunCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.RequestContext);

        if (command.PayrollRunId <= 0
            || command.ExpectedRowVersion is not { Length: 8 }
            || !PayrollValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext))
        {
            return PayrollRunResult.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        PayrollRunData? run =
            await _repository.GetPayrollRunByIdAsync(
                command.PayrollRunId,
                cancellationToken);

        if (run is null)
        {
            return PayrollRunResult.Failure(
                PayrollErrorCode.PayrollRunNotFound);
        }

        if (run.Status == PayrollRunStatuses.Closed)
        {
            return PayrollRunResult.Failure(
                PayrollErrorCode.InvalidState);
        }

        PayrollCalculationResult calculation;

        try
        {
            calculation = run.RunType == PayrollRunTypes.Aguinaldo
                ? AguinaldoCalculator.Calculate(
                    await _repository.GetAguinaldoCalculationInputsAsync(
                        run,
                        cancellationToken))
                : PayrollCalculator.Calculate(
                    await _repository.GetPayrollCalculationInputsAsync(
                        run,
                        cancellationToken));
        }
        catch (PayrollCalculationException)
        {
            return PayrollRunResult.Failure(
                PayrollErrorCode.ConfigurationNotFound);
        }

        string resultsJson = JsonSerializer.Serialize(
            calculation.Employees,
            ResultsJsonOptions);

        try
        {
            PayrollRunData updatedRun =
                await _repository.SavePayrollRunResultsAsync(
                    run.PayrollRunId,
                    command.ExpectedRowVersion,
                    resultsJson,
                    command.ActorUserId,
                    command.RequestContext,
                    cancellationToken);

            IReadOnlyList<PayrollRunEmployeeData> employees =
                await _repository.GetPayrollRunEmployeesAsync(
                    run.PayrollRunId,
                    cancellationToken);

            return PayrollRunResult.Success(
                updatedRun,
                employees,
                calculation.Warnings);
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollRunResult.Failure(exception.ErrorCode);
        }
    }

    public async Task<PayrollRunResult> CloseAsync(
        ClosePayrollRunCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.RequestContext);

        if (command.PayrollRunId <= 0
            || command.ExpectedRowVersion is not { Length: 8 }
            || !PayrollValidation.IsValidMutationRequest(
                command.ActorUserId,
                command.RequestContext))
        {
            return PayrollRunResult.Failure(
                PayrollErrorCode.InvalidRequest);
        }

        try
        {
            PayrollRunData run =
                await _repository.ClosePayrollRunAsync(
                    command.PayrollRunId,
                    command.ExpectedRowVersion,
                    command.ActorUserId,
                    command.RequestContext,
                    cancellationToken);

            IReadOnlyList<PayrollRunEmployeeData> employees =
                await _repository.GetPayrollRunEmployeesAsync(
                    run.PayrollRunId,
                    cancellationToken);

            return PayrollRunResult.Success(run, employees);
        }
        catch (PayrollPersistenceException exception)
        {
            return PayrollRunResult.Failure(exception.ErrorCode);
        }
    }

    private static string? NormalizeRunType(string? runType)
    {
        string? trimmed = runType?.Trim();

        if (string.Equals(trimmed, PayrollRunTypes.Regular, StringComparison.OrdinalIgnoreCase))
        {
            return PayrollRunTypes.Regular;
        }

        if (string.Equals(trimmed, PayrollRunTypes.Aguinaldo, StringComparison.OrdinalIgnoreCase))
        {
            return PayrollRunTypes.Aguinaldo;
        }

        return null;
    }

    private static bool IsValidYear(int year)
    {
        return year is >= 2000 and <= 2100;
    }
}
