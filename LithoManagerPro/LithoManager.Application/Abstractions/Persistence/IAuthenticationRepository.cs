using LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;
using LithoManager.Application.Features.Authentication
    .GetCurrentUser;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Authentication
    .ChangePassword;
using LithoManager.Application.Features.Authentication.ForgotPassword;
using LithoManager.Application.Features.Authentication
    .ResetPassword;
using LithoManager.Application.Features.Authentication
    .RefreshTokens;

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

    Task<UserTokenValidationData?>
    GetUserTokenValidationByIdAsync(
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
            short maximumFailedLoginAttempts,
            int lockoutDurationMinutes,
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

    Task<PasswordResetContextData?>
GetPasswordResetContextByTokenHashAsync(
    byte[] tokenHash,
    CancellationToken cancellationToken);

    Task<CompletePasswordResetData>
    CompletePasswordResetAsync(
        byte[] tokenHash,
        string expectedPasswordHash,
        string newPasswordHash,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<CreateRefreshTokenData> CreateRefreshTokenAsync(
        int userId,
        byte[] tokenHash,
        Guid tokenFamilyId,
        DateTime expiresAtUtc,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<RefreshTokenContextData?>
    GetRefreshTokenContextByTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken);

    Task<RotateRefreshTokenData> RotateRefreshTokenAsync(
        byte[] currentTokenHash,
        byte[] newTokenHash,
        DateTime expiresAtUtc,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<RevokeRefreshTokenData> RevokeRefreshTokenAsync(
        byte[] tokenHash,
        string revokedReason,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

    Task<RevokeUserRefreshTokensData>
    RevokeUserRefreshTokensAsync(
        int userId,
        string revokedReason,
        int? actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken);

}

