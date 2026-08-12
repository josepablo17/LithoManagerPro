namespace LithoManager.Application.Features.Authentication.Logout;

public interface ILogoutService
{
    Task<LogoutResult> LogoutAsync(
        LogoutCommand command,
        CancellationToken cancellationToken);

    Task<LogoutResult> RevokeUserSessionsAsync(
        RevokeUserSessionsCommand command,
        CancellationToken cancellationToken);
}
