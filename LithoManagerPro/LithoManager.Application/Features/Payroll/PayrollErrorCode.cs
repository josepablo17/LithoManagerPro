namespace LithoManager.Application.Features.Payroll;

public enum PayrollErrorCode
{
    None = 0,
    InvalidRequest = 1,
    AccessNotAvailable = 2,
    EmployeeNotFound = 3,
    EmployeeInactive = 4,
    DepartmentInactive = 5,
    ConfigurationNotFound = 6,
    AttendanceRecordNotFound = 7,
    OvertimeRecordNotFound = 8,
    EmployeeDisabilityNotFound = 9,
    DuplicateRecord = 10,
    DateOverlap = 11,
    InvalidState = 12,
    ConcurrencyConflict = 13
}
