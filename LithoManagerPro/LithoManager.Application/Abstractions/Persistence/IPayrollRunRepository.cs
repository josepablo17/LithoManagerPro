using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Payroll.Runs;
using LithoManager.Application.Features.Payroll.Runs.Calculation;

namespace LithoManager.Application.Abstractions.Persistence;

public interface IPayrollRunRepository
{
    Task<IReadOnlyList<PayrollRunData>> GetPayrollRunsAsync(
        int? periodYear,
        string? runType,
        CancellationToken cancellationToken);

    Task<PayrollRunData?> GetPayrollRunByIdAsync(
        int payrollRunId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PayrollRunEmployeeData>> GetPayrollRunEmployeesAsync(
        int payrollRunId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<PayrollRunLineData>> GetPayrollRunLinesAsync(
        int payrollRunId,
        int? employeeId,
        CancellationToken cancellationToken);

    Task<PayrollCalculationInputs> GetPayrollCalculationInputsAsync(
        PayrollRunData payrollRun,
        CancellationToken cancellationToken);

    Task<AguinaldoCalculationInputs> GetAguinaldoCalculationInputsAsync(
        PayrollRunData payrollRun,
        CancellationToken cancellationToken);

    Task<PayrollRunData> CreatePayrollRunAsync(
        string runType,
        int periodYear,
        int? periodMonth,
        DateTime? paymentDate,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<PayrollRunData> SavePayrollRunResultsAsync(
        int payrollRunId,
        byte[] expectedRowVersion,
        string resultsJson,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<PayrollRunData> ClosePayrollRunAsync(
        int payrollRunId,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);
}
