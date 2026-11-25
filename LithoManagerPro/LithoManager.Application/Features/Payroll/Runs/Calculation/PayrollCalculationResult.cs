namespace LithoManager.Application.Features.Payroll.Runs.Calculation;

public sealed class PayrollCalculationResult
{
    public IReadOnlyList<EmployeePayrollResult> Employees { get; init; } = [];

    /// <summary>
    /// Things a person should review (missing schedules, months without a
    /// closed payroll...). They do not stop the calculation.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class EmployeePayrollResult
{
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
    public IReadOnlyList<PayrollLineResult> Lines { get; init; } = [];
}

public sealed class PayrollLineResult
{
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
