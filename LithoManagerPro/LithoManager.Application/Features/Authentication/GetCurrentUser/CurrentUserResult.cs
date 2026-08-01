namespace LithoManager.Application.Features.Authentication
    .GetCurrentUser;

public sealed record CurrentUserResult(
    bool IsSuccessful,
    CurrentUserErrorCode ErrorCode,
    CurrentUserInfo? User)
{
    public static CurrentUserResult Success(
        CurrentUserInfo user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new CurrentUserResult(
            IsSuccessful: true,
            ErrorCode: CurrentUserErrorCode.None,
            User: user);
    }

    public static CurrentUserResult Failure(
        CurrentUserErrorCode errorCode)
    {
        if (errorCode == CurrentUserErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new CurrentUserResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            User: null);
    }
}