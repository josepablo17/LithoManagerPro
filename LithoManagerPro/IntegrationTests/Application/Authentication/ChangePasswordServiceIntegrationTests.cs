using LithoManager.Application.Features.Authentication
    .ChangePassword;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.IntegrationTests.Collections;
using LithoManager.IntegrationTests.Fixtures;
using Xunit;

namespace LithoManager.IntegrationTests.Application
    .Authentication;

[Collection(
    AuthenticationDatabaseCollection.Name)]
public sealed class
    ChangePasswordServiceIntegrationTests
{
    private readonly AuthenticationDatabaseFixture
        _fixture;

    public ChangePasswordServiceIntegrationTests(
        AuthenticationDatabaseFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(
            fixture);

        _fixture = fixture;
    }

    [Fact]
    public async Task ChangeAsync_WhenCredentialsAreValid_PersistsNewPassword()
    {
        // Arrange
        await _fixture.RestoreTestPasswordAsync();

        ChangePasswordService service =
            CreateService();

        AuthenticationRequestContext requestContext =
            AuthenticationDatabaseFixture
                .CreateRequestContext(
                    "/integration-tests/" +
                    "service-change-password");

        ChangePasswordCommand command =
            new(
                UserId:
                    _fixture
                        .SuperAdministratorUserId,
                CurrentPassword:
                    AuthenticationDatabaseFixture
                        .TestPassword,
                NewPassword:
                    AuthenticationDatabaseFixture
                        .ChangedTestPassword,
                ConfirmNewPassword:
                    AuthenticationDatabaseFixture
                        .ChangedTestPassword,
                RequestContext:
                    requestContext);

        DateTime startedAtUtc =
            DateTime.UtcNow.AddSeconds(-2);

        try
        {
            // Act
            ChangePasswordResult result =
                await service.ChangeAsync(
                    command,
                    CancellationToken.None);

            DateTime completedAtUtc =
                DateTime.UtcNow.AddSeconds(2);

            // Assert: resultado del servicio
            Assert.True(
                result.IsSuccessful);

            Assert.Equal(
                ChangePasswordErrorCode.None,
                result.ErrorCode);

            DateTime passwordChangedAtUtc =
                Assert.IsType<DateTime>(
                    result.PasswordChangedAtUtc);

            Assert.InRange(
                passwordChangedAtUtc,
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
                _fixture.SuperAdministratorUserId,
                updatedUser.UserId);

            Assert.Equal(
                passwordChangedAtUtc,
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

            // La contraseña anterior ya no funciona.
            Assert.False(
                _fixture.PasswordService
                    .VerifyPassword(
                        updatedUser.PasswordHash,
                        AuthenticationDatabaseFixture
                            .TestPassword));
        }
        finally
        {
            await _fixture
                .RestoreTestPasswordAsync();
        }
    }

    [Fact]
    public async Task ChangeAsync_WhenCurrentPasswordIsInvalid_DoesNotChangePassword()
    {
        // Arrange
        await _fixture.RestoreTestPasswordAsync();

        ChangePasswordService service =
            CreateService();

        ChangePasswordCommand command =
            new(
                UserId:
                    _fixture
                        .SuperAdministratorUserId,
                CurrentPassword:
                    "IncorrectCurrentPassword1!",
                NewPassword:
                    AuthenticationDatabaseFixture
                        .ChangedTestPassword,
                ConfirmNewPassword:
                    AuthenticationDatabaseFixture
                        .ChangedTestPassword,
                RequestContext:
                    AuthenticationDatabaseFixture
                        .CreateRequestContext(
                            "/integration-tests/" +
                            "invalid-current-password"));

        try
        {
            // Act
            ChangePasswordResult result =
                await service.ChangeAsync(
                    command,
                    CancellationToken.None);

            // Assert
            Assert.False(
                result.IsSuccessful);

            Assert.Equal(
                ChangePasswordErrorCode
                    .CurrentPasswordInvalid,
                result.ErrorCode);

            Assert.Null(
                result.PasswordChangedAtUtc);

            AuthenticationUserData? user =
                await _fixture.Repository
                    .GetUserForAuthenticationByIdAsync(
                        _fixture
                            .SuperAdministratorUserId,
                        CancellationToken.None);

            Assert.NotNull(user);

            // La contraseña original permanece.
            Assert.True(
                _fixture.PasswordService
                    .VerifyPassword(
                        user.PasswordHash,
                        AuthenticationDatabaseFixture
                            .TestPassword));

            // La contraseña nueva no fue persistida.
            Assert.False(
                _fixture.PasswordService
                    .VerifyPassword(
                        user.PasswordHash,
                        AuthenticationDatabaseFixture
                            .ChangedTestPassword));
        }
        finally
        {
            await _fixture
                .RestoreTestPasswordAsync();
        }
    }

    [Fact]
    public async Task ChangeAsync_WhenNewPasswordMatchesCurrentPassword_ReturnsPasswordReuseNotAllowed()
    {
        // Arrange
        await _fixture.RestoreTestPasswordAsync();

        ChangePasswordService service =
            CreateService();

        ChangePasswordCommand command =
            new(
                UserId:
                    _fixture
                        .SuperAdministratorUserId,
                CurrentPassword:
                    AuthenticationDatabaseFixture
                        .TestPassword,
                NewPassword:
                    AuthenticationDatabaseFixture
                        .TestPassword,
                ConfirmNewPassword:
                    AuthenticationDatabaseFixture
                        .TestPassword,
                RequestContext:
                    AuthenticationDatabaseFixture
                        .CreateRequestContext(
                            "/integration-tests/" +
                            "password-reuse"));

        try
        {
            // Act
            ChangePasswordResult result =
                await service.ChangeAsync(
                    command,
                    CancellationToken.None);

            // Assert
            Assert.False(
                result.IsSuccessful);

            Assert.Equal(
                ChangePasswordErrorCode
                    .PasswordReuseNotAllowed,
                result.ErrorCode);

            Assert.Null(
                result.PasswordChangedAtUtc);

            AuthenticationUserData? user =
                await _fixture.Repository
                    .GetUserForAuthenticationByIdAsync(
                        _fixture
                            .SuperAdministratorUserId,
                        CancellationToken.None);

            Assert.NotNull(user);

            Assert.True(
                _fixture.PasswordService
                    .VerifyPassword(
                        user.PasswordHash,
                        AuthenticationDatabaseFixture
                            .TestPassword));
        }
        finally
        {
            await _fixture
                .RestoreTestPasswordAsync();
        }
    }

    private ChangePasswordService CreateService()
    {
        return new ChangePasswordService(
            authenticationRepository:
                _fixture.Repository,
            passwordService:
                _fixture.PasswordService,
            timeProvider:
                _fixture.TimeProvider);
    }
}