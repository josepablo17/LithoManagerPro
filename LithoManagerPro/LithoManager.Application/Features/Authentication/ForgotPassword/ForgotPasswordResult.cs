namespace LithoManager.Application.Features.Authentication
    .ForgotPassword;

public sealed record ForgotPasswordResult(
    bool IsSuccessful,
    ForgotPasswordErrorCode ErrorCode)
{
    public static ForgotPasswordResult Success()
    {
        return new ForgotPasswordResult(
            IsSuccessful: true,
            ErrorCode:
                ForgotPasswordErrorCode.None);
    }

    public static ForgotPasswordResult Failure(
        ForgotPasswordErrorCode errorCode)
    {
        if (errorCode
            == ForgotPasswordErrorCode.None)
        {
            throw new ArgumentException(
                "A failure result must contain " +
                "an error code.",
                nameof(errorCode));
        }

        return new ForgotPasswordResult(
            IsSuccessful: false,
            ErrorCode: errorCode);
    }
}