using System.IdentityModel.Tokens.Jwt;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.IntegrationTests.Collections;
using LithoManager.IntegrationTests.Fixtures;
using Xunit;

namespace LithoManager.IntegrationTests.Application
    .Authentication;

[Collection(
    AuthenticationDatabaseCollection.Name)]
public sealed class
    AuthenticationServiceIntegrationTests
{
    private readonly AuthenticationDatabaseFixture
        _fixture;

    public AuthenticationServiceIntegrationTests(
        AuthenticationDatabaseFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(
            fixture);

        _fixture = fixture;
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsRealAccessToken()
    {
        // Arrange
        await _fixture.ResetLoginStateAsync();

        AuthenticationService service =
            CreateService();

        LoginCommand command =
            new(
                EmailAddress:
                    AuthenticationDatabaseFixture
                        .TestEmailAddress,

                Password:
                    AuthenticationDatabaseFixture
                        .TestPassword,

                RequestContext:
                    AuthenticationDatabaseFixture
                        .CreateRequestContext(
                            "/integration-tests/" +
                            "login-success"));

        // Act
        LoginResult result =
            await service.LoginAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);

        Assert.Equal(
            LoginErrorCode.None,
            result.ErrorCode);

        Assert.False(
            result.RequiresPasswordChange);

        string accessToken =
            Assert.IsType<string>(
                result.AccessToken);

        Assert.False(
            string.IsNullOrWhiteSpace(
                accessToken));

        Assert.Null(
            result.PasswordChangeToken);

        DateTimeOffset expiresAtUtc =
            Assert.IsType<DateTimeOffset>(
                result.AccessTokenExpiresAtUtc);

        Assert.True(
            expiresAtUtc
            > DateTimeOffset.UtcNow);

        LoginUserData user =
            Assert.IsType<LoginUserData>(
                result.User);

        Assert.Equal(
            _fixture.SuperAdministratorUserId,
            user.UserId);

        Assert.Equal(
            AuthenticationDatabaseFixture
                .TestEmailAddress,
            user.EmailAddress);

        Assert.Equal(
            "SuperAdministrator",
            user.RoleCode);

        JwtSecurityToken jwt =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(
                    accessToken);

        Assert.Equal(
            _fixture
                .SuperAdministratorUserId
                .ToString(),
            jwt.Subject);

        Assert.Contains(
            jwt.Claims,
            claim =>
                claim.Type == "token_use"
                && claim.Value == "access");

        string tokenVersion =
            Assert.Single(
                jwt.Claims,
                claim =>
                    claim.Type == "token_version")
                .Value;

        Assert.True(
            int.TryParse(
                tokenVersion,
                out int parsedTokenVersion));

        Assert.True(
            parsedTokenVersion > 0);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_RegistersFailedAttempt()
    {
        // Arrange
        await _fixture.ResetLoginStateAsync();

        AuthenticationService service =
            CreateService();

        LoginCommand command =
            new(
                EmailAddress:
                    AuthenticationDatabaseFixture
                        .TestEmailAddress,

                Password:
                    "IncorrectPassword1!",

                RequestContext:
                    AuthenticationDatabaseFixture
                        .CreateRequestContext(
                            "/integration-tests/" +
                            "login-failure"));

        try
        {
            // Act
            LoginResult result =
                await service.LoginAsync(
                    command,
                    CancellationToken.None);

            // Assert
            Assert.False(
                result.IsSuccessful);

            Assert.Equal(
                LoginErrorCode.InvalidCredentials,
                result.ErrorCode);

            Assert.Null(
                result.AccessToken);

            AuthenticationUserData? user =
                await _fixture.Repository
                    .GetUserForAuthenticationAsync(
                        AuthenticationDatabaseFixture
                            .TestEmailAddress,
                        CancellationToken.None);

            Assert.NotNull(user);

            Assert.Equal(
                1,
                user.FailedLoginAttempts);

            Assert.Null(
                user.LockoutEndAtUtc);
        }
        finally
        {
            await _fixture.ResetLoginStateAsync();
        }
    }

    private AuthenticationService
        CreateService()
    {
        return new AuthenticationService(
            authenticationRepository:
                _fixture.Repository,

            passwordService:
                _fixture.PasswordService,

            tokenService:
                _fixture.TokenService,

            refreshTokenService:
                _fixture.RefreshTokenService,

            sessionOptions:
                new LithoManager.Application.Features
                    .Authentication
                    .AuthenticationSessionOptions
                {
                    RefreshTokenExpirationDays = 1
                },

            securityOptions:
                new LithoManager.Application.Features
                    .Authentication
                    .AuthenticationSecurityOptions
                {
                    PasswordResetTokenExpirationMinutes = 15,
                    MaximumFailedLoginAttempts = 5,
                    LockoutDurationMinutes = 15
                },

            timeProvider:
                _fixture.TimeProvider);
    }
}
