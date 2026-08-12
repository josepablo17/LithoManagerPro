using LithoManager.Application.Abstractions.Security;

namespace LithoManager.Application.Features.Authentication.Login;

public sealed record LoginResult(
    bool IsSuccessful,
    LoginErrorCode ErrorCode,
    LoginUserData? User,
    string? AccessToken,
    DateTimeOffset? AccessTokenExpiresAtUtc,
    string? RefreshToken,
    DateTime? RefreshTokenExpiresAtUtc,
    string? PasswordChangeToken,
    DateTimeOffset? PasswordChangeTokenExpiresAtUtc,
    bool RequiresPasswordChange,
    DateTime? LockoutEndAtUtc)
{
    public static LoginResult Success(
        LoginUserData user,
        AccessTokenResult accessToken)
    {
        return Success(
            user,
            accessToken,
            refreshToken: null,
            refreshTokenExpiresAtUtc: null);
    }

    public static LoginResult Success(
        LoginUserData user,
        AccessTokenResult accessToken,
        string? refreshToken,
        DateTime? refreshTokenExpiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(accessToken);

        return new LoginResult(
            IsSuccessful: true,
            ErrorCode: LoginErrorCode.None,
            User: user,
            AccessToken: accessToken.AccessToken,
            AccessTokenExpiresAtUtc:
                accessToken.ExpiresAtUtc,
            RefreshToken: refreshToken,
            RefreshTokenExpiresAtUtc:
                refreshTokenExpiresAtUtc,
            PasswordChangeToken: null,
            PasswordChangeTokenExpiresAtUtc: null,
            RequiresPasswordChange: false,
            LockoutEndAtUtc: null);
    }

    public static LoginResult PasswordChangeRequired(
        LoginUserData user,
        PasswordChangeTokenResult passwordChangeToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        ArgumentNullException.ThrowIfNull(
            passwordChangeToken);

        return new LoginResult(
            IsSuccessful: true,
            ErrorCode: LoginErrorCode.None,
            User: user,
            AccessToken: null,
            AccessTokenExpiresAtUtc: null,
            RefreshToken: null,
            RefreshTokenExpiresAtUtc: null,
            PasswordChangeToken:
                passwordChangeToken.Token,
            PasswordChangeTokenExpiresAtUtc:
                passwordChangeToken.ExpiresAtUtc,
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
            RefreshToken: null,
            RefreshTokenExpiresAtUtc: null,
            PasswordChangeToken: null,
            PasswordChangeTokenExpiresAtUtc: null,
            RequiresPasswordChange: false,
            LockoutEndAtUtc: lockoutEndAtUtc);
    }
}
