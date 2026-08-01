namespace LithoManager.Application.Features.Authentication
    .GetCurrentUser;

public enum CurrentUserErrorCode
{
    None = 0,
    InvalidRequest = 1,
    UserNotFound = 2,
    AccountInactive = 3,
    EmailNotConfirmed = 4,
    RoleInactive = 5,
    EmployeeInactive = 6,
    DepartmentInactive = 7,
    PasswordChangeRequired = 8
}