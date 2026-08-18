namespace LithoManager.Application.Features
    .Payroll.GetEmployeeDisabilities;

public sealed record GetEmployeeDisabilitiesQuery(
    int ActorUserId,
    int? EmployeeId,
    int? DepartmentId,
    int? DisabilityTypeId,
    string? DisabilityStatus,
    string? IssuerInstitution,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? SearchTerm);
