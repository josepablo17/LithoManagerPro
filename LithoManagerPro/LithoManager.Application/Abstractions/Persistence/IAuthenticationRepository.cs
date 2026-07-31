using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;

namespace LithoManager.Application.Abstractions.Persistence;

public interface IAuthenticationRepository
{
    Task<AuthenticationUserData?> GetUserForAuthenticationAsync(
        string emailAddress,
        CancellationToken cancellationToken);

    Task<SuccessfulLoginRegistrationData> RegisterSuccessfulLoginAsync(
    int userId,
    AuthenticationRequestContext requestContext,
    CancellationToken cancellationToken);

    Task<FailedLoginRegistrationData> RegisterFailedLoginAsync(
        string attemptedEmailAddress,
        int? userId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<TemporaryPasswordChangeData>
    ChangeTemporaryPasswordAsync(
        int userId,
        string newPasswordHash,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);
}