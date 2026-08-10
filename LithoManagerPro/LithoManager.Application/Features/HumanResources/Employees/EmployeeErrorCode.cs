namespace LithoManager.Application.Features
    .HumanResources.Employees;

public enum EmployeeErrorCode
{
    None = 0,
    InvalidRequest = 1,
    EmployeeNotFound = 2,
    DuplicateIdentificationNumber = 3,
    UserNotFound = 4,
    UserAlreadyAssigned = 5,
    DepartmentNotFound = 6,
    DepartmentInactive = 7,
    ConcurrencyConflict = 8,
    AccessNotAvailable = 9
}
