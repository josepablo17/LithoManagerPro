using LithoManager.Application.Features.Authentication.Login;

namespace LithoManager.Application.Features.Authentication
    .RefreshSession;

public sealed record RefreshSessionResult(
    bool IsSuccessful,
    RefreshSessionErrorCode ErrorCode,
    LoginUserData? User,
    string? AccessToken,
    DateTimeOffset? AccessTokenExpiresAtUtc,
    string? RefreshToken,
    DateTime? RefreshTokenExpiresAtUtc)
{
    public static RefreshSessionResult Success(
        LoginUserData user,
        string accessToken,
        DateTimeOffset accessTokenExpiresAtUtc,
        string refreshToken,
        DateTime refreshTokenExpiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            refreshToken);

        return new RefreshSessionResult(
            IsSuccessful: true,
            ErrorCode: RefreshSessionErrorCode.None,
            User: user,
            AccessToken: accessToken,
            AccessTokenExpiresAtUtc:
                accessTokenExpiresAtUtc,
            RefreshToken: refreshToken,
            RefreshTokenExpiresAtUtc:
                refreshTokenExpiresAtUtc);
    }

    public static RefreshSessionResult Failure(
        RefreshSessionErrorCode errorCode)
    {
        if (errorCode == RefreshSessionErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new RefreshSessionResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            User: null,
            AccessToken: null,
            AccessTokenExpiresAtUtc: null,
            RefreshToken: null,
            RefreshTokenExpiresAtUtc: null);
    }
}
