namespace LithoManager.Application.Features.Payroll.Runs;

public static class PayrollRunTypes
{
    public const string Regular = "Regular";
    public const string Aguinaldo = "Aguinaldo";
}

public static class PayrollRunStatuses
{
    public const string Open = "Open";
    public const string Calculated = "Calculated";
    public const string Closed = "Closed";
}

public sealed class PayrollRunData
{
    public int PayrollRunId { get; init; }
    public string RunType { get; init; } = string.Empty;
    public int PeriodYear { get; init; }
    public int PeriodMonth { get; init; }
    public DateTime PeriodStartDate { get; init; }
    public DateTime PeriodEndDate { get; init; }
    public DateTime PaymentDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public int EmployeeCount { get; init; }
    public decimal TotalGrossAmount { get; init; }
    public decimal TotalEmployeeContributionsAmount { get; init; }
    public decimal TotalIncomeTaxAmount { get; init; }
    public decimal TotalNetAmount { get; init; }
    public decimal TotalEmployerContributionsAmount { get; init; }
    public DateTime? CalculatedAtUtc { get; init; }
    public int? CalculatedByUserId { get; init; }
    public string? CalculatedByEmailAddress { get; init; }
    public DateTime? ClosedAtUtc { get; init; }
    public int? ClosedByUserId { get; init; }
    public string? ClosedByEmailAddress { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public int? CreatedByUserId { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public int? UpdatedByUserId { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public sealed class PayrollRunEmployeeData
{
    public int PayrollRunEmployeeId { get; init; }
    public int PayrollRunId { get; init; }
    public int EmployeeId { get; init; }
    public string IdentificationNumber { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public string? DepartmentName { get; init; }
    public string? JobTitle { get; init; }
    public decimal MonthlyBaseSalary { get; init; }
    public decimal PaidDays { get; init; }
    public decimal GrossAmount { get; init; }
    public decimal EmployeeContributionsAmount { get; init; }
    public decimal IncomeTaxAmount { get; init; }
    public decimal NetAmount { get; init; }
    public decimal EmployerContributionsAmount { get; init; }
}

public sealed class PayrollRunLineData
{
    public int PayrollRunLineId { get; init; }
    public int PayrollRunEmployeeId { get; init; }
    public int EmployeeId { get; init; }
    public int PayrollConceptId { get; init; }
    public string PayrollConceptCode { get; init; } = string.Empty;
    public string ConceptKind { get; init; } = string.Empty;
    public string? ReferenceCode { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal? Quantity { get; init; }
    public decimal? Rate { get; init; }
    public decimal? BaseAmount { get; init; }
    public decimal Amount { get; init; }
    public int SortOrder { get; init; }
}
