namespace LithoManager.Application.Features.Authentication
    .GetCurrentUser;

public interface IGetCurrentUserService
{
    Task<CurrentUserResult> GetAsync(
        int userId,
        CancellationToken cancellationToken);
}