using LithoManager.Application.Features.Authentication
    .GetCurrentUser;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.IntegrationTests.Collections;
using LithoManager.IntegrationTests.Fixtures;
using Xunit;
using LithoManager.Application.Features.Authentication
    .ChangePassword;

namespace LithoManager.IntegrationTests.Infrastructure
    .Persistence;

[Collection(
    AuthenticationDatabaseCollection.Name)]
public sealed class AuthenticationRepositoryTests
{
    private readonly AuthenticationDatabaseFixture
        _fixture;

    public AuthenticationRepositoryTests(
        AuthenticationDatabaseFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(
            fixture);

        _fixture = fixture;
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenRequestIsValid_PersistsNewPassword()
    {
        // Arrange
        await _fixture.RestoreTestPasswordAsync();

        Guid correlationId =
            Guid.NewGuid();

        AuthenticationRequestContext requestContext =
            new(
                CorrelationId:
                    correlationId,
                ClientIpAddress:
                    "127.0.0.1",
                UserAgent:
                    "LithoManager.IntegrationTests",
                RequestPath:
                    "/integration-tests/" +
                    "repository-change-password");

        string newPasswordHash =
            _fixture.PasswordService.HashPassword(
                AuthenticationDatabaseFixture
                    .ChangedTestPassword);

        DateTime startedAtUtc =
            DateTime.UtcNow.AddSeconds(-2);

        try
        {
            // Act
            ChangePasswordData result =
                await _fixture.Repository
                    .ChangePasswordAsync(
                        userId:
                            _fixture
                                .SuperAdministratorUserId,
                        newPasswordHash:
                            newPasswordHash,
                        requestContext:
                            requestContext,
                        cancellationToken:
                            CancellationToken.None);

            DateTime completedAtUtc =
                DateTime.UtcNow.AddSeconds(2);

            // Assert: resultado del procedimiento
            Assert.Equal(
                _fixture.SuperAdministratorUserId,
                result.UserId);

            Assert.False(
                result.RequiresPasswordChange);

            Assert.InRange(
                result.PasswordChangedAtUtc,
                startedAtUtc,
                completedAtUtc);

            // Assert: persistencia real
            AuthenticationUserData? updatedUser =
                await _fixture.Repository
                    .GetUserForAuthenticationByIdAsync(
                        _fixture
                            .SuperAdministratorUserId,
                        CancellationToken.None);

            Assert.NotNull(updatedUser);

            Assert.Equal(
                result.PasswordChangedAtUtc,
                updatedUser.PasswordChangedAtUtc);

            Assert.False(
                updatedUser.RequiresPasswordChange);

            Assert.Equal(
                0,
                updatedUser.FailedLoginAttempts);

            Assert.Null(
                updatedUser.LockoutEndAtUtc);

            // La contraseña nueva funciona.
            Assert.True(
                _fixture.PasswordService
                    .VerifyPassword(
                        updatedUser.PasswordHash,
                        AuthenticationDatabaseFixture
                            .ChangedTestPassword));

            // La contraseña anterior dejó de funcionar.
            Assert.False(
                _fixture.PasswordService
                    .VerifyPassword(
                        updatedUser.PasswordHash,
                        AuthenticationDatabaseFixture
                            .TestPassword));
        }
        finally
        {
            await _fixture.RestoreTestPasswordAsync();
        }
    }

    [Fact]
    public async Task GetUserForAuthenticationByIdAsync_WhenUserExists_ReturnsSecurityData()
    {
        // Arrange
        await _fixture.RestoreTestPasswordAsync();

        // Act
        AuthenticationUserData? user =
            await _fixture.Repository
                .GetUserForAuthenticationByIdAsync(
                    _fixture.SuperAdministratorUserId,
                    CancellationToken.None);

        // Assert
        Assert.NotNull(user);

        Assert.Equal(
            _fixture.SuperAdministratorUserId,
            user.UserId);

        Assert.Equal(
            AuthenticationDatabaseFixture
                .TestEmailAddress,
            user.EmailAddress);

        Assert.True(
            _fixture.PasswordService
                .VerifyPassword(
                    user.PasswordHash,
                    AuthenticationDatabaseFixture
                        .TestPassword));

        Assert.True(user.IsEmailConfirmed);
        Assert.True(user.IsActive);
        Assert.True(user.IsRoleActive);

        Assert.False(
            user.RequiresPasswordChange);

        Assert.Null(
            user.LockoutEndAtUtc);

        Assert.Equal(
            "SuperAdministrator",
            user.RoleCode);
    }

    [Fact]
    public async Task GetUserForAuthenticationByIdAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Act
        AuthenticationUserData? user =
            await _fixture.Repository
                .GetUserForAuthenticationByIdAsync(
                    userId: int.MaxValue,
                    CancellationToken.None);

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task GetCurrentUserByIdAsync_WhenUserExists_ReturnsMappedUser()
    {
        // Act
        CurrentUserData? user =
            await _fixture.Repository
                .GetCurrentUserByIdAsync(
                    _fixture.SuperAdministratorUserId,
                    CancellationToken.None);

        // Assert
        Assert.NotNull(user);

        Assert.Equal(
            _fixture.SuperAdministratorUserId,
            user.UserId);

        Assert.Equal(
            AuthenticationDatabaseFixture
                .TestEmailAddress,
            user.EmailAddress);

        Assert.True(user.IsEmailConfirmed);
        Assert.True(user.IsActive);
        Assert.True(user.IsRoleActive);

        Assert.Equal(
            "SuperAdministrator",
            user.RoleCode);

        Assert.False(
            string.IsNullOrWhiteSpace(
                user.RoleDisplayName));

        Assert.False(
            user.RequiresPasswordChange);

        Assert.Null(user.EmployeeId);
        Assert.Null(user.FirstName);
        Assert.Null(user.LastName);
        Assert.Null(user.JobTitle);

        Assert.Null(user.DepartmentId);
        Assert.Null(user.DepartmentCode);
        Assert.Null(user.DepartmentName);
    }

    [Fact]
    public async Task GetCurrentUserByIdAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Act
        CurrentUserData? user =
            await _fixture.Repository
                .GetCurrentUserByIdAsync(
                    userId: int.MaxValue,
                    CancellationToken.None);

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserForAuthenticationAsync_WhenUserExists_ReturnsSecurityData()
    {
        // Act
        AuthenticationUserData? user =
            await _fixture.Repository
                .GetUserForAuthenticationAsync(
                    AuthenticationDatabaseFixture
                        .TestEmailAddress,
                    CancellationToken.None);

        // Assert
        Assert.NotNull(user);

        Assert.Equal(
            _fixture.SuperAdministratorUserId,
            user.UserId);

        Assert.Equal(
            AuthenticationDatabaseFixture
                .TestEmailAddress,
            user.EmailAddress);

        Assert.True(
            _fixture.PasswordService
                .VerifyPassword(
                    user.PasswordHash,
                    AuthenticationDatabaseFixture
                        .TestPassword));

        Assert.True(user.IsEmailConfirmed);
        Assert.True(user.IsActive);
        Assert.False(user.RequiresPasswordChange);

        Assert.Null(
            user.TemporaryPasswordExpiresAtUtc);

        Assert.Equal(
            0,
            user.FailedLoginAttempts);

        Assert.Null(user.LockoutEndAtUtc);

        Assert.True(user.RoleId > 0);

        Assert.Equal(
            "SuperAdministrator",
            user.RoleCode);

        Assert.True(user.IsRoleActive);

        Assert.Null(user.EmployeeId);
        Assert.Null(user.IsEmployeeActive);
        Assert.Null(user.DepartmentId);
    }

    [Fact]
    public async Task GetUserForAuthenticationAsync_WhenEmailDoesNotExist_ReturnsNull()
    {
        // Act
        AuthenticationUserData? user =
            await _fixture.Repository
                .GetUserForAuthenticationAsync(
                    "missing.user@lithomanager.local",
                    CancellationToken.None);

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task RegisterFailedLoginAsync_WhenUserIsUnknown_RegistersAnonymousAttempt()
    {
        // Arrange
        AuthenticationRequestContext requestContext =
            CreateRequestContext();

        // Act
        FailedLoginRegistrationData result =
            await _fixture.Repository
                .RegisterFailedLoginAsync(
                    attemptedEmailAddress:
                        "unknown.user@lithomanager.local",
                    userId: null,
                    requestContext:
                        requestContext,
                    cancellationToken:
                        CancellationToken.None);

        // Assert
        Assert.Null(result.UserId);

        Assert.Equal(
            0,
            result.FailedLoginAttempts);

        Assert.Null(
            result.LockoutEndAtUtc);

        Assert.False(
            result.IsLockedOut);
    }

    [Fact]
    public async Task RegisterSuccessfulLoginAsync_WhenUserExists_UpdatesLoginState()
    {
        // Arrange
        AuthenticationRequestContext requestContext =
            CreateRequestContext();

        DateTime startedAtUtc =
            DateTime.UtcNow.AddSeconds(-2);

        // Act
        SuccessfulLoginRegistrationData result =
            await _fixture.Repository
                .RegisterSuccessfulLoginAsync(
                    userId:
                        _fixture
                            .SuperAdministratorUserId,
                    requestContext:
                        requestContext,
                    cancellationToken:
                        CancellationToken.None);

        DateTime completedAtUtc =
            DateTime.UtcNow.AddSeconds(2);

        // Assert
        Assert.Equal(
            _fixture.SuperAdministratorUserId,
            result.UserId);

        Assert.Equal(
            0,
            result.FailedLoginAttempts);

        Assert.Null(
            result.LockoutEndAtUtc);

        Assert.InRange(
            result.LastLoginAtUtc,
            startedAtUtc,
            completedAtUtc);

        AuthenticationUserData? updatedUser =
            await _fixture.Repository
                .GetUserForAuthenticationAsync(
                    AuthenticationDatabaseFixture
                        .TestEmailAddress,
                    CancellationToken.None);

        Assert.NotNull(updatedUser);

        Assert.Equal(
            result.LastLoginAtUtc,
            updatedUser.LastLoginAtUtc);

        Assert.Equal(
            0,
            updatedUser.FailedLoginAttempts);

        Assert.Null(
            updatedUser.LockoutEndAtUtc);
    }

    private static AuthenticationRequestContext
        CreateRequestContext()
    {
        return new AuthenticationRequestContext(
            CorrelationId: Guid.NewGuid(),
            ClientIpAddress: "127.0.0.1",
            UserAgent:
                "LithoManager.IntegrationTests",
            RequestPath:
                "/integration-tests/authentication");
    }
}