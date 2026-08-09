using LithoManager.Application.Features.Authentication
    .ChangePassword;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Security;
using LithoManager.UnitTests.TestDoubles.Persistence;
using LithoManager.UnitTests.TestDoubles.Security;
using LithoManager.UnitTests.TestDoubles.Time;
using Xunit;

namespace LithoManager.UnitTests.Features.Authentication
    .ChangePassword;

public sealed class ChangePasswordServiceTests
{
    private const string CurrentPassword =
        "CurrentPassword1!";

    private const string NewPassword =
        "NewStrongPassword1!";

    private const string StoredPasswordHash =
        "stored-password-hash";

    private static readonly DateTimeOffset UtcNow =
        new(
            year: 2026,
            month: 8,
            day: 4,
            hour: 20,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

    [Fact]
    public async Task ChangeAsync_WhenCommandIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangePasswordService service =
            CreateService(
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
            repository
                .GetUserForAuthenticationByIdCallCount);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Fact]
    public async Task ChangeAsync_WhenUserIdIsInvalid_ReturnsInvalidRequest()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        ChangePasswordCommand command =
            CreateValidCommand() with
            {
                UserId = 0
            };

        // Act
        ChangePasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ChangePasswordErrorCode.InvalidRequest);

        AssertNoRepositoryLookup(repository);

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

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        AuthenticationRequestContext requestContext =
            new(
                CorrelationId: Guid.Empty,
                ClientIpAddress: "127.0.0.1",
                UserAgent: "LithoManager.UnitTests",
                RequestPath:
                    "/api/auth/change-password");

        ChangePasswordCommand command =
            CreateValidCommand() with
            {
                RequestContext = requestContext
            };

        // Act
        ChangePasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ChangePasswordErrorCode.InvalidRequest);

        AssertNoRepositoryLookup(repository);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Theory]
    [InlineData("", "NewStrongPassword1!", "NewStrongPassword1!")]
    [InlineData("CurrentPassword1!", "", "")]
    [InlineData("CurrentPassword1!", "NewStrongPassword1!", "")]
    [InlineData("CurrentPassword1!", "", "NewStrongPassword1!")]
    public async Task ChangeAsync_WhenPasswordFieldsAreMissing_ReturnsInvalidRequest(
        string currentPassword,
        string newPassword,
        string confirmNewPassword)
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        ChangePasswordCommand command =
            new(
                UserId: 1,
                CurrentPassword: currentPassword,
                NewPassword: newPassword,
                ConfirmNewPassword:
                    confirmNewPassword,
                RequestContext:
                    CreateValidRequestContext());

        // Act
        ChangePasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ChangePasswordErrorCode.InvalidRequest);

        AssertNoRepositoryLookup(repository);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Fact]
    public async Task ChangeAsync_WhenCurrentPasswordIsTooLong_ReturnsInvalidRequest()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        ChangePasswordCommand command =
            CreateValidCommand() with
            {
                CurrentPassword =
                    new string('a', 1025)
            };

        // Act
        ChangePasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ChangePasswordErrorCode.InvalidRequest);

        AssertNoRepositoryLookup(repository);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Fact]
    public async Task ChangeAsync_WhenNewPasswordsDoNotMatch_ReturnsPasswordsDoNotMatch()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        ChangePasswordCommand command =
            CreateValidCommand() with
            {
                ConfirmNewPassword =
                    "DifferentPassword1!"
            };

        // Act
        ChangePasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ChangePasswordErrorCode
                .PasswordsDoNotMatch);

        AssertNoRepositoryLookup(repository);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Theory]
    [InlineData("Short1!")]
    [InlineData("newstrongpassword1!")]
    [InlineData("NEWSTRONGPASSWORD1!")]
    [InlineData("NewStrongPassword!")]
    [InlineData("NewStrongPassword123")]
    [InlineData(" NewStrongPassword1!")]
    [InlineData("NewStrongPassword1! ")]
    public async Task ChangeAsync_WhenNewPasswordIsWeak_ReturnsWeakPassword(
        string weakPassword)
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        ChangePasswordCommand command =
            CreateValidCommand() with
            {
                NewPassword = weakPassword,
                ConfirmNewPassword = weakPassword
            };

        // Act
        ChangePasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ChangePasswordErrorCode.WeakPassword);

        AssertNoRepositoryLookup(repository);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Fact]
    public async Task ChangeAsync_WhenNewPasswordIsTooLong_ReturnsWeakPassword()
    {
        // Arrange
        string password =
            new string('A', 126) + "a1!";

        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        ChangePasswordCommand command =
            CreateValidCommand() with
            {
                NewPassword = password,
                ConfirmNewPassword = password
            };

        // Act
        ChangePasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ChangePasswordErrorCode.WeakPassword);

        AssertNoRepositoryLookup(repository);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Fact]
    public async Task ChangeAsync_WhenUserDoesNotExist_ReturnsAccessNotAvailable()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn = null
            };

        FakePasswordService passwordService =
            new();

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        // Act
        ChangePasswordResult result =
            await service.ChangeAsync(
                CreateValidCommand(),
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ChangePasswordErrorCode
                .AccessNotAvailable);

        Assert.Equal(
            1,
            repository
                .GetUserForAuthenticationByIdCallCount);

        Assert.Equal(
            1,
            repository
                .RequestedAuthenticationUserId);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Theory]
    [InlineData("UserInactive")]
    [InlineData("EmailNotConfirmed")]
    [InlineData("RoleInactive")]
    [InlineData("EmployeeInactive")]
    [InlineData("PasswordChangeRequired")]
    [InlineData("AccountLocked")]
    public async Task ChangeAsync_WhenAccessIsUnavailable_ReturnsAccessNotAvailable(
        string scenario)
    {
        // Arrange
        AuthenticationUserData user =
            scenario switch
            {
                "UserInactive" =>
                    CreateValidUser(
                        isActive: false),

                "EmailNotConfirmed" =>
                    CreateValidUser(
                        isEmailConfirmed: false),

                "RoleInactive" =>
                    CreateValidUser(
                        isRoleActive: false),

                "EmployeeInactive" =>
                    CreateValidUser(
                        employeeId: 20,
                        isEmployeeActive: false),

                "PasswordChangeRequired" =>
                    CreateValidUser(
                        requiresPasswordChange: true),

                "AccountLocked" =>
                    CreateValidUser(
                        lockoutEndAtUtc:
                            UtcNow.UtcDateTime
                                .AddMinutes(10)),

                _ => throw new InvalidOperationException(
                    "The test scenario is not supported.")
            };

        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn = user
            };

        FakePasswordService passwordService =
            new();

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        // Act
        ChangePasswordResult result =
            await service.ChangeAsync(
                CreateValidCommand(),
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ChangePasswordErrorCode
                .AccessNotAvailable);

        Assert.Equal(
            1,
            repository
                .GetUserForAuthenticationByIdCallCount);

        Assert.Equal(
            0,
            passwordService.VerifyPasswordCallCount);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Fact]
    public async Task ChangeAsync_WhenAuthenticationLookupReturnsDifferentUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn =
                    CreateValidUser(
                        userId: 999)
            };

        FakePasswordService passwordService =
            new();

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        // Act and assert
        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () => service.ChangeAsync(
                        CreateValidCommand(),
                        CancellationToken.None));

        Assert.Contains(
            "unexpected UserId",
            exception.Message);

        Assert.Equal(
            0,
            passwordService.VerifyPasswordCallCount);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Fact]
    public async Task ChangeAsync_WhenCurrentPasswordIsInvalid_ReturnsCurrentPasswordInvalid()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn =
                    CreateValidUser()
            };

        FakePasswordService passwordService =
            new();

        passwordService
            .VerifyPasswordResultsToReturn
            .Enqueue(false);

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        // Act
        ChangePasswordResult result =
            await service.ChangeAsync(
                CreateValidCommand(),
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ChangePasswordErrorCode
                .CurrentPasswordInvalid);

        Assert.Equal(
            1,
            passwordService.VerifyPasswordCallCount);

        Assert.Single(
            passwordService.VerificationCalls);

        Assert.Equal(
            StoredPasswordHash,
            passwordService
                .VerificationCalls[0]
                .PasswordHash);

        Assert.Equal(
            CurrentPassword,
            passwordService
                .VerificationCalls[0]
                .ProvidedPassword);

        AssertNoPasswordChangeExecution(
            repository,
            passwordService);
    }

    [Fact]
    public async Task ChangeAsync_WhenNewPasswordMatchesCurrentPassword_ReturnsPasswordReuseNotAllowed()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn =
                    CreateValidUser()
            };

        FakePasswordService passwordService =
            new();

        passwordService
            .VerifyPasswordResultsToReturn
            .Enqueue(true);

        passwordService
            .VerifyPasswordResultsToReturn
            .Enqueue(true);

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        ChangePasswordCommand command =
            CreateValidCommand() with
            {
                NewPassword = CurrentPassword,
                ConfirmNewPassword = CurrentPassword
            };

        // Act
        ChangePasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        AssertFailure(
            result,
            ChangePasswordErrorCode
                .PasswordReuseNotAllowed);

        Assert.Equal(
            2,
            passwordService.VerifyPasswordCallCount);

        Assert.Equal(
            CurrentPassword,
            passwordService
                .VerificationCalls[0]
                .ProvidedPassword);

        Assert.Equal(
            CurrentPassword,
            passwordService
                .VerificationCalls[1]
                .ProvidedPassword);

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
                day: 4,
                hour: 20,
                minute: 5,
                second: 0,
                kind: DateTimeKind.Utc);

        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn =
                    CreateValidUser(),

                ChangePasswordToReturn =
                    new ChangePasswordData
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
                    "generated-new-password-hash"
            };

        passwordService
            .VerifyPasswordResultsToReturn
            .Enqueue(true);

        passwordService
            .VerifyPasswordResultsToReturn
            .Enqueue(false);

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        AuthenticationRequestContext requestContext =
            CreateValidRequestContext();

        ChangePasswordCommand command =
            CreateValidCommand() with
            {
                RequestContext = requestContext
            };

        // Act
        ChangePasswordResult result =
            await service.ChangeAsync(
                command,
                CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);

        Assert.Equal(
            ChangePasswordErrorCode.None,
            result.ErrorCode);

        Assert.Equal(
            passwordChangedAtUtc,
            result.PasswordChangedAtUtc);

        Assert.Equal(
            1,
            repository
                .GetUserForAuthenticationByIdCallCount);

        Assert.Equal(
            1,
            repository
                .RequestedAuthenticationUserId);

        Assert.Equal(
            2,
            passwordService.VerifyPasswordCallCount);

        Assert.Equal(
            CurrentPassword,
            passwordService
                .VerificationCalls[0]
                .ProvidedPassword);

        Assert.Equal(
            NewPassword,
            passwordService
                .VerificationCalls[1]
                .ProvidedPassword);

        Assert.Equal(
            1,
            passwordService.HashPasswordCallCount);

        Assert.Equal(
            NewPassword,
            passwordService.PasswordReceivedForHash);

        Assert.Equal(
            1,
            repository.ChangePasswordCallCount);

        Assert.Equal(
            1,
            repository
                .RequestedVoluntaryPasswordChangeUserId);

        Assert.Equal(
            "generated-new-password-hash",
            repository
                .RequestedVoluntaryNewPasswordHash);

        Assert.Same(
            requestContext,
            repository
                .RequestedVoluntaryPasswordChangeContext);
    }

    [Fact]
    public async Task ChangeAsync_WhenChangeReturnsDifferentUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            CreateRepositoryForSuccessfulExecution(
                new ChangePasswordData
                {
                    UserId = 999,
                    PasswordChangedAtUtc =
                        DateTime.UtcNow,
                    RequiresPasswordChange =
                        false
                });

        FakePasswordService passwordService =
            CreatePasswordServiceForSuccessfulExecution();

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        // Act and assert
        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () => service.ChangeAsync(
                        CreateValidCommand(),
                        CancellationToken.None));

        Assert.Contains(
            "unexpected UserId",
            exception.Message);

        Assert.Equal(
            1,
            repository.ChangePasswordCallCount);
    }

    [Fact]
    public async Task ChangeAsync_WhenTemporaryPasswordFlagIsReturned_ThrowsInvalidOperationException()
    {
        // Arrange
        FakeAuthenticationRepository repository =
            CreateRepositoryForSuccessfulExecution(
                new ChangePasswordData
                {
                    UserId = 1,
                    PasswordChangedAtUtc =
                        DateTime.UtcNow,
                    RequiresPasswordChange =
                        true
                });

        FakePasswordService passwordService =
            CreatePasswordServiceForSuccessfulExecution();

        ChangePasswordService service =
            CreateService(
                repository,
                passwordService);

        // Act and assert
        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () => service.ChangeAsync(
                        CreateValidCommand(),
                        CancellationToken.None));

        Assert.Contains(
            "temporary password flag",
            exception.Message);

        Assert.Equal(
            1,
            repository.ChangePasswordCallCount);
    }

    private static ChangePasswordService
        CreateService(
            FakeAuthenticationRepository repository,
            FakePasswordService passwordService)
    {
        return new ChangePasswordService(
            repository,
            passwordService,
            new PasswordPolicy(),
            new FixedTimeProvider(UtcNow));
    }

    private static AuthenticationUserData
        CreateValidUser(
            int userId = 1,
            bool isActive = true,
            bool isEmailConfirmed = true,
            bool isRoleActive = true,
            int? employeeId = null,
            bool? isEmployeeActive = null,
            bool requiresPasswordChange = false,
            DateTime? lockoutEndAtUtc = null)
    {
        return new AuthenticationUserData
        {
            UserId = userId,
            EmailAddress =
                "administrator@lithomanager.test",
            PasswordHash =
                StoredPasswordHash,
            TokenVersion = 1,
            IsEmailConfirmed =
                isEmailConfirmed,
            IsActive =
                isActive,
            RequiresPasswordChange =
                requiresPasswordChange,
            FailedLoginAttempts = 0,
            LockoutEndAtUtc =
                lockoutEndAtUtc,
            RoleId = 1,
            RoleCode =
                "SuperAdministrator",
            RoleDisplayName =
                "Super Administrator",
            IsRoleActive =
                isRoleActive,
            EmployeeId =
                employeeId,
            IsEmployeeActive =
                isEmployeeActive
        };
    }

    private static AuthenticationRequestContext
        CreateValidRequestContext()
    {
        return new AuthenticationRequestContext(
            CorrelationId: Guid.NewGuid(),
            ClientIpAddress: "127.0.0.1",
            UserAgent: "LithoManager.UnitTests",
            RequestPath:
                "/api/auth/change-password");
    }

    private static ChangePasswordCommand
        CreateValidCommand()
    {
        return new ChangePasswordCommand(
            UserId: 1,
            CurrentPassword:
                CurrentPassword,
            NewPassword:
                NewPassword,
            ConfirmNewPassword:
                NewPassword,
            RequestContext:
                CreateValidRequestContext());
    }

    private static FakeAuthenticationRepository
        CreateRepositoryForSuccessfulExecution(
            ChangePasswordData changePasswordData)
    {
        return new FakeAuthenticationRepository
        {
            AuthenticationUserToReturn =
                CreateValidUser(),

            ChangePasswordToReturn =
                changePasswordData
        };
    }

    private static FakePasswordService
        CreatePasswordServiceForSuccessfulExecution()
    {
        FakePasswordService passwordService =
            new();

        passwordService
            .VerifyPasswordResultsToReturn
            .Enqueue(true);

        passwordService
            .VerifyPasswordResultsToReturn
            .Enqueue(false);

        return passwordService;
    }

    private static void AssertFailure(
        ChangePasswordResult result,
        ChangePasswordErrorCode expectedErrorCode)
    {
        Assert.False(result.IsSuccessful);

        Assert.Equal(
            expectedErrorCode,
            result.ErrorCode);

        Assert.Null(
            result.PasswordChangedAtUtc);
    }

    private static void AssertNoRepositoryLookup(
        FakeAuthenticationRepository repository)
    {
        Assert.Equal(
            0,
            repository
                .GetUserForAuthenticationByIdCallCount);

        Assert.Null(
            repository
                .RequestedAuthenticationUserId);
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
            repository.ChangePasswordCallCount);

        Assert.Null(
            repository
                .RequestedVoluntaryPasswordChangeUserId);

        Assert.Null(
            repository
                .RequestedVoluntaryNewPasswordHash);

        Assert.Null(
            repository
                .RequestedVoluntaryPasswordChangeContext);
    }
}
