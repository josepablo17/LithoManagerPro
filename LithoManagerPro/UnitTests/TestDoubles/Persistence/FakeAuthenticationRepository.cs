using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;
using LithoManager.Application.Features.Authentication
    .GetCurrentUser;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Authentication
    .ChangePassword;
using LithoManager.Application.Features.Authentication.ForgotPassword;

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

    public int?
        RequestedAuthenticationUserId
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

    public Task<FailedLoginRegistrationData>
        RegisterFailedLoginAsync(
            string attemptedEmailAddress,
            int? userId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        RegisterFailedLoginCallCount++;

        FailedLoginEmailAddress =
            attemptedEmailAddress;

        FailedLoginUserId = userId;

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
}