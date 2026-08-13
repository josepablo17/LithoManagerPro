namespace LithoManager.Application.Features.LeaveManagement;

public enum LeaveManagementErrorCode
{
    None = 0,
    InvalidRequest = 1,
    AccessNotAvailable = 2,
    EmployeeNotFound = 3,
    EmployeeInactive = 4,
    DepartmentInactive = 5,
    LeaveTypeNotFound = 6,
    LeavePolicyNotFound = 7,
    LeaveBalanceNotFound = 8,
    InsufficientLeaveBalance = 9,
    PendingLeaveRequestExists = 10,
    LeaveRequestDateOverlap = 11,
    LeaveRequestNotFound = 12,
    ConcurrencyConflict = 13,
    LeaveRequestAlreadyResolved = 14
}
