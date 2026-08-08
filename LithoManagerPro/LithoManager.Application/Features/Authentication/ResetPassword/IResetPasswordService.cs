namespace LithoManager.Application.Features.Authentication
    .ResetPassword;

public interface IResetPasswordService
{
    Task<ResetPasswordResult> ResetAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken);
}