using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;
using LithoManager.Application.Features.Authentication
    .GetCurrentUser;
using LithoManager.Application.Features.Authentication.Login;

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
}