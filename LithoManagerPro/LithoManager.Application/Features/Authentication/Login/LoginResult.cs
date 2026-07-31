using LithoManager.Application.Abstractions.Security;

namespace LithoManager.Application.Features.Authentication.Login;

public sealed record LoginResult(
    bool IsSuccessful,
    LoginErrorCode ErrorCode,
    LoginUserData? User,
    string? AccessToken,
    DateTimeOffset? AccessTokenExpiresAtUtc,
    bool RequiresPasswordChange,
    DateTime? LockoutEndAtUtc)
{
    public static LoginResult Success(
        LoginUserData user,
        AccessTokenResult accessToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(accessToken);

        return new LoginResult(
            IsSuccessful: true,
            ErrorCode: LoginErrorCode.None,
            User: user,
            AccessToken: accessToken.AccessToken,
            AccessTokenExpiresAtUtc: accessToken.ExpiresAtUtc,
            RequiresPasswordChange: false,
            LockoutEndAtUtc: null);
    }

    public static LoginResult PasswordChangeRequired(
        LoginUserData user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new LoginResult(
            IsSuccessful: true,
            ErrorCode: LoginErrorCode.None,
            User: user,
            AccessToken: null,
            AccessTokenExpiresAtUtc: null,
            RequiresPasswordChange: true,
            LockoutEndAtUtc: null);
    }

    public static LoginResult Failure(
        LoginErrorCode errorCode,
        DateTime? lockoutEndAtUtc = null)
    {
        if (errorCode == LoginErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new LoginResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            User: null,
            AccessToken: null,
            AccessTokenExpiresAtUtc: null,
            RequiresPasswordChange: false,
            LockoutEndAtUtc: lockoutEndAtUtc);
    }
}