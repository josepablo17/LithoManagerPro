using LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.UnitTests.TestDoubles.Persistence;
using LithoManager.UnitTests.TestDoubles.Security;
using Xunit;

namespace LithoManager.UnitTests.Features.Authentication
    .ChangeTemporaryPassword;

public sealed class ChangeTemporaryPasswordServiceTests
{
    [Fact]
    public async Task ChangeAsync_WhenCommandIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangeTemporaryPasswordService service =
            new(
                repository,
                passwordService);

        // Act and assert
        await Assert.ThrowsAsync<
            ArgumentNullException>(
                () => service.ChangeAsync(
                    null!,
                    CancellationToken.None));

        Assert.Equal(
            0,
            passwordService.HashPasswordCallCount);

        Assert.Equal(
            0,
            repository.ChangeTemporaryPasswordCallCount);
    }

    [Fact]
    public async Task ChangeAsync_WhenUserIdIsInvalid_ReturnsInvalidRequest()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangeTemporaryPasswordService service =
            new(
                repository,
                passwordService);

        ChangeTemporaryPasswordCommand command =
            new(
                UserId: 0,
                NewPassword: "StrongPassword1!",
                ConfirmNewPassword: "StrongPassword1!",
                RequestContext:
                    CreateValidRequestContext());

        // Act
        ChangeTemporaryPasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);

        Assert.Equal(
            ChangeTemporaryPasswordErrorCode
                .InvalidRequest,
            result.ErrorCode);

        Assert.Null(
            result.PasswordChangedAtUtc);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Fact]
    public async Task ChangeAsync_WhenCorrelationIdIsEmpty_ReturnsInvalidRequest()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangeTemporaryPasswordService service =
            new(
                repository,
                passwordService);

        AuthenticationRequestContext requestContext =
            new(
                CorrelationId: Guid.Empty,
                ClientIpAddress: "127.0.0.1",
                UserAgent: "UnitTests",
                RequestPath:
                    "/api/auth/change-temporary-password");

        ChangeTemporaryPasswordCommand command =
            new(
                UserId: 1,
                NewPassword: "StrongPassword1!",
                ConfirmNewPassword: "StrongPassword1!",
                RequestContext: requestContext);

        // Act
        ChangeTemporaryPasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);

        Assert.Equal(
            ChangeTemporaryPasswordErrorCode
                .InvalidRequest,
            result.ErrorCode);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("StrongPassword1!", "")]
    [InlineData("", "StrongPassword1!")]
    public async Task ChangeAsync_WhenPasswordFieldsAreMissing_ReturnsInvalidRequest(
        string newPassword,
        string confirmNewPassword)
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangeTemporaryPasswordService service =
            new(
                repository,
                passwordService);

        ChangeTemporaryPasswordCommand command =
            new(
                UserId: 1,
                NewPassword: newPassword,
                ConfirmNewPassword:
                    confirmNewPassword,
                RequestContext:
                    CreateValidRequestContext());

        // Act
        ChangeTemporaryPasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);

        Assert.Equal(
            ChangeTemporaryPasswordErrorCode
                .InvalidRequest,
            result.ErrorCode);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Fact]
    public async Task ChangeAsync_WhenPasswordsDoNotMatch_ReturnsPasswordsDoNotMatch()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangeTemporaryPasswordService service =
            new(
                repository,
                passwordService);

        ChangeTemporaryPasswordCommand command =
            new(
                UserId: 1,
                NewPassword: "StrongPassword1!",
                ConfirmNewPassword:
                    "DifferentPassword1!",
                RequestContext:
                    CreateValidRequestContext());

        // Act
        ChangeTemporaryPasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);

        Assert.Equal(
            ChangeTemporaryPasswordErrorCode
                .PasswordsDoNotMatch,
            result.ErrorCode);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Theory]
    [InlineData("Short1!")]
    [InlineData("strongpassword1!")]
    [InlineData("STRONGPASSWORD1!")]
    [InlineData("NoDigitsHere!")]
    [InlineData("StrongPassword123")]
    [InlineData(" StrongPassword1!")]
    [InlineData("StrongPassword1! ")]
    public async Task ChangeAsync_WhenPasswordIsWeak_ReturnsWeakPassword(
        string weakPassword)
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangeTemporaryPasswordService service =
            new(
                repository,
                passwordService);

        ChangeTemporaryPasswordCommand command =
            new(
                UserId: 1,
                NewPassword: weakPassword,
                ConfirmNewPassword: weakPassword,
                RequestContext:
                    CreateValidRequestContext());

        // Act
        ChangeTemporaryPasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);

        Assert.Equal(
            ChangeTemporaryPasswordErrorCode
                .WeakPassword,
            result.ErrorCode);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Fact]
    public async Task ChangeAsync_WhenPasswordIsTooLong_ReturnsWeakPassword()
    {
        // Arrange
        string password =
            new string('a', 129);

        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangeTemporaryPasswordService service =
            new(
                repository,
                passwordService);

        ChangeTemporaryPasswordCommand command =
            new(
                UserId: 1,
                NewPassword: password,
                ConfirmNewPassword: password,
                RequestContext:
                    CreateValidRequestContext());

        // Act
        ChangeTemporaryPasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);

        Assert.Equal(
            ChangeTemporaryPasswordErrorCode
                .WeakPassword,
            result.ErrorCode);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Fact]
    public async Task ChangeAsync_WhenRequestIsValid_ChangesPasswordSuccessfully()
    {
        // Arrange
        DateTime passwordChangedAtUtc =
            new(
                year: 2026,
                month: 8,
                day: 1,
                hour: 6,
                minute: 15,
                second: 0,
                kind: DateTimeKind.Utc);

        FakeAuthenticationRepository repository =
            new()
            {
                TemporaryPasswordChangeToReturn =
                    new TemporaryPasswordChangeData
                    {
                        UserId = 1,
                        PasswordChangedAtUtc =
                            passwordChangedAtUtc,
                        RequiresPasswordChange =
                            false
                    }
            };

        FakePasswordService passwordService =
            new()
            {
                HashToReturn =
                    "generated-secure-password-hash"
            };

        ChangeTemporaryPasswordService service =
            new(
                repository,
                passwordService);

        AuthenticationRequestContext requestContext =
            CreateValidRequestContext();

        ChangeTemporaryPasswordCommand command =
            new(
                UserId: 1,
                NewPassword: "StrongPassword1!",
                ConfirmNewPassword: "StrongPassword1!",
                RequestContext: requestContext);

        // Act
        ChangeTemporaryPasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);

        Assert.Equal(
            ChangeTemporaryPasswordErrorCode.None,
            result.ErrorCode);

        Assert.Equal(
            passwordChangedAtUtc,
            result.PasswordChangedAtUtc);

        Assert.Equal(
            1,
            passwordService.HashPasswordCallCount);

        Assert.Equal(
            "StrongPassword1!",
            passwordService.PasswordReceivedForHash);

        Assert.Equal(
            1,
            repository.ChangeTemporaryPasswordCallCount);

        Assert.Equal(
            1,
            repository.RequestedPasswordChangeUserId);

        Assert.Equal(
            "generated-secure-password-hash",
            repository.RequestedNewPasswordHash);

        Assert.Same(
            requestContext,
            repository.RequestedPasswordChangeContext);
    }

    [Fact]
    public async Task ChangeAsync_WhenRepositoryReturnsDifferentUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                TemporaryPasswordChangeToReturn =
                    new TemporaryPasswordChangeData
                    {
                        UserId = 999,
                        PasswordChangedAtUtc =
                            DateTime.UtcNow,
                        RequiresPasswordChange =
                            false
                    }
            };

        FakePasswordService passwordService =
            new();

        ChangeTemporaryPasswordService service =
            new(
                repository,
                passwordService);

        ChangeTemporaryPasswordCommand command =
            CreateValidCommand();

        // Act and assert
        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () => service.ChangeAsync(
                        command,
                        CancellationToken.None));

        Assert.Contains(
            "unexpected UserId",
            exception.Message);
    }

    [Fact]
    public async Task ChangeAsync_WhenPasswordChangeFlagRemainsEnabled_ThrowsInvalidOperationException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                TemporaryPasswordChangeToReturn =
                    new TemporaryPasswordChangeData
                    {
                        UserId = 1,
                        PasswordChangedAtUtc =
                            DateTime.UtcNow,
                        RequiresPasswordChange =
                            true
                    }
            };

        FakePasswordService passwordService =
            new();

        ChangeTemporaryPasswordService service =
            new(
                repository,
                passwordService);

        ChangeTemporaryPasswordCommand command =
            CreateValidCommand();

        // Act and assert
        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () => service.ChangeAsync(
                        command,
                        CancellationToken.None));

        Assert.Contains(
            "flag was not removed",
            exception.Message);
    }

    private static AuthenticationRequestContext
        CreateValidRequestContext()
    {
        return new AuthenticationRequestContext(
            CorrelationId: Guid.NewGuid(),
            ClientIpAddress: "127.0.0.1",
            UserAgent: "LithoManager.UnitTests",
            RequestPath:
                "/api/auth/change-temporary-password");
    }

    private static ChangeTemporaryPasswordCommand
        CreateValidCommand()
    {
        return new ChangeTemporaryPasswordCommand(
            UserId: 1,
            NewPassword: "StrongPassword1!",
            ConfirmNewPassword: "StrongPassword1!",
            RequestContext:
                CreateValidRequestContext());
    }

    private static void
        AssertNoPasswordChangeExecution(
            FakeAuthenticationRepository repository,
            FakePasswordService passwordService)
    {
        Assert.Equal(
            0,
            passwordService.HashPasswordCallCount);

        Assert.Null(
            passwordService.PasswordReceivedForHash);

        Assert.Equal(
            0,
            repository.ChangeTemporaryPasswordCallCount);

        Assert.Null(
            repository.RequestedPasswordChangeUserId);

        Assert.Null(
            repository.RequestedNewPasswordHash);
    }
}