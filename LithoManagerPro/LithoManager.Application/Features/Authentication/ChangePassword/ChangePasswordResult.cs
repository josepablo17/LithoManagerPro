namespace LithoManager.Application.Features.Authentication
    .ChangePassword;

public sealed record ChangePasswordResult(
    bool IsSuccessful,
    ChangePasswordErrorCode ErrorCode,
    DateTime? PasswordChangedAtUtc)
{
    public static ChangePasswordResult Success(
        DateTime passwordChangedAtUtc)
    {
        return new ChangePasswordResult(
            IsSuccessful: true,
            ErrorCode: ChangePasswordErrorCode.None,
            PasswordChangedAtUtc: passwordChangedAtUtc);
    }

    public static ChangePasswordResult Failure(
        ChangePasswordErrorCode errorCode)
    {
        if (errorCode == ChangePasswordErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain an error code.",
                nameof(errorCode));
        }

        return new ChangePasswordResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            PasswordChangedAtUtc: null);
    }
}