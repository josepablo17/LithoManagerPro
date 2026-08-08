namespace LithoManager.Application.Features.Authentication
    .ResetPassword;

public sealed record ResetPasswordResult(
    bool IsSuccessful,
    ResetPasswordErrorCode ErrorCode,
    DateTime? PasswordChangedAtUtc)
{
    public static ResetPasswordResult Success(
        DateTime passwordChangedAtUtc)
    {
        return new ResetPasswordResult(
            IsSuccessful: true,
            ErrorCode:
                ResetPasswordErrorCode.None,
            PasswordChangedAtUtc:
                passwordChangedAtUtc);
    }

    public static ResetPasswordResult Failure(
        ResetPasswordErrorCode errorCode)
    {
        if (errorCode
            == ResetPasswordErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain " +
                "an error code.",
                nameof(errorCode));
        }

        return new ResetPasswordResult(
            IsSuccessful: false,
            ErrorCode: errorCode,
            PasswordChangedAtUtc: null);
    }
}