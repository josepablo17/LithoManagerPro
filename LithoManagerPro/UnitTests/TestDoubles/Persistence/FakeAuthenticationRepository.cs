using LithoManager.Application.Abstractions.Persistence;
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

namespace LithoManager.UnitTests.TestDoubles.Persistence;

public sealed class FakeAuthenticationRepository
    : IAuthenticationRepository
{
    public AuthenticationUserData?
        AuthenticationUserToReturn
    {
        get;
        set;
    }

    public CurrentUserData? CurrentUserToReturn
    {
        get;
        set;
    }

    public UserTokenValidationData?
        UserTokenValidationToReturn
    {
        get;
        set;
    }

    public FailedLoginRegistrationData
        FailedLoginToReturn
    {
        get;
        set;
    } = new()
    {
        FailedLoginAttempts = 1,
        IsLockedOut = false
    };

    public SuccessfulLoginRegistrationData
        SuccessfulLoginToReturn
    {
        get;
        set;
    } = new()
    {
        UserId = 1,
        LastLoginAtUtc = DateTime.UtcNow,
        FailedLoginAttempts = 0,
        LockoutEndAtUtc = null
    };

    public TemporaryPasswordChangeData?
        TemporaryPasswordChangeToReturn
    {
        get;
        set;
    }

    public ChangePasswordData?
    ChangePasswordToReturn
    {
        get;
        set;
    }

    public int
    GetUserForAuthenticationByIdCallCount
    {
        get;
        private set;
    }

    public int
    GetUserTokenValidationByIdCallCount
    {
        get;
        private set;
    }

    public int?
        RequestedAuthenticationUserId
    {
        get;
        private set;
    }

    public int?
        RequestedTokenValidationUserId
    {
        get;
        private set;
    }

    public int ChangePasswordCallCount
    {
        get;
        private set;
    }

    public int?
        RequestedVoluntaryPasswordChangeUserId
    {
        get;
        private set;
    }

    public string?
        RequestedVoluntaryNewPasswordHash
    {
        get;
        private set;
    }

    public AuthenticationRequestContext?
        RequestedVoluntaryPasswordChangeContext
    {
        get;
        private set;
    }

    public int GetUserForAuthenticationCallCount
    {
        get;
        private set;
    }

    public string? RequestedEmailAddress
    {
        get;
        private set;
    }

    public int RegisterFailedLoginCallCount
    {
        get;
        private set;
    }

    public string? FailedLoginEmailAddress
    {
        get;
        private set;
    }

    public int? FailedLoginUserId
    {
        get;
        private set;
    }

    public AuthenticationRequestContext?
        FailedLoginRequestContext
    {
        get;
        private set;
    }

    public short? FailedLoginMaximumAttempts
    {
        get;
        private set;
    }

    public int? FailedLoginLockoutDurationMinutes
    {
        get;
        private set;
    }

    public int RegisterSuccessfulLoginCallCount
    {
        get;
        private set;
    }

    public int? SuccessfulLoginUserId
    {
        get;
        private set;
    }

    public AuthenticationRequestContext?
        SuccessfulLoginRequestContext
    {
        get;
        private set;
    }

    public int GetCurrentUserByIdCallCount
    {
        get;
        private set;
    }

    public int? RequestedUserId
    {
        get;
        private set;
    }

    public int ChangeTemporaryPasswordCallCount
    {
        get;
        private set;
    }

    public int? RequestedPasswordChangeUserId
    {
        get;
        private set;
    }

    public string? RequestedNewPasswordHash
    {
        get;
        private set;
    }

    public AuthenticationRequestContext?
        RequestedPasswordChangeContext
    {
        get;
        private set;
    }

    public RevokePasswordResetTokenData
    RevokePasswordResetTokenResult
    {
        get;
        set;
    } = new()
    {
        PasswordResetTokenId = 1,
        UserId = 1,
        RevokedAtUtc =
        DateTime.SpecifyKind(
            DateTime.UtcNow,
            DateTimeKind.Utc),
        WasRevoked = true,
        IsInactive = true
    };

    public int
        RevokePasswordResetTokenCallCount
    {
        get;
        private set;
    }

    public int?
        LastRevokedPasswordResetTokenId
    {
        get;
        private set;
    }

    public AuthenticationRequestContext?
        LastPasswordResetRevocationContext
    {
        get;
        private set;
    }

    public PasswordResetContextData?
    PasswordResetContextToReturn
    {
        get;
        set;
    }

    public CompletePasswordResetData?
        CompletePasswordResetToReturn
    {
        get;
        set;
    }

    public int
        GetPasswordResetContextByTokenHashCallCount
    {
        get;
        private set;
    }

    public byte[]?
        LastPasswordResetContextTokenHash
    {
        get;
        private set;
    }

    public int CompletePasswordResetCallCount
    {
        get;
        private set;
    }

    public byte[]?
        LastCompletedPasswordResetTokenHash
    {
        get;
        private set;
    }

    public string?
        LastExpectedPasswordHash
    {
        get;
        private set;
    }

    public string?
        LastCompletedNewPasswordHash
    {
        get;
        private set;
    }

    public AuthenticationRequestContext?
        LastCompletePasswordResetRequestContext
    {
        get;
        private set;
    }

    public CreateRefreshTokenData?
        CreateRefreshTokenToReturn
    {
        get;
        set;
    }

    public RefreshTokenContextData?
        RefreshTokenContextToReturn
    {
        get;
        set;
    }

    public RotateRefreshTokenData?
        RotateRefreshTokenToReturn
    {
        get;
        set;
    }

    public RevokeRefreshTokenData?
        RevokeRefreshTokenToReturn
    {
        get;
        set;
    }

    public RevokeUserRefreshTokensData?
        RevokeUserRefreshTokensToReturn
    {
        get;
        set;
    }

    public int CreateRefreshTokenCallCount
    {
        get;
        private set;
    }

    public int? LastRefreshTokenUserId
    {
        get;
        private set;
    }

    public byte[]? LastCreatedRefreshTokenHash
    {
        get;
        private set;
    }

    public Guid? LastRefreshTokenFamilyId
    {
        get;
        private set;
    }

    public DateTime? LastRefreshTokenExpiresAtUtc
    {
        get;
        private set;
    }

    public AuthenticationRequestContext?
        LastCreateRefreshTokenRequestContext
    {
        get;
        private set;
    }

    public int GetRefreshTokenContextCallCount
    {
        get;
        private set;
    }

    public byte[]? LastRefreshTokenContextHash
    {
        get;
        private set;
    }

    public int RotateRefreshTokenCallCount
    {
        get;
        private set;
    }

    public byte[]? LastCurrentRefreshTokenHash
    {
        get;
        private set;
    }

    public byte[]? LastNewRefreshTokenHash
    {
        get;
        private set;
    }

    public DateTime? LastRotatedRefreshTokenExpiresAtUtc
    {
        get;
        private set;
    }

    public AuthenticationRequestContext?
        LastRotateRefreshTokenRequestContext
    {
        get;
        private set;
    }

    public int RevokeRefreshTokenCallCount
    {
        get;
        private set;
    }

    public byte[]? LastRevokedRefreshTokenHash
    {
        get;
        private set;
    }

    public string? LastRefreshTokenRevokedReason
    {
        get;
        private set;
    }

    public AuthenticationRequestContext?
        LastRevokeRefreshTokenRequestContext
    {
        get;
        private set;
    }

    public int RevokeUserRefreshTokensCallCount
    {
        get;
        private set;
    }

    public int? LastRefreshTokensRevocationUserId
    {
        get;
        private set;
    }

    public int? LastRefreshTokensRevocationActorUserId
    {
        get;
        private set;
    }

    public string?
        LastUserRefreshTokensRevokedReason
    {
        get;
        private set;
    }

    public AuthenticationRequestContext?
        LastRevokeUserRefreshTokensRequestContext
    {
        get;
        private set;
    }

    public Task<AuthenticationUserData?>
        GetUserForAuthenticationAsync(
            string emailAddress,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetUserForAuthenticationCallCount++;
        RequestedEmailAddress = emailAddress;

        return Task.FromResult(
            AuthenticationUserToReturn);
    }

    public Task<AuthenticationUserData?>
    GetUserForAuthenticationByIdAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetUserForAuthenticationByIdCallCount++;

        RequestedAuthenticationUserId =
            userId;

        return Task.FromResult(
            AuthenticationUserToReturn);
    }

    public Task<UserTokenValidationData?>
    GetUserTokenValidationByIdAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetUserTokenValidationByIdCallCount++;

        RequestedTokenValidationUserId =
            userId;

        return Task.FromResult(
            UserTokenValidationToReturn);
    }

    public Task<FailedLoginRegistrationData>
        RegisterFailedLoginAsync(
            string attemptedEmailAddress,
            int? userId,
            short maximumFailedLoginAttempts,
            int lockoutDurationMinutes,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RegisterFailedLoginCallCount++;

        FailedLoginEmailAddress =
            attemptedEmailAddress;

        FailedLoginUserId = userId;

        FailedLoginMaximumAttempts =
            maximumFailedLoginAttempts;

        FailedLoginLockoutDurationMinutes =
            lockoutDurationMinutes;

        FailedLoginRequestContext =
            requestContext;

        return Task.FromResult(
            FailedLoginToReturn);
    }

    public Task<SuccessfulLoginRegistrationData>
        RegisterSuccessfulLoginAsync(
            int userId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RegisterSuccessfulLoginCallCount++;

        SuccessfulLoginUserId =
            userId;

        SuccessfulLoginRequestContext =
            requestContext;

        return Task.FromResult(
            SuccessfulLoginToReturn);
    }

    public Task<CurrentUserData?>
        GetCurrentUserByIdAsync(
            int userId,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetCurrentUserByIdCallCount++;
        RequestedUserId = userId;

        return Task.FromResult(
            CurrentUserToReturn);
    }

    public Task<TemporaryPasswordChangeData>
        ChangeTemporaryPasswordAsync(
            int userId,
            string newPasswordHash,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ChangeTemporaryPasswordCallCount++;

        RequestedPasswordChangeUserId =
            userId;

        RequestedNewPasswordHash =
            newPasswordHash;

        RequestedPasswordChangeContext =
            requestContext;

        TemporaryPasswordChangeData result =
            TemporaryPasswordChangeToReturn
            ?? throw new InvalidOperationException(
                "No temporary-password result " +
                "was configured for this test.");

        return Task.FromResult(result);
    }


    public Task<ChangePasswordData>
    ChangePasswordAsync(
        int userId,
        string newPasswordHash,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ChangePasswordCallCount++;

        RequestedVoluntaryPasswordChangeUserId =
            userId;

        RequestedVoluntaryNewPasswordHash =
            newPasswordHash;

        RequestedVoluntaryPasswordChangeContext =
            requestContext;

        ChangePasswordData result =
            ChangePasswordToReturn
            ?? throw new InvalidOperationException(
                "No voluntary password-change result " +
                "was configured for this test.");

        return Task.FromResult(result);
    }

    public CreatePasswordResetTokenData
    CreatePasswordResetTokenResult
    { get; set; } =
        new();

    public string? LastPasswordResetEmailAddress
    {
        get;
        private set;
    }

    public byte[]? LastPasswordResetTokenHash
    {
        get;
        private set;
    }

    public DateTime? LastPasswordResetExpiresAtUtc
    {
        get;
        private set;
    }

    public AuthenticationRequestContext?
        LastPasswordResetRequestContext
    {
        get;
        private set;
    }

    public int CreatePasswordResetTokenCallCount
    {
        get;
        private set;
    }

    public Task<CreatePasswordResetTokenData>
    CreatePasswordResetTokenAsync(
        string emailAddress,
        byte[] tokenHash,
        DateTime expiresAtUtc,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CreatePasswordResetTokenCallCount++;

        LastPasswordResetEmailAddress =
            emailAddress;

        LastPasswordResetTokenHash =
            (byte[])tokenHash.Clone();

        LastPasswordResetExpiresAtUtc =
            expiresAtUtc;

        LastPasswordResetRequestContext =
            requestContext;

        return Task.FromResult(
            CreatePasswordResetTokenResult);
    }

    public Task<RevokePasswordResetTokenData>
    RevokePasswordResetTokenAfterDeliveryFailureAsync(
        int passwordResetTokenId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        RevokePasswordResetTokenCallCount++;

        LastRevokedPasswordResetTokenId =
            passwordResetTokenId;

        LastPasswordResetRevocationContext =
            requestContext;

        return Task.FromResult(
            RevokePasswordResetTokenResult);
    }

    public Task<PasswordResetContextData?>
GetPasswordResetContextByTokenHashAsync(
    byte[] tokenHash,
    CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        GetPasswordResetContextByTokenHashCallCount++;

        LastPasswordResetContextTokenHash =
            (byte[])tokenHash.Clone();

        return Task.FromResult(
            PasswordResetContextToReturn);
    }

    public Task<CompletePasswordResetData>
    CompletePasswordResetAsync(
        byte[] tokenHash,
        string expectedPasswordHash,
        string newPasswordHash,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        CompletePasswordResetCallCount++;

        LastCompletedPasswordResetTokenHash =
            (byte[])tokenHash.Clone();

        LastExpectedPasswordHash =
            expectedPasswordHash;

        LastCompletedNewPasswordHash =
            newPasswordHash;

        LastCompletePasswordResetRequestContext =
            requestContext;

        CompletePasswordResetData result =
            CompletePasswordResetToReturn
            ?? throw new InvalidOperationException(
                "No password-reset completion result " +
                "was configured for this test.");

        return Task.FromResult(result);
    }

    public Task<CreateRefreshTokenData>
    CreateRefreshTokenAsync(
        int userId,
        byte[] tokenHash,
        Guid tokenFamilyId,
        DateTime expiresAtUtc,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        CreateRefreshTokenCallCount++;

        LastRefreshTokenUserId =
            userId;

        LastCreatedRefreshTokenHash =
            (byte[])tokenHash.Clone();

        LastRefreshTokenFamilyId =
            tokenFamilyId;

        LastRefreshTokenExpiresAtUtc =
            expiresAtUtc;

        LastCreateRefreshTokenRequestContext =
            requestContext;

        CreateRefreshTokenData result =
            CreateRefreshTokenToReturn
            ?? throw new InvalidOperationException(
                "No refresh-token creation result " +
                "was configured for this test.");

        return Task.FromResult(result);
    }

    public Task<RefreshTokenContextData?>
    GetRefreshTokenContextByTokenHashAsync(
        byte[] tokenHash,
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        GetRefreshTokenContextCallCount++;

        LastRefreshTokenContextHash =
            (byte[])tokenHash.Clone();

        return Task.FromResult(
            RefreshTokenContextToReturn);
    }

    public Task<RotateRefreshTokenData>
    RotateRefreshTokenAsync(
        byte[] currentTokenHash,
        byte[] newTokenHash,
        DateTime expiresAtUtc,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        RotateRefreshTokenCallCount++;

        LastCurrentRefreshTokenHash =
            (byte[])currentTokenHash.Clone();

        LastNewRefreshTokenHash =
            (byte[])newTokenHash.Clone();

        LastRotatedRefreshTokenExpiresAtUtc =
            expiresAtUtc;

        LastRotateRefreshTokenRequestContext =
            requestContext;

        RotateRefreshTokenData result =
            RotateRefreshTokenToReturn
            ?? throw new InvalidOperationException(
                "No refresh-token rotation result " +
                "was configured for this test.");

        return Task.FromResult(result);
    }

    public Task<RevokeRefreshTokenData>
    RevokeRefreshTokenAsync(
        byte[] tokenHash,
        string revokedReason,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        RevokeRefreshTokenCallCount++;

        LastRevokedRefreshTokenHash =
            (byte[])tokenHash.Clone();

        LastRefreshTokenRevokedReason =
            revokedReason;

        LastRevokeRefreshTokenRequestContext =
            requestContext;

        RevokeRefreshTokenData result =
            RevokeRefreshTokenToReturn
            ?? throw new InvalidOperationException(
                "No refresh-token revocation result " +
                "was configured for this test.");

        return Task.FromResult(result);
    }

    public Task<RevokeUserRefreshTokensData>
    RevokeUserRefreshTokensAsync(
        int userId,
        string revokedReason,
        int? actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        RevokeUserRefreshTokensCallCount++;

        LastRefreshTokensRevocationUserId =
            userId;

        LastRefreshTokensRevocationActorUserId =
            actorUserId;

        LastUserRefreshTokensRevokedReason =
            revokedReason;

        LastRevokeUserRefreshTokensRequestContext =
            requestContext;

        RevokeUserRefreshTokensData result =
            RevokeUserRefreshTokensToReturn
            ?? throw new InvalidOperationException(
                "No user refresh-token revocation result " +
                "was configured for this test.");

        return Task.FromResult(result);
    }
}
