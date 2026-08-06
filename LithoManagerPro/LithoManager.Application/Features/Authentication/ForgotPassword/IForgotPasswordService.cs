namespace LithoManager.Application.Features.Authentication
    .ForgotPassword;

public interface IForgotPasswordService
{
    Task<ForgotPasswordResult> RequestAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken);
}