using LithoManager.Application.Abstractions.Security;
using LithoManager.Application.Features.Authentication
    .ForgotPassword;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.UnitTests.TestDoubles
    .Notifications;
using LithoManager.UnitTests.TestDoubles
    .Persistence;
using LithoManager.UnitTests.TestDoubles
    .Security;
using LithoManager.UnitTests.TestDoubles.Time;
using Xunit;

namespace LithoManager.UnitTests.Features.Authentication
    .ForgotPassword;

public sealed class ForgotPasswordServiceTests
{
    private const string EmailAddress =
        "administrator@lithomanager.test";

    private const string GeneratedToken =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private static readonly byte[] TokenHash =
        new byte[32];

    private static readonly DateTimeOffset UtcNow =
        new(
            year: 2026,
            month: 8,
            day: 6,
            hour: 20,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

    private static readonly DateTime ExpiresAtUtc =
        UtcNow
            .AddMinutes(15)
            .UtcDateTime;

    [Fact]
    public async Task RequestAsync_WhenCommandIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        FakePasswordResetEmailSender emailSender =
            new();

        ForgotPasswordService service =
            CreateService(
                repository,
                tokenService,
                emailSender);

        // Act and assert
        await Assert.ThrowsAsync<
            ArgumentNullException>(
                () => service.RequestAsync(
                    null!,
                    CancellationToken.None));

        AssertNoExecution(
            repository,
            tokenService,
            emailSender);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-email")]
    [InlineData("user@example.com extra")]
    public async Task RequestAsync_WhenEmailIsInvalid_ReturnsInvalidRequest(
        string emailAddress)
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        FakePasswordResetEmailSender emailSender =
            new();

        ForgotPasswordService service =
            CreateService(
                repository,
                tokenService,
                emailSender);

        ForgotPasswordCommand command =
            CreateValidCommand() with
            {
                EmailAddress = emailAddress
            };

        // Act
        ForgotPasswordResult result =
            await service.RequestAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ForgotPasswordErrorCode.InvalidRequest);

        AssertNoExecution(
            repository,
            tokenService,
            emailSender);
    }



    [Fact]
    public async Task RequestAsync_WhenCorrelationIdIsEmpty_ReturnsInvalidRequest()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        FakePasswordResetEmailSender emailSender =
            new();

        ForgotPasswordService service =
            CreateService(
                repository,
                tokenService,
                emailSender);

        AuthenticationRequestContext requestContext =
            new(
                CorrelationId: Guid.Empty,
                ClientIpAddress: "127.0.0.1",
                UserAgent:
                    "LithoManager.UnitTests",
                RequestPath:
                    "/api/auth/forgot-password");

        ForgotPasswordCommand command =
            new(
                EmailAddress: EmailAddress,
                RequestContext: requestContext);

        // Act
        ForgotPasswordResult result =
            await service.RequestAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ForgotPasswordErrorCode.InvalidRequest);

        AssertNoExecution(
            repository,
            tokenService,
            emailSender);
    }

    [Fact]
    public async Task RequestAsync_WhenAccountIsEligible_CreatesTokenAndSendsEmail()
    {
        // Arrange
        AuthenticationRequestContext requestContext =
            CreateValidRequestContext();

        FakeAuthenticationRepository repository =
            new()
            {
                CreatePasswordResetTokenResult =
                    CreateSuccessfulTokenCreation()
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        FakePasswordResetEmailSender emailSender =
            new()
            {
                ResultToReturn = true
            };

        ForgotPasswordService service =
            CreateService(
                repository,
                tokenService,
                emailSender);

        ForgotPasswordCommand command =
            new(
                EmailAddress:
                    "  ADMINISTRATOR@LITHOMANAGER.TEST  ",
                RequestContext:
                    requestContext);

        // Act
        ForgotPasswordResult result =
            await service.RequestAsync(
                command,
                CancellationToken.None);

        // Assert: resultado público
        AssertSuccess(result);

        // Assert: generación
        Assert.Equal(
            1,
            tokenService.GenerateTokenCallCount);

        Assert.Equal(
            0,
            tokenService.ComputeTokenHashCallCount);

        // Assert: persistencia
        Assert.Equal(
            1,
            repository
                .CreatePasswordResetTokenCallCount);

        Assert.Equal(
            EmailAddress,
            repository
                .LastPasswordResetEmailAddress);

        Assert.Equal(
            TokenHash,
            repository
                .LastPasswordResetTokenHash);

        Assert.Equal(
            ExpiresAtUtc,
            repository
                .LastPasswordResetExpiresAtUtc);

        Assert.Same(
            requestContext,
            repository
                .LastPasswordResetRequestContext);

        // Assert: correo
        Assert.Equal(
            1,
            emailSender.CallCount);

        Assert.Equal(
            EmailAddress,
            emailSender.LastEmailAddress);

        Assert.Equal(
            GeneratedToken,
            emailSender.LastToken);

        Assert.Equal(
            ExpiresAtUtc,
            emailSender.LastExpiresAtUtc);

        Assert.Equal(
            requestContext.CorrelationId,
            emailSender.LastCorrelationId);
        Assert.Equal(
    0,
    repository
        .RevokePasswordResetTokenCallCount);

        // No se revoca porque el correo fue enviado.
        Assert.Equal(
            0,
            repository
                .RevokePasswordResetTokenCallCount);
    }

    [Fact]
    public async Task RequestAsync_WhenAccountIsNotEligible_ReturnsGenericSuccessWithoutSendingEmail()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                CreatePasswordResetTokenResult =
                    new CreatePasswordResetTokenData
                    {
                        PasswordResetTokenId = null,
                        UserId = null,
                        EmailAddress = null,
                        ExpiresAtUtc = null,
                        WasCreated = false
                    }
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        FakePasswordResetEmailSender emailSender =
            new();

        ForgotPasswordService service =
            CreateService(
                repository,
                tokenService,
                emailSender);

        ForgotPasswordCommand command =
            CreateValidCommand();

        // Act
        ForgotPasswordResult result =
            await service.RequestAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertSuccess(result);

        Assert.Equal(
            1,
            tokenService.GenerateTokenCallCount);

        Assert.Equal(
            1,
            repository
                .CreatePasswordResetTokenCallCount);

        Assert.Equal(
            0,
            emailSender.CallCount);

        Assert.Equal(
            0,
                repository
                    .RevokePasswordResetTokenCallCount);
                
        Assert.Equal(
            0,
            repository
                .RevokePasswordResetTokenCallCount);
    }

    [Fact]
    public async Task RequestAsync_WhenEmailDeliveryFails_RevokesCreatedTokenAndReturnsGenericSuccess()
    {
        // Arrange
        AuthenticationRequestContext requestContext =
            CreateValidRequestContext();

        DateTime revokedAtUtc =
            UtcNow
                .AddSeconds(2)
                .UtcDateTime;

        FakeAuthenticationRepository repository =
            new()
            {
                CreatePasswordResetTokenResult =
                    CreateSuccessfulTokenCreation(),

                RevokePasswordResetTokenResult =
                    new RevokePasswordResetTokenData
                    {
                        PasswordResetTokenId = 15,
                        UserId = 3,
                        RevokedAtUtc =
                            revokedAtUtc,
                        WasRevoked = true,
                        IsInactive = true
                    }
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        FakePasswordResetEmailSender emailSender =
            new()
            {
                ResultToReturn = false
            };

        ForgotPasswordService service =
            CreateService(
                repository,
                tokenService,
                emailSender);

        ForgotPasswordCommand command =
            new(
                EmailAddress: EmailAddress,
                RequestContext: requestContext);

        // Act
        ForgotPasswordResult result =
            await service.RequestAsync(
                command,
                CancellationToken.None);

        // Assert: la respuesta continúa siendo genérica
        AssertSuccess(result);

        Assert.Equal(
            1,
            emailSender.CallCount);

        Assert.Equal(
            1,
            repository
                .RevokePasswordResetTokenCallCount);

        Assert.Equal(
            15,
            repository
                .LastRevokedPasswordResetTokenId);

        Assert.Same(
            requestContext,
            repository
                .LastPasswordResetRevocationContext);
    }

    [Fact]
    public async Task RequestAsync_WhenTokenWasAlreadyInactiveAfterDeliveryFailure_ReturnsGenericSuccess()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                CreatePasswordResetTokenResult =
                    CreateSuccessfulTokenCreation(),

                RevokePasswordResetTokenResult =
                    new RevokePasswordResetTokenData
                    {
                        PasswordResetTokenId = 15,
                        UserId = 3,
                        RevokedAtUtc = null,
                        WasRevoked = false,
                        IsInactive = true
                    }
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        FakePasswordResetEmailSender emailSender =
            new()
            {
                ResultToReturn = false
            };

        ForgotPasswordService service =
            CreateService(
                repository,
                tokenService,
                emailSender);

        // Act
        ForgotPasswordResult result =
            await service.RequestAsync(
                CreateValidCommand(),
                CancellationToken.None);

        // Assert
        AssertSuccess(result);

        Assert.Equal(
            1,
            repository
                .RevokePasswordResetTokenCallCount);
    }

    [Fact]
    public async Task RequestAsync_WhenRepositoryReturnsDataWithoutCreatingToken_ThrowsInvalidOperationException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                CreatePasswordResetTokenResult =
                    new CreatePasswordResetTokenData
                    {
                        PasswordResetTokenId = null,
                        UserId = 3,
                        EmailAddress = null,
                        ExpiresAtUtc = null,
                        WasCreated = false
                    }
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        FakePasswordResetEmailSender emailSender =
            new();

        ForgotPasswordService service =
            CreateService(
                repository,
                tokenService,
                emailSender);

        // Act and assert
        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () => service.RequestAsync(
                        CreateValidCommand(),
                        CancellationToken.None));

        Assert.Contains(
            "was not created",
            exception.Message);

        Assert.Equal(
            0,
            emailSender.CallCount);

        Assert.Equal(
            0,
            repository
                .RevokePasswordResetTokenCallCount);
    }

    [Fact]
    public async Task RequestAsync_WhenRevocationReturnsUnexpectedTokenId_ThrowsInvalidOperationException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                CreatePasswordResetTokenResult =
                    CreateSuccessfulTokenCreation(),

                RevokePasswordResetTokenResult =
                    new RevokePasswordResetTokenData
                    {
                        PasswordResetTokenId = 99,
                        UserId = 3,
                        RevokedAtUtc =
                            UtcNow
                                .AddSeconds(2)
                                .UtcDateTime,
                        WasRevoked = true
                    }
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        FakePasswordResetEmailSender emailSender =
            new()
            {
                ResultToReturn = false
            };

        ForgotPasswordService service =
            CreateService(
                repository,
                tokenService,
                emailSender);

        // Act and assert
        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () => service.RequestAsync(
                        CreateValidCommand(),
                        CancellationToken.None));

        Assert.Contains(
            "unexpected token identifier",
            exception.Message);

        Assert.Equal(
            1,
            repository
                .RevokePasswordResetTokenCallCount);
    }

    private static ForgotPasswordService
        CreateService(
            FakeAuthenticationRepository repository,
            FakePasswordResetTokenService tokenService,
            FakePasswordResetEmailSender emailSender)
    {
        return new ForgotPasswordService(
            authenticationRepository:
                repository,
            passwordResetTokenService:
                tokenService,
            passwordResetEmailSender:
                emailSender,
            timeProvider:
                new FixedTimeProvider(
                    UtcNow));
    }

    private static FakePasswordResetTokenService
        CreateTokenService()
    {
        return new FakePasswordResetTokenService
        {
            GeneratedTokenToReturn =
                new GeneratedPasswordResetToken(
                    token:
                        GeneratedToken,
                    tokenHash:
                        TokenHash)
        };
    }

    private static ForgotPasswordCommand
        CreateValidCommand()
    {
        return new ForgotPasswordCommand(
            EmailAddress:
                EmailAddress,
            RequestContext:
                CreateValidRequestContext());
    }

    private static AuthenticationRequestContext
        CreateValidRequestContext()
    {
        return new AuthenticationRequestContext(
            CorrelationId:
                Guid.NewGuid(),
            ClientIpAddress:
                "127.0.0.1",
            UserAgent:
                "LithoManager.UnitTests",
            RequestPath:
                "/api/auth/forgot-password");
    }

    private static CreatePasswordResetTokenData
        CreateSuccessfulTokenCreation()
    {
        return new CreatePasswordResetTokenData
        {
            PasswordResetTokenId = 15,
            UserId = 3,
            EmailAddress =
                EmailAddress,
            ExpiresAtUtc =
                ExpiresAtUtc,
            WasCreated = true
        };
    }

    private static void AssertSuccess(
        ForgotPasswordResult result)
    {
        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            ForgotPasswordErrorCode.None,
            result.ErrorCode);
    }

    private static void AssertFailure(
        ForgotPasswordResult result,
        ForgotPasswordErrorCode expectedErrorCode)
    {
        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            expectedErrorCode,
            result.ErrorCode);
    }

    private static void AssertNoExecution(
        FakeAuthenticationRepository repository,
        FakePasswordResetTokenService tokenService,
        FakePasswordResetEmailSender emailSender)
    {
        Assert.Equal(
            0,
            tokenService.GenerateTokenCallCount);

        Assert.Equal(
            0,
            repository
                .CreatePasswordResetTokenCallCount);

        Assert.Equal(
            0,
            emailSender.CallCount);

        Assert.Equal(
            0,
            repository
                .RevokePasswordResetTokenCallCount);

        Assert.Equal(
    0,
    repository
        .RevokePasswordResetTokenCallCount);
    }

    [Fact]
    public async Task RequestAsync_WhenTokenIsAlreadyInactiveAfterDeliveryFailure_ReturnsGenericSuccess()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                CreatePasswordResetTokenResult =
                    CreateSuccessfulTokenCreation(),

                RevokePasswordResetTokenResult =
                    new RevokePasswordResetTokenData
                    {
                        PasswordResetTokenId = 15,
                        UserId = 3,
                        RevokedAtUtc = null,
                        WasRevoked = false,
                        IsInactive = true
                    }
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        FakePasswordResetEmailSender emailSender =
            new()
            {
                ResultToReturn = false
            };

        ForgotPasswordService service =
            CreateService(
                repository,
                tokenService,
                emailSender);

        // Act
        ForgotPasswordResult result =
            await service.RequestAsync(
                CreateValidCommand(),
                CancellationToken.None);

        // Assert
        AssertSuccess(result);

        Assert.Equal(
            1,
            emailSender.CallCount);

        Assert.Equal(
            1,
            repository
                .RevokePasswordResetTokenCallCount);
    }

    [Fact]
    public async Task RequestAsync_WhenRevocationLeavesTokenActive_ThrowsInvalidOperationException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                CreatePasswordResetTokenResult =
                    CreateSuccessfulTokenCreation(),

                RevokePasswordResetTokenResult =
                    new RevokePasswordResetTokenData
                    {
                        PasswordResetTokenId = 15,
                        UserId = 3,
                        RevokedAtUtc = null,
                        WasRevoked = false,
                        IsInactive = false
                    }
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        FakePasswordResetEmailSender emailSender =
            new()
            {
                ResultToReturn = false
            };

        ForgotPasswordService service =
            CreateService(
                repository,
                tokenService,
                emailSender);

        // Act and assert
        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () => service.RequestAsync(
                        CreateValidCommand(),
                        CancellationToken.None));

        Assert.Contains(
            "remained active",
            exception.Message);

        Assert.Equal(
            1,
            repository
                .RevokePasswordResetTokenCallCount);
    }

}