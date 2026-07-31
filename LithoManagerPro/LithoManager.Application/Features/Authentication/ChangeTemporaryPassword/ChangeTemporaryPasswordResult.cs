namespace LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;

public sealed record ChangeTemporaryPasswordResult(
    bool IsSuccessful,
    ChangeTemporaryPasswordErrorCode ErrorCode,
    DateTime? PasswordChangedAtUtc)
{
    public static ChangeTemporaryPasswordResult Success(
        DateTime passwordChangedAtUtc)
    {
        return new ChangeTemporaryPasswordResult(
            IsSuccessful: true,
            ErrorCode:
                ChangeTemporaryPasswordErrorCode.None,
            PasswordChangedAtUtc:
                passwordChangedAtUtc);
    }

    public static ChangeTemporaryPasswordResult Failure(
        ChangeTemporaryPasswordErrorCode errorCode)
    {
        if (errorCode
            == ChangeTemporaryPasswordErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new ChangeTemporaryPasswordResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            PasswordChangedAtUtc: null);
    }
}