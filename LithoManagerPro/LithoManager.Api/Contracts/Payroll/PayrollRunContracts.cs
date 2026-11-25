using LithoManager.Application.Features.Payroll.Runs;

namespace LithoManager.Api.Contracts.Payroll;

public sealed class CreatePayrollRunRequest
{
    public string? RunType { get; init; }

    public int? PeriodYear { get; init; }

    public int? PeriodMonth { get; init; }

    public DateTime? PaymentDate { get; init; }
}

public sealed class PayrollRunMutationRequest
{
    public string? ExpectedRowVersion { get; init; }
}

public sealed record PayrollRunResponse(
    int PayrollRunId,
    string RunType,
    int PeriodYear,
    int PeriodMonth,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    DateOnly PaymentDate,
    string Status,
    int EmployeeCount,
    decimal TotalGrossAmount,
    decimal TotalEmployeeContributionsAmount,
    decimal TotalIncomeTaxAmount,
    decimal TotalNetAmount,
    decimal TotalEmployerContributionsAmount,
    DateTime? CalculatedAtUtc,
    string? CalculatedByEmailAddress,
    DateTime? ClosedAtUtc,
    string? ClosedByEmailAddress,
    DateTime CreatedAtUtc,
    string RowVersion);

public sealed record PayrollRunEmployeeResponse(
    int EmployeeId,
    string IdentificationNumber,
    string EmployeeName,
    string? DepartmentName,
    string? JobTitle,
    decimal MonthlyBaseSalary,
    decimal PaidDays,
    decimal GrossAmount,
    decimal EmployeeContributionsAmount,
    decimal IncomeTaxAmount,
    decimal NetAmount,
    decimal EmployerContributionsAmount);

public sealed record PayrollRunDetailResponse(
    PayrollRunResponse PayrollRun,
    IReadOnlyList<PayrollRunEmployeeResponse> Employees,
    IReadOnlyList<string> Warnings);

public sealed record PayrollRunLineResponse(
    int PayrollRunLineId,
    string PayrollConceptCode,
    string ConceptKind,
    string? ReferenceCode,
    string Description,
    decimal? Quantity,
    decimal? Rate,
    decimal? BaseAmount,
    decimal Amount,
    int SortOrder);

internal static class PayrollRunResponseMapper
{
    public static PayrollRunResponse Map(PayrollRunData run)
    {
        ArgumentNullException.ThrowIfNull(run);

        return new PayrollRunResponse(
            run.PayrollRunId,
            run.RunType,
            run.PeriodYear,
            run.PeriodMonth,
            DateOnly.FromDateTime(run.PeriodStartDate),
            DateOnly.FromDateTime(run.PeriodEndDate),
            DateOnly.FromDateTime(run.PaymentDate),
            run.Status,
            run.EmployeeCount,
            run.TotalGrossAmount,
            run.TotalEmployeeContributionsAmount,
            run.TotalIncomeTaxAmount,
            run.TotalNetAmount,
            run.TotalEmployerContributionsAmount,
            run.CalculatedAtUtc,
            run.CalculatedByEmailAddress,
            run.ClosedAtUtc,
            run.ClosedByEmailAddress,
            run.CreatedAtUtc,
            Convert.ToBase64String(run.RowVersion));
    }

    public static PayrollRunDetailResponse MapDetail(PayrollRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new PayrollRunDetailResponse(
            Map(result.PayrollRun!),
            result.Employees.Select(Map).ToList(),
            result.Warnings);
    }

    public static PayrollRunEmployeeResponse Map(PayrollRunEmployeeData employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        return new PayrollRunEmployeeResponse(
            employee.EmployeeId,
            employee.IdentificationNumber,
            employee.EmployeeName,
            employee.DepartmentName,
            employee.JobTitle,
            employee.MonthlyBaseSalary,
            employee.PaidDays,
            employee.GrossAmount,
            employee.EmployeeContributionsAmount,
            employee.IncomeTaxAmount,
            employee.NetAmount,
            employee.EmployerContributionsAmount);
    }

    public static PayrollRunLineResponse Map(PayrollRunLineData line)
    {
        ArgumentNullException.ThrowIfNull(line);

        return new PayrollRunLineResponse(
            line.PayrollRunLineId,
            line.PayrollConceptCode,
            line.ConceptKind,
            line.ReferenceCode,
            line.Description,
            line.Quantity,
            line.Rate,
            line.BaseAmount,
            line.Amount,
            line.SortOrder);
    }
}
