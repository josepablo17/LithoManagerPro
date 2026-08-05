namespace LithoManager.Application.Features.Authentication
    .ChangePassword;

public enum ChangePasswordErrorCode
{
    None = 0,
    InvalidRequest = 1,
    PasswordsDoNotMatch = 2,
    WeakPassword = 3,
    CurrentPasswordInvalid = 4,
    PasswordReuseNotAllowed = 5,
    AccessNotAvailable = 6
}