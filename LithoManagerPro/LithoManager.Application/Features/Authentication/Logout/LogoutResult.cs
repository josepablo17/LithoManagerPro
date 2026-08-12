namespace LithoManager.Application.Features.Authentication.Logout;

public sealed record LogoutResult(
    bool IsSuccessful,
    LogoutErrorCode ErrorCode,
    int? UserId,
    DateTime? RevokedAtUtc,
    int RevokedCount,
    bool WasRevoked,
    bool WasAlreadyInactive)
{
    public static LogoutResult Success(
        int? userId,
        DateTime? revokedAtUtc,
        int revokedCount,
        bool wasRevoked,
        bool wasAlreadyInactive)
    {
        return new LogoutResult(
            IsSuccessful: true,
            ErrorCode: LogoutErrorCode.None,
            UserId: userId,
            RevokedAtUtc: revokedAtUtc,
            RevokedCount: revokedCount,
            WasRevoked: wasRevoked,
            WasAlreadyInactive: wasAlreadyInactive);
    }

    public static LogoutResult Failure(
        LogoutErrorCode errorCode)
    {
        if (errorCode == LogoutErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new LogoutResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            UserId: null,
            RevokedAtUtc: null,
            RevokedCount: 0,
            WasRevoked: false,
            WasAlreadyInactive: false);
    }
}
