namespace LithoManager.Application.Features.Authentication.Login;

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken);
}