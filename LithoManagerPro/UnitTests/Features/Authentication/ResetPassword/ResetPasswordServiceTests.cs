using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.Authentication
    .ResetPassword;
using LithoManager.Application.Security;
using LithoManager.UnitTests.TestDoubles.Persistence;
using LithoManager.UnitTests.TestDoubles.Security;

namespace LithoManager.UnitTests.Features.Authentication
    .ResetPassword;

public sealed class ResetPasswordServiceTests
{
    private const string Token =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private const string CurrentPasswordHash =
        "current-password-hash";

    private const string NewPassword =
        "NewSecure#Password123";

    private const string NewPasswordHash =
        "new-password-hash";

    private static readonly byte[] TokenHash =
        Enumerable
            .Range(1, 32)
            .Select(value => (byte)value)
            .ToArray();

    private static readonly DateTime
        PasswordChangedAtUtc =
            new(
                year: 2026,
                month: 8,
                day: 8,
                hour: 6,
                minute: 0,
                second: 0,
                kind: DateTimeKind.Utc);

    [Fact]
    public async Task ResetAsync_WhenCommandIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        // Act and assert
        await Assert.ThrowsAsync<
            ArgumentNullException>(
                () => service.ResetAsync(
                    null!,
                    CancellationToken.None));

        AssertNoExecution(
            repository,
            passwordService,
            tokenService);
    }

    [Fact]
    public async Task ResetAsync_WhenCorrelationIdIsEmpty_ReturnsInvalidRequest()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        ResetPasswordCommand command =
            CreateValidCommand() with
            {
                RequestContext =
                    new AuthenticationRequestContext(
                        CorrelationId:
                            Guid.Empty,
                        ClientIpAddress:
                            "127.0.0.1",
                        UserAgent:
                            "LithoManager.UnitTests",
                        RequestPath:
                            "/api/auth/reset-password")
            };

        // Act
        ResetPasswordResult result =
            await service.ResetAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ResetPasswordErrorCode
                .InvalidRequest);

        AssertNoExecution(
            repository,
            passwordService,
            tokenService);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ResetAsync_WhenTokenIsInvalid_ReturnsInvalidRequest(
        string token)
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        ResetPasswordCommand command =
            CreateValidCommand() with
            {
                Token = token
            };

        // Act
        ResetPasswordResult result =
            await service.ResetAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ResetPasswordErrorCode
                .InvalidRequest);

        AssertNoExecution(
            repository,
            passwordService,
            tokenService);
    }

    [Fact]
    public async Task ResetAsync_WhenTokenExceedsMaximumLength_ReturnsInvalidRequest()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        ResetPasswordCommand command =
            CreateValidCommand() with
            {
                Token = new string(
                    'A',
                    513)
            };

        // Act
        ResetPasswordResult result =
            await service.ResetAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ResetPasswordErrorCode
                .InvalidRequest);

        AssertNoExecution(
            repository,
            passwordService,
            tokenService);
    }

    [Fact]
    public async Task ResetAsync_WhenPasswordsDoNotMatch_ReturnsPasswordsDoNotMatch()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        ResetPasswordCommand command =
            CreateValidCommand() with
            {
                ConfirmNewPassword =
                    "Different#Password123"
            };

        // Act
        ResetPasswordResult result =
            await service.ResetAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ResetPasswordErrorCode
                .PasswordsDoNotMatch);

        AssertNoExecution(
            repository,
            passwordService,
            tokenService);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase123!")]
    [InlineData("ALLUPPERCASE123!")]
    [InlineData("NoNumbersHere!!")]
    [InlineData("NoSpecialCharacter123")]
    public async Task ResetAsync_WhenPasswordIsWeak_ReturnsWeakPassword(
        string password)
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        ResetPasswordCommand command =
            CreateValidCommand() with
            {
                NewPassword = password,
                ConfirmNewPassword = password
            };

        // Act
        ResetPasswordResult result =
            await service.ResetAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ResetPasswordErrorCode
                .WeakPassword);

        AssertNoExecution(
            repository,
            passwordService,
            tokenService);
    }

    [Fact]
    public async Task ResetAsync_WhenTokenIsNotAvailable_ReturnsPasswordResetNotAvailable()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                PasswordResetContextToReturn =
                    null
            };

        FakePasswordService passwordService =
            new();

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        // Act
        ResetPasswordResult result =
            await service.ResetAsync(
                CreateValidCommand(),
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ResetPasswordErrorCode
                .PasswordResetNotAvailable);

        Assert.Equal(
            1,
            tokenService.ComputeTokenHashCallCount);

        Assert.Equal(
            Token,
            tokenService.LastTokenToHash);

        Assert.Equal(
            1,
            repository
                .GetPasswordResetContextByTokenHashCallCount);

        Assert.NotNull(
            repository
                .LastPasswordResetContextTokenHash);

        Assert.Equal(
            TokenHash,
            repository
                .LastPasswordResetContextTokenHash);

        Assert.Equal(
            0,
            passwordService.VerifyPasswordCallCount);

        Assert.Equal(
            0,
            passwordService.HashPasswordCallCount);

        Assert.Equal(
            0,
            repository.CompletePasswordResetCallCount);
    }

    [Fact]
    public async Task ResetAsync_WhenNewPasswordMatchesCurrentPassword_ReturnsPasswordReuseNotAllowed()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                PasswordResetContextToReturn =
                    CreateValidContext()
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = true
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        // Act
        ResetPasswordResult result =
            await service.ResetAsync(
                CreateValidCommand(),
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ResetPasswordErrorCode
                .PasswordReuseNotAllowed);

        Assert.Equal(
            1,
            passwordService.VerifyPasswordCallCount);

        Assert.Equal(
            CurrentPasswordHash,
            passwordService
                .PasswordHashReceivedForVerification);

        Assert.Equal(
            NewPassword,
            passwordService
                .ProvidedPasswordReceived);

        Assert.Equal(
            0,
            passwordService.HashPasswordCallCount);

        Assert.Equal(
            0,
            repository.CompletePasswordResetCallCount);
    }

    [Fact]
    public async Task ResetAsync_WhenResetIsValid_CompletesPasswordReset()
    {
        // Arrange
        AuthenticationRequestContext requestContext =
            CreateValidRequestContext();

        FakeAuthenticationRepository repository =
            new()
            {
                PasswordResetContextToReturn =
                    CreateValidContext(),

                CompletePasswordResetToReturn =
                    CreateSuccessfulCompletion()
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = false,
                HashToReturn =
                    NewPasswordHash
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        ResetPasswordCommand command =
            new(
                Token:
                    Token,
                NewPassword:
                    NewPassword,
                ConfirmNewPassword:
                    NewPassword,
                RequestContext:
                    requestContext);

        // Act
        ResetPasswordResult result =
            await service.ResetAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            ResetPasswordErrorCode.None,
            result.ErrorCode);

        Assert.Equal(
            PasswordChangedAtUtc,
            result.PasswordChangedAtUtc);

        Assert.Equal(
            1,
            tokenService.ComputeTokenHashCallCount);

        Assert.Equal(
            Token,
            tokenService.LastTokenToHash);

        Assert.Equal(
            1,
            repository
                .GetPasswordResetContextByTokenHashCallCount);

        Assert.Equal(
            TokenHash,
            repository
                .LastPasswordResetContextTokenHash);

        Assert.Equal(
            1,
            passwordService.VerifyPasswordCallCount);

        Assert.Equal(
            CurrentPasswordHash,
            passwordService
                .PasswordHashReceivedForVerification);

        Assert.Equal(
            NewPassword,
            passwordService
                .ProvidedPasswordReceived);

        Assert.Equal(
            1,
            passwordService.HashPasswordCallCount);

        Assert.Equal(
            NewPassword,
            passwordService
                .PasswordReceivedForHash);

        Assert.Equal(
            1,
            repository.CompletePasswordResetCallCount);

        Assert.Equal(
            TokenHash,
            repository
                .LastCompletedPasswordResetTokenHash);

        Assert.Equal(
            CurrentPasswordHash,
            repository.LastExpectedPasswordHash);

        Assert.Equal(
            NewPasswordHash,
            repository.LastCompletedNewPasswordHash);

        Assert.Same(
            requestContext,
            repository
                .LastCompletePasswordResetRequestContext);
    }

    [Fact]
    public async Task ResetAsync_WhenFinalValidationFails_ReturnsPasswordResetNotAvailable()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                PasswordResetContextToReturn =
                    CreateValidContext(),

                CompletePasswordResetToReturn =
                    new CompletePasswordResetData
                    {
                        WasCompleted = false
                    }
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = false,
                HashToReturn =
                    NewPasswordHash
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        // Act
        ResetPasswordResult result =
            await service.ResetAsync(
                CreateValidCommand(),
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ResetPasswordErrorCode
                .PasswordResetNotAvailable);

        Assert.Equal(
            1,
            repository.CompletePasswordResetCallCount);
    }

    [Fact]
    public async Task ResetAsync_WhenContextContainsInvalidTokenId_ThrowsInvalidOperationException()
    {
        // Arrange
        PasswordResetContextData invalidContext =
            CreateValidContext();

        invalidContext.PasswordResetTokenId = 0;

        FakeAuthenticationRepository repository =
            new()
            {
                PasswordResetContextToReturn =
                    invalidContext
            };

        FakePasswordService passwordService =
            new();

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        // Act and assert
        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () => service.ResetAsync(
                    CreateValidCommand(),
                    CancellationToken.None));

        Assert.Equal(
            0,
            passwordService.VerifyPasswordCallCount);

        Assert.Equal(
            0,
            repository.CompletePasswordResetCallCount);
    }

    [Fact]
    public async Task ResetAsync_WhenContextExpirationIsNotUtc_ThrowsInvalidOperationException()
    {
        // Arrange
        PasswordResetContextData invalidContext =
            CreateValidContext();

        invalidContext.ExpiresAtUtc =
            DateTime.SpecifyKind(
                invalidContext.ExpiresAtUtc,
                DateTimeKind.Unspecified);

        FakeAuthenticationRepository repository =
            new()
            {
                PasswordResetContextToReturn =
                    invalidContext
            };

        FakePasswordService passwordService =
            new();

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        // Act and assert
        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () => service.ResetAsync(
                    CreateValidCommand(),
                    CancellationToken.None));

        Assert.Equal(
            0,
            repository.CompletePasswordResetCallCount);
    }

    [Fact]
    public async Task ResetAsync_WhenPasswordServiceReturnsInvalidHash_ThrowsInvalidOperationException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                PasswordResetContextToReturn =
                    CreateValidContext()
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = false,
                HashToReturn = string.Empty
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        // Act and assert
        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () => service.ResetAsync(
                    CreateValidCommand(),
                    CancellationToken.None));

        Assert.Equal(
            1,
            passwordService.HashPasswordCallCount);

        Assert.Equal(
            0,
            repository.CompletePasswordResetCallCount);
    }

    [Fact]
    public async Task ResetAsync_WhenFailedCompletionContainsData_ThrowsInvalidOperationException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                PasswordResetContextToReturn =
                    CreateValidContext(),

                CompletePasswordResetToReturn =
                    new CompletePasswordResetData
                    {
                        PasswordResetTokenId = 10,
                        UserId = 25,
                        WasCompleted = false
                    }
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = false,
                HashToReturn =
                    NewPasswordHash
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        // Act and assert
        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () => service.ResetAsync(
                    CreateValidCommand(),
                    CancellationToken.None));
    }

    [Fact]
    public async Task ResetAsync_WhenSuccessfulCompletionReturnsUnexpectedUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        CompletePasswordResetData completion =
            CreateSuccessfulCompletion();

        completion.UserId = 999;

        FakeAuthenticationRepository repository =
            new()
            {
                PasswordResetContextToReturn =
                    CreateValidContext(),

                CompletePasswordResetToReturn =
                    completion
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = false,
                HashToReturn =
                    NewPasswordHash
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        // Act and assert
        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () => service.ResetAsync(
                    CreateValidCommand(),
                    CancellationToken.None));
    }

    [Fact]
    public async Task ResetAsync_WhenSuccessfulCompletionKeepsTemporaryPasswordRequirement_ThrowsInvalidOperationException()
    {
        // Arrange
        CompletePasswordResetData completion =
            CreateSuccessfulCompletion();

        completion.RequiresPasswordChange = true;

        FakeAuthenticationRepository repository =
            new()
            {
                PasswordResetContextToReturn =
                    CreateValidContext(),

                CompletePasswordResetToReturn =
                    completion
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = false,
                HashToReturn =
                    NewPasswordHash
            };

        FakePasswordResetTokenService tokenService =
            CreateTokenService();

        ResetPasswordService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        // Act and assert
        await Assert.ThrowsAsync<
            InvalidOperationException>(
                () => service.ResetAsync(
                    CreateValidCommand(),
                    CancellationToken.None));
    }

    private static ResetPasswordService
        CreateService(
            FakeAuthenticationRepository repository,
            FakePasswordService passwordService,
            FakePasswordResetTokenService tokenService)
    {
        return new ResetPasswordService(
            repository,
            passwordService,
            new PasswordPolicy(),
            tokenService);
    }

    private static FakePasswordResetTokenService
        CreateTokenService()
    {
        return new FakePasswordResetTokenService
        {
            ComputedHashToReturn =
                TokenHash
        };
    }

    private static ResetPasswordCommand
        CreateValidCommand()
    {
        return new ResetPasswordCommand(
            Token:
                Token,
            NewPassword:
                NewPassword,
            ConfirmNewPassword:
                NewPassword,
            RequestContext:
                CreateValidRequestContext());
    }

    private static AuthenticationRequestContext
        CreateValidRequestContext()
    {
        return new AuthenticationRequestContext(
            CorrelationId:
                Guid.Parse(
                    "2181ed2e-f68d-4aae-a609-8c931aaa8f31"),
            ClientIpAddress:
                "127.0.0.1",
            UserAgent:
                "LithoManager.UnitTests",
            RequestPath:
                "/api/auth/reset-password");
    }

    private static PasswordResetContextData
        CreateValidContext()
    {
        return new PasswordResetContextData
        {
            PasswordResetTokenId = 10,
            UserId = 25,
            PasswordHash =
                CurrentPasswordHash,
            ExpiresAtUtc =
                new DateTime(
                    year: 2026,
                    month: 8,
                    day: 8,
                    hour: 6,
                    minute: 15,
                    second: 0,
                    kind: DateTimeKind.Utc)
        };
    }

    private static CompletePasswordResetData
        CreateSuccessfulCompletion()
    {
        return new CompletePasswordResetData
        {
            PasswordResetTokenId = 10,
            UserId = 25,
            PasswordChangedAtUtc =
                PasswordChangedAtUtc,
            RequiresPasswordChange =
                false,
            WasCompleted =
                true
        };
    }

    private static void AssertFailure(
        ResetPasswordResult result,
        ResetPasswordErrorCode expectedErrorCode)
    {
        Assert.False(
            result.IsSuccessful);

        Assert.Equal(
            expectedErrorCode,
            result.ErrorCode);

        Assert.Null(
            result.PasswordChangedAtUtc);
    }

    private static void AssertNoExecution(
        FakeAuthenticationRepository repository,
        FakePasswordService passwordService,
        FakePasswordResetTokenService tokenService)
    {
        Assert.Equal(
            0,
            tokenService.ComputeTokenHashCallCount);

        Assert.Equal(
            0,
            repository
                .GetPasswordResetContextByTokenHashCallCount);

        Assert.Equal(
            0,
            passwordService.VerifyPasswordCallCount);

        Assert.Equal(
            0,
            passwordService.HashPasswordCallCount);

        Assert.Equal(
            0,
            repository.CompletePasswordResetCallCount);
    }
}
