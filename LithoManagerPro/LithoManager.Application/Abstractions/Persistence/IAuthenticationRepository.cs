using LithoManager.Application.Features.Authentication.Login;

namespace LithoManager.Application.Abstractions.Persistence;

public interface IAuthenticationRepository
{
    Task<AuthenticationUserData?> GetUserForAuthenticationAsync(
        string emailAddress,
        CancellationToken cancellationToken);
}