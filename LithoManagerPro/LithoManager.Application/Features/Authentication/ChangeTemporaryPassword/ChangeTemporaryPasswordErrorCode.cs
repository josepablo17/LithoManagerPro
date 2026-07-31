namespace LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;

public enum ChangeTemporaryPasswordErrorCode
{
    None = 0,
    InvalidRequest = 1,
    PasswordsDoNotMatch = 2,
    WeakPassword = 3
}