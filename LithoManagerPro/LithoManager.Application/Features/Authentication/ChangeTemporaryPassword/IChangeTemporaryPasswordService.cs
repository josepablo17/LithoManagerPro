namespace LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;

public interface IChangeTemporaryPasswordService
{
    Task<ChangeTemporaryPasswordResult> ChangeAsync(
        ChangeTemporaryPasswordCommand command,
        CancellationToken cancellationToken);
}