namespace LithoManager.Application.Features.Authentication
    .RefreshSession;

public interface IRefreshSessionService
{
    Task<RefreshSessionResult> RefreshAsync(
        RefreshSessionCommand command,
        CancellationToken cancellationToken);
}
