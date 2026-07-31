namespace LithoManager.Application.Features.Authentication.Login;

public enum LoginErrorCode
{
    None = 0,
    InvalidRequest = 1,
    InvalidCredentials = 2,
    AccountLocked = 3,
    EmailNotConfirmed = 4,
    AccountInactive = 5,
    RoleInactive = 6,
    EmployeeInactive = 7,
    TemporaryPasswordExpired = 8
}