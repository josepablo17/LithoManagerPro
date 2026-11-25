using LithoManager.Application.Features.Authentication.Login;

namespace LithoManager.Application.Features.Payroll.Runs;

public sealed record CreatePayrollRunCommand(
    string? RunType,
    int PeriodYear,
    int? PeriodMonth,
    DateTime? PaymentDate,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);

public sealed record CalculatePayrollRunCommand(
    int PayrollRunId,
    byte[]? ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);

public sealed record ClosePayrollRunCommand(
    int PayrollRunId,
    byte[]? ExpectedRowVersion,
    int ActorUserId,
    AuthenticationRequestContext RequestContext);

public sealed record PayrollRunResult(
    bool IsSuccessful,
    PayrollErrorCode ErrorCode,
    PayrollRunData? PayrollRun,
    IReadOnlyList<PayrollRunEmployeeData> Employees,
    IReadOnlyList<string> Warnings)
{
    public static PayrollRunResult Success(
        PayrollRunData payrollRun,
        IReadOnlyList<PayrollRunEmployeeData>? employees = null,
        IReadOnlyList<string>? warnings = null)
    {
        ArgumentNullException.ThrowIfNull(payrollRun);

        return new PayrollRunResult(
            IsSuccessful: true,
            ErrorCode: PayrollErrorCode.None,
            PayrollRun: payrollRun,
            Employees: employees ?? [],
            Warnings: warnings ?? []);
    }

    public static PayrollRunResult Failure(
        PayrollErrorCode errorCode)
    {
        if (errorCode == PayrollErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new PayrollRunResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            PayrollRun: null,
            Employees: [],
            Warnings: []);
    }
}

public interface IPayrollRunService
{
    Task<PayrollItemsResult<PayrollRunData>> GetRunsAsync(
        int? periodYear,
        string? runType,
        CancellationToken cancellationToken);

    /// <summary>Returns the run with its employees.</summary>
    Task<PayrollRunResult> GetRunAsync(
        int payrollRunId,
        CancellationToken cancellationToken);

    Task<PayrollItemsResult<PayrollRunLineData>> GetEmployeeLinesAsync(
        int payrollRunId,
        int employeeId,
        CancellationToken cancellationToken);

    Task<PayrollRunResult> CreateAsync(
        CreatePayrollRunCommand command,
        CancellationToken cancellationToken);

    /// <summary>
    /// Runs the payroll or aguinaldo calculator and replaces the run's
    /// results. Returns the updated run, its employees and the warnings.
    /// </summary>
    Task<PayrollRunResult> CalculateAsync(
        CalculatePayrollRunCommand command,
        CancellationToken cancellationToken);

    Task<PayrollRunResult> CloseAsync(
        ClosePayrollRunCommand command,
        CancellationToken cancellationToken);
}
