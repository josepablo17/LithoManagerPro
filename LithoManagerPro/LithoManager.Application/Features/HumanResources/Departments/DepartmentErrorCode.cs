namespace LithoManager.Application.Features
    .HumanResources.Departments;

public enum DepartmentErrorCode
{
    None = 0,
    InvalidRequest = 1,
    DepartmentNotFound = 2,
    DuplicateDepartmentCode = 3,
    DuplicateDepartmentName = 4,
    ConcurrencyConflict = 5,
    AccessNotAvailable = 6,
    DepartmentHasActiveEmployees = 7
}
