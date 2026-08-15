namespace LithoManager.Api.Contracts.Documents;

public sealed record DocumentEmployeeOptionResponse(
    int EmployeeId,
    string IdentificationNumber,
    string FirstName,
    string LastName,
    int DepartmentId,
    string DepartmentCode,
    string DepartmentName,
    string JobTitle);
