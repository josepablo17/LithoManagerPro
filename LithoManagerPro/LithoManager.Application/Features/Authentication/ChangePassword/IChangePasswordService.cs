namespace LithoManager.Application.Features.Authentication
    .ChangePassword;

public interface IChangePasswordService
{
    Task<ChangePasswordResult> ChangeAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken);
}