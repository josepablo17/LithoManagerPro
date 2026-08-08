using LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;
using LithoManager.Application.Features.Authentication
    .GetCurrentUser;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Authentication
    .ChangePassword;
using LithoManager.Application.Features.Authentication.ForgotPassword;

namespace LithoManager.Application.Abstractions.Persistence;

public interface IAuthenticationRepository
{
    Task<AuthenticationUserData?>
        GetUserForAuthenticationAsync(
            string emailAddress,
            CancellationToken cancellationToken);

    Task<AuthenticationUserData?>
    GetUserForAuthenticationByIdAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<CurrentUserData?> GetCurrentUserByIdAsync(
        int userId,
        CancellationToken cancellationToken);

    Task<SuccessfulLoginRegistrationData>
        RegisterSuccessfulLoginAsync(
            int userId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken);

    Task<FailedLoginRegistrationData>
        RegisterFailedLoginAsync(
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

    Task<ChangePasswordData> ChangePasswordAsync(
    int userId,
    string newPasswordHash,
    AuthenticationRequestContext requestContext,
    CancellationToken cancellationToken);

    Task<CreatePasswordResetTokenData>
    CreatePasswordResetTokenAsync(
        string emailAddress,
        byte[] tokenHash,
        DateTime expiresAtUtc,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);


    Task<RevokePasswordResetTokenData>
    RevokePasswordResetTokenAfterDeliveryFailureAsync(
        int passwordResetTokenId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);



}

