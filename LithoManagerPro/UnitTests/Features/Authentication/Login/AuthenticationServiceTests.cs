using LithoManager.Application.Features.Authentication.Login;
using LithoManager.UnitTests.TestDoubles.Persistence;
using LithoManager.UnitTests.TestDoubles.Security;
using LithoManager.UnitTests.TestDoubles.Time;
using Xunit;

namespace LithoManager.UnitTests.Features.Authentication.Login;

public sealed class AuthenticationServiceTests
{
    private static readonly DateTimeOffset UtcNow =
        new(
            year: 2026,
            month: 8,
            day: 1,
            hour: 6,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

    [Fact]
    public async Task LoginAsync_WhenCommandIsNull_ThrowsArgumentNullException()
    {
        AuthenticationService service =
            CreateService(
                new FakeAuthenticationRepository(),
                new FakePasswordService(),
                new FakeTokenService());

        await Assert.ThrowsAsync<
            ArgumentNullException>(
                () => service.LoginAsync(
                    null!,
                    CancellationToken.None));
    }

    [Theory]
    [InlineData("", "StrongPassword1!")]
    [InlineData("   ", "StrongPassword1!")]
    [InlineData("admin@lithomanager.com", "")]
    [InlineData("admin@lithomanager.com", "   ")]
    public async Task LoginAsync_WhenInputIsInvalid_ReturnsInvalidRequest(
        string emailAddress,
        string password)
    {
        FakeAuthenticationRepository repository =
            new();

        FakePasswordService passwordService =
            new();

        FakeTokenService tokenService =
            new();

        AuthenticationService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        LoginCommand command =
            CreateCommand(
                emailAddress,
                password);

        LoginResult result =
            await service.LoginAsync(
                command,
                CancellationToken.None);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            LoginErrorCode.InvalidRequest,
            result.ErrorCode);

        Assert.Equal(
            0,
            repository.GetUserForAuthenticationCallCount);

        Assert.Equal(
            0,
            passwordService.VerifyPasswordCallCount);

        AssertNoTokensGenerated(tokenService);
    }

    [Fact]
    public async Task LoginAsync_WhenCorrelationIdIsEmpty_ThrowsArgumentException()
    {
        FakeAuthenticationRepository repository =
            new();

        AuthenticationService service =
            CreateService(
                repository,
                new FakePasswordService(),
                new FakeTokenService());

        LoginCommand command =
            new(
                EmailAddress:
                    "admin@lithomanager.com",
                Password:
                    "StrongPassword1!",
                RequestContext:
                    new AuthenticationRequestContext(
                        CorrelationId: Guid.Empty,
                        ClientIpAddress: "127.0.0.1",
                        UserAgent: "UnitTests",
                        RequestPath:
                            "/api/auth/login"));

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.LoginAsync(
                command,
                CancellationToken.None));

        Assert.Equal(
            0,
            repository.GetUserForAuthenticationCallCount);
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_RegistersFailedLogin()
    {
        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn = null
            };

        AuthenticationService service =
            CreateService(
                repository,
                new FakePasswordService(),
                new FakeTokenService());

        AuthenticationRequestContext requestContext =
            CreateRequestContext();

        LoginCommand command =
            new(
                EmailAddress:
                    "  missing@lithomanager.com  ",
                Password:
                    "StrongPassword1!",
                RequestContext:
                    requestContext);

        LoginResult result =
            await service.LoginAsync(
                command,
                CancellationToken.None);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            LoginErrorCode.InvalidCredentials,
            result.ErrorCode);

        Assert.Equal(
            "missing@lithomanager.com",
            repository.RequestedEmailAddress);

        Assert.Equal(
            1,
            repository.RegisterFailedLoginCallCount);

        Assert.Equal(
            "missing@lithomanager.com",
            repository.FailedLoginEmailAddress);

        Assert.Null(repository.FailedLoginUserId);

        Assert.Equal(
            (short?)4,
            repository.FailedLoginMaximumAttempts);

        Assert.Equal(
            (int?)20,
            repository
                .FailedLoginLockoutDurationMinutes);

        Assert.Same(
            requestContext,
            repository.FailedLoginRequestContext);

        Assert.Equal(
            0,
            repository.RegisterSuccessfulLoginCallCount);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_ReturnsInvalidCredentials()
    {
        AuthenticationUserData user =
            CreateValidUser();

        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn = user,
                FailedLoginToReturn =
                    new FailedLoginRegistrationData
                    {
                        UserId = user.UserId,
                        FailedLoginAttempts = 1,
                        LockoutEndAtUtc = null,
                        IsLockedOut = false
                    }
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = false
            };

        FakeTokenService tokenService =
            new();

        AuthenticationService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        LoginResult result =
            await service.LoginAsync(
                CreateCommand(),
                CancellationToken.None);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            LoginErrorCode.InvalidCredentials,
            result.ErrorCode);

        Assert.Equal(
            1,
            passwordService.VerifyPasswordCallCount);

        Assert.Equal(
            user.PasswordHash,
            passwordService
                .PasswordHashReceivedForVerification);

        Assert.Equal(
            "StrongPassword1!",
            passwordService
                .ProvidedPasswordReceived);

        Assert.Equal(
            user.UserId,
            repository.FailedLoginUserId);

        Assert.Equal(
            (short?)4,
            repository.FailedLoginMaximumAttempts);

        Assert.Equal(
            (int?)20,
            repository
                .FailedLoginLockoutDurationMinutes);

        Assert.Equal(
            1,
            repository.RegisterFailedLoginCallCount);

        Assert.Equal(
            0,
            repository.RegisterSuccessfulLoginCallCount);

        AssertNoTokensGenerated(tokenService);
    }

    [Fact]
    public async Task LoginAsync_WhenFailedAttemptLocksAccount_ReturnsAccountLocked()
    {
        DateTime lockoutEndAtUtc =
            UtcNow
                .AddMinutes(15)
                .UtcDateTime;

        AuthenticationUserData user =
            CreateValidUser();

        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn = user,
                FailedLoginToReturn =
                    new FailedLoginRegistrationData
                    {
                        UserId = user.UserId,
                        FailedLoginAttempts = 5,
                        LockoutEndAtUtc =
                            lockoutEndAtUtc,
                        IsLockedOut = true
                    }
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = false
            };

        AuthenticationService service =
            CreateService(
                repository,
                passwordService,
                new FakeTokenService());

        LoginResult result =
            await service.LoginAsync(
                CreateCommand(),
                CancellationToken.None);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            LoginErrorCode.AccountLocked,
            result.ErrorCode);

        Assert.Equal(
            lockoutEndAtUtc,
            result.LockoutEndAtUtc);
    }

    [Fact]
    public async Task LoginAsync_WhenAccountIsAlreadyLocked_ReturnsAccountLocked()
    {
        DateTime lockoutEndAtUtc =
            UtcNow
                .AddMinutes(10)
                .UtcDateTime;

        AuthenticationUserData user =
            CopyUser(
                CreateValidUser(),
                lockoutEndAtUtc:
                    lockoutEndAtUtc);

        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn = user
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = true
            };

        FakeTokenService tokenService =
            new();

        AuthenticationService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        LoginResult result =
            await service.LoginAsync(
                CreateCommand(),
                CancellationToken.None);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            LoginErrorCode.AccountLocked,
            result.ErrorCode);

        Assert.Equal(
            lockoutEndAtUtc,
            result.LockoutEndAtUtc);

        Assert.Equal(
            0,
            repository.RegisterSuccessfulLoginCallCount);

        AssertNoTokensGenerated(tokenService);
    }

    [Theory]
    [InlineData(
        LoginErrorCode.AccountInactive,
        false,
        true,
        true,
        true)]
    [InlineData(
        LoginErrorCode.EmailNotConfirmed,
        true,
        false,
        true,
        true)]
    [InlineData(
        LoginErrorCode.RoleInactive,
        true,
        true,
        false,
        true)]
    [InlineData(
        LoginErrorCode.EmployeeInactive,
        true,
        true,
        true,
        false)]
    public async Task LoginAsync_WhenAccountStateIsInvalid_ReturnsExpectedError(
        LoginErrorCode expectedError,
        bool isActive,
        bool isEmailConfirmed,
        bool isRoleActive,
        bool isEmployeeActive)
    {
        AuthenticationUserData user =
            CopyUser(
                CreateValidEmployeeUser(),
                isActive:
                    isActive,
                isEmailConfirmed:
                    isEmailConfirmed,
                isRoleActive:
                    isRoleActive,
                isEmployeeActive:
                    isEmployeeActive);

        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn = user
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = true
            };

        FakeTokenService tokenService =
            new();

        AuthenticationService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        LoginResult result =
            await service.LoginAsync(
                CreateCommand(),
                CancellationToken.None);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            expectedError,
            result.ErrorCode);

        Assert.Equal(
            0,
            repository.RegisterSuccessfulLoginCallCount);

        AssertNoTokensGenerated(tokenService);
    }

    [Fact]
    public async Task LoginAsync_WhenTemporaryPasswordIsExpired_ReturnsTemporaryPasswordExpired()
    {
        AuthenticationUserData user =
            CopyUser(
                CreateValidUser(),
                requiresPasswordChange: true,
                temporaryPasswordExpiresAtUtc:
                    UtcNow
                        .AddSeconds(-1)
                        .UtcDateTime);

        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn = user
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = true
            };

        FakeTokenService tokenService =
            new();

        AuthenticationService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        LoginResult result =
            await service.LoginAsync(
                CreateCommand(),
                CancellationToken.None);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            LoginErrorCode.TemporaryPasswordExpired,
            result.ErrorCode);

        Assert.Equal(
            0,
            repository.RegisterSuccessfulLoginCallCount);

        AssertNoTokensGenerated(tokenService);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordChangeIsRequired_ReturnsPasswordChangeToken()
    {
        AuthenticationUserData user =
            CopyUser(
                CreateValidUser(),
                requiresPasswordChange: true,
                temporaryPasswordExpiresAtUtc:
                    UtcNow
                        .AddMinutes(30)
                        .UtcDateTime);

        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn = user
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = true
            };

        FakeTokenService tokenService =
            new();

        AuthenticationService service =
            CreateService(
                repository,
                passwordService,
                tokenService);

        LoginResult result =
            await service.LoginAsync(
                CreateCommand(),
                CancellationToken.None);

        Assert.True(result.IsSuccessful);
        Assert.True(result.RequiresPasswordChange);

        Assert.Null(result.AccessToken);

        Assert.Equal(
            "fake-password-change-token",
            result.PasswordChangeToken);

        Assert.Equal(
            1,
            repository.RegisterSuccessfulLoginCallCount);

        Assert.Equal(
            1,
            tokenService
                .GeneratePasswordChangeTokenCallCount);

        Assert.Equal(
            0,
            tokenService.GenerateAccessTokenCallCount);

        Assert.NotNull(
            tokenService.PasswordChangeTokenUserReceived);

        Assert.Equal(
            user.UserId,
            tokenService
                .PasswordChangeTokenUserReceived!
                .UserId);

        Assert.Equal(
            user.EmailAddress,
            tokenService
                .PasswordChangeTokenUserReceived
                .EmailAddress);

        Assert.Equal(
            user.TokenVersion,
            tokenService
                .PasswordChangeTokenUserReceived
                .TokenVersion);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsAccessTokenAndUser()
    {
        AuthenticationUserData user =
            CreateValidEmployeeUser();

        FakeAuthenticationRepository repository =
            new()
            {
                AuthenticationUserToReturn = user,
                CreateRefreshTokenToReturn =
                    new()
                    {
                        RefreshTokenId = 1,
                        UserId = user.UserId,
                        TokenFamilyId = Guid.NewGuid(),
                        TokenVersion =
                            user.TokenVersion,
                        ExpiresAtUtc =
                            UtcNow
                                .AddDays(1)
                                .UtcDateTime,
                        CreatedAtUtc =
                            UtcNow.UtcDateTime
                    }
            };

        FakePasswordService passwordService =
            new()
            {
                VerifyPasswordResult = true
            };

        FakeTokenService tokenService =
            new();

        FakeRefreshTokenService refreshTokenService =
            new()
            {
                GeneratedTokenToReturn =
                    new(
                        token:
                            "fake-refresh-token",
                        tokenHash:
                            Enumerable
                                .Repeat(
                                    (byte)7,
                                    32)
                                .ToArray())
            };

        AuthenticationService service =
            CreateService(
                repository,
                passwordService,
                tokenService,
                refreshTokenService);

        AuthenticationRequestContext requestContext =
            CreateRequestContext();

        LoginResult result =
            await service.LoginAsync(
                new LoginCommand(
                    EmailAddress:
                        user.EmailAddress,
                    Password:
                        "StrongPassword1!",
                    RequestContext:
                        requestContext),
                CancellationToken.None);

        Assert.True(result.IsSuccessful);

        Assert.Equal(
            LoginErrorCode.None,
            result.ErrorCode);

        Assert.False(result.RequiresPasswordChange);

        Assert.Equal(
            "fake-access-token",
            result.AccessToken);

        Assert.Null(result.PasswordChangeToken);

        Assert.Equal(
            "fake-refresh-token",
            result.RefreshToken);

        Assert.Equal(
            UtcNow.AddDays(1).UtcDateTime,
            result.RefreshTokenExpiresAtUtc);

        Assert.NotNull(result.User);

        Assert.Equal(
            user.UserId,
            result.User!.UserId);

        Assert.Equal(
            user.RoleCode,
            result.User.RoleCode);

        Assert.Equal(
            user.EmployeeId,
            result.User.EmployeeId);

        Assert.Equal(
            user.DepartmentCode,
            result.User.DepartmentCode);

        Assert.Equal(
            1,
            repository.RegisterSuccessfulLoginCallCount);

        Assert.Equal(
            user.UserId,
            repository.SuccessfulLoginUserId);

        Assert.Same(
            requestContext,
            repository.SuccessfulLoginRequestContext);

        Assert.Equal(
            1,
            tokenService.GenerateAccessTokenCallCount);

        Assert.Equal(
            1,
            refreshTokenService.GenerateTokenCallCount);

        Assert.Equal(
            1,
            repository.CreateRefreshTokenCallCount);

        Assert.Equal(
            user.UserId,
            repository.LastRefreshTokenUserId);

        Assert.Equal(
            UtcNow.AddDays(1).UtcDateTime,
            repository.LastRefreshTokenExpiresAtUtc);

        Assert.Equal(
            refreshTokenService
                .GeneratedTokenToReturn
                .TokenHash,
            repository.LastCreatedRefreshTokenHash);

        Assert.Equal(
            0,
            tokenService
                .GeneratePasswordChangeTokenCallCount);

        Assert.NotNull(
            tokenService.AccessTokenUserReceived);

        Assert.Equal(
            user.UserId,
            tokenService.AccessTokenUserReceived!.UserId);

        Assert.Equal(
            user.EmailAddress,
            tokenService.AccessTokenUserReceived
                .EmailAddress);

        Assert.Equal(
            user.TokenVersion,
            tokenService.AccessTokenUserReceived
                .TokenVersion);

        Assert.Equal(
            user.RoleCode,
            tokenService.AccessTokenUserReceived
                .RoleCode);

        Assert.Equal(
            user.EmployeeId,
            tokenService.AccessTokenUserReceived
                .EmployeeId);
    }

    private static AuthenticationService CreateService(
        FakeAuthenticationRepository repository,
        FakePasswordService passwordService,
        FakeTokenService tokenService,
        FakeRefreshTokenService? refreshTokenService = null)
    {
        return new AuthenticationService(
            repository,
            passwordService,
            tokenService,
            refreshTokenService
                ?? new FakeRefreshTokenService(),
            new LithoManager.Application.Features.Authentication
                .AuthenticationSessionOptions
            {
                RefreshTokenExpirationDays = 1
            },
            new LithoManager.Application.Features.Authentication
                .AuthenticationSecurityOptions
            {
                MaximumFailedLoginAttempts = 4,
                LockoutDurationMinutes = 20,
                PasswordResetTokenExpirationMinutes = 15
            },
            new FixedTimeProvider(UtcNow));
    }

    private static LoginCommand CreateCommand(
        string emailAddress =
            "admin@lithomanager.com",
        string password =
            "StrongPassword1!")
    {
        return new LoginCommand(
            EmailAddress: emailAddress,
            Password: password,
            RequestContext:
                CreateRequestContext());
    }

    private static AuthenticationRequestContext
        CreateRequestContext()
    {
        return new AuthenticationRequestContext(
            CorrelationId: Guid.NewGuid(),
            ClientIpAddress: "127.0.0.1",
            UserAgent: "LithoManager.UnitTests",
            RequestPath: "/api/auth/login");
    }

    private static AuthenticationUserData
        CreateValidUser()
    {
        return new AuthenticationUserData
        {
            UserId = 1,
            EmailAddress =
                "admin@lithomanager.com",
            PasswordHash =
                "stored-password-hash",
            TokenVersion = 1,

            IsEmailConfirmed = true,
            IsActive = true,
            RequiresPasswordChange = false,

            TemporaryPasswordExpiresAtUtc = null,
            PasswordChangedAtUtc =
                UtcNow.AddDays(-1).UtcDateTime,

            FailedLoginAttempts = 0,
            LockoutEndAtUtc = null,
            LastLoginAtUtc = null,

            RoleId = 1,
            RoleCode =
                "SuperAdministrator",
            RoleDisplayName =
                "Super Administrator",
            IsRoleActive = true,

            EmployeeId = null,
            FirstName = null,
            LastName = null,
            JobTitle = null,
            ProfileImagePath = null,
            IsEmployeeActive = null,

            DepartmentId = null,
            DepartmentCode = null,
            DepartmentName = null
        };
    }

    private static AuthenticationUserData
        CreateValidEmployeeUser()
    {
        return new AuthenticationUserData
        {
            UserId = 2,
            EmailAddress =
                "employee@lithomanager.com",
            PasswordHash =
                "stored-password-hash",
            TokenVersion = 1,

            IsEmailConfirmed = true,
            IsActive = true,
            RequiresPasswordChange = false,

            TemporaryPasswordExpiresAtUtc = null,
            PasswordChangedAtUtc =
                UtcNow.AddDays(-1).UtcDateTime,

            FailedLoginAttempts = 0,
            LockoutEndAtUtc = null,
            LastLoginAtUtc = null,

            RoleId = 4,
            RoleCode = "Employee",
            RoleDisplayName = "Employee",
            IsRoleActive = true,

            EmployeeId = 10,
            FirstName = "Ana",
            LastName = "Rodriguez",
            JobTitle = "Business Analyst",
            ProfileImagePath =
                "/profiles/employee-10.jpg",
            IsEmployeeActive = true,

            DepartmentId = 3,
            DepartmentCode =
                "INFORMATION_TECHNOLOGY",
            DepartmentName =
                "Information Technology"
        };
    }

    private static AuthenticationUserData CopyUser(
        AuthenticationUserData source,
        bool? isActive = null,
        bool? isEmailConfirmed = null,
        bool? isRoleActive = null,
        bool? isEmployeeActive = null,
        bool? requiresPasswordChange = null,
        DateTime? temporaryPasswordExpiresAtUtc = null,
        DateTime? lockoutEndAtUtc = null)
    {
        return new AuthenticationUserData
        {
            UserId = source.UserId,
            EmailAddress = source.EmailAddress,
            PasswordHash = source.PasswordHash,
            TokenVersion = source.TokenVersion,

            IsEmailConfirmed =
                isEmailConfirmed
                ?? source.IsEmailConfirmed,

            IsActive =
                isActive
                ?? source.IsActive,

            RequiresPasswordChange =
                requiresPasswordChange
                ?? source.RequiresPasswordChange,

            TemporaryPasswordExpiresAtUtc =
                temporaryPasswordExpiresAtUtc
                ?? source.TemporaryPasswordExpiresAtUtc,

            PasswordChangedAtUtc =
                source.PasswordChangedAtUtc,

            FailedLoginAttempts =
                source.FailedLoginAttempts,

            LockoutEndAtUtc =
                lockoutEndAtUtc
                ?? source.LockoutEndAtUtc,

            LastLoginAtUtc =
                source.LastLoginAtUtc,

            RoleId = source.RoleId,
            RoleCode = source.RoleCode,
            RoleDisplayName =
                source.RoleDisplayName,

            IsRoleActive =
                isRoleActive
                ?? source.IsRoleActive,

            EmployeeId = source.EmployeeId,
            FirstName = source.FirstName,
            LastName = source.LastName,
            JobTitle = source.JobTitle,
            ProfileImagePath =
                source.ProfileImagePath,

            IsEmployeeActive =
                isEmployeeActive
                ?? source.IsEmployeeActive,

            DepartmentId = source.DepartmentId,
            DepartmentCode =
                source.DepartmentCode,
            DepartmentName =
                source.DepartmentName
        };
    }

    private static void AssertNoTokensGenerated(
        FakeTokenService tokenService)
    {
        Assert.Equal(
            0,
            tokenService.GenerateAccessTokenCallCount);

        Assert.Equal(
            0,
            tokenService
                .GeneratePasswordChangeTokenCallCount);
    }
}
