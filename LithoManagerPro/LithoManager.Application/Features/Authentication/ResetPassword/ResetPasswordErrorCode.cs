namespace LithoManager.Application.Features.Authentication
    .ResetPassword;

public enum ResetPasswordErrorCode
{
    None = 0,
    InvalidRequest = 1,
    PasswordsDoNotMatch = 2,
    WeakPassword = 3,
    PasswordReuseNotAllowed = 4,
    PasswordResetNotAvailable = 5
}