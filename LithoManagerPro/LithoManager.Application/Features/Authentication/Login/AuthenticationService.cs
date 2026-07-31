using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Abstractions.Security;

namespace LithoManager.Application.Features.Authentication.Login;

public sealed class AuthenticationService
    : IAuthenticationService
{
    private const int MaximumEmailAddressLength = 254;
    private const int MaximumPasswordLength = 1024;

    private readonly IAuthenticationRepository
        _authenticationRepository;

    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public AuthenticationService(
        IAuthenticationRepository authenticationRepository,
        IPasswordService passwordService,
        ITokenService tokenService,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            authenticationRepository);

        ArgumentNullException.ThrowIfNull(passwordService);
        ArgumentNullException.ThrowIfNull(tokenService);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _authenticationRepository =
            authenticationRepository;

        _passwordService = passwordService;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<LoginResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        string emailAddress =
            command.EmailAddress?.Trim()
            ?? string.Empty;

        string password =
            command.Password
            ?? string.Empty;

        if (!IsValidInput(emailAddress, password))
        {
            return LoginResult.Failure(
                LoginErrorCode.InvalidRequest);
        }

        if (command.RequestContext.CorrelationId
            == Guid.Empty)
        {
            throw new ArgumentException(
                "CorrelationId cannot be empty.",
                nameof(command));
        }

        AuthenticationUserData? user =
            await _authenticationRepository
                .GetUserForAuthenticationAsync(
                    emailAddress,
                    cancellationToken);

        if (user is null)
        {
            await _authenticationRepository
                .RegisterFailedLoginAsync(
                    attemptedEmailAddress: emailAddress,
                    userId: null,
                    requestContext:
                        command.RequestContext,
                    cancellationToken:
                        cancellationToken);

            return LoginResult.Failure(
                LoginErrorCode.InvalidCredentials);
        }

        bool isPasswordValid =
            _passwordService.VerifyPassword(
                user.PasswordHash,
                password);

        if (!isPasswordValid)
        {
            FailedLoginRegistrationData failedLogin =
                await _authenticationRepository
                    .RegisterFailedLoginAsync(
                        attemptedEmailAddress:
                            emailAddress,
                        userId: user.UserId,
                        requestContext:
                            command.RequestContext,
                        cancellationToken:
                            cancellationToken);

            if (failedLogin.IsLockedOut)
            {
                return LoginResult.Failure(
                    LoginErrorCode.AccountLocked,
                    failedLogin.LockoutEndAtUtc);
            }

            return LoginResult.Failure(
                LoginErrorCode.InvalidCredentials);
        }

        DateTime utcNow =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        LoginResult? accountValidationResult =
            ValidateAccount(user, utcNow);

        if (accountValidationResult is not null)
        {
            return accountValidationResult;
        }

        await _authenticationRepository
            .RegisterSuccessfulLoginAsync(
                userId: user.UserId,
                requestContext:
                    command.RequestContext,
                cancellationToken:
                    cancellationToken);

        LoginUserData loginUser =
            MapLoginUser(user);

        if (user.RequiresPasswordChange)
        {
            PasswordChangeTokenResult
                passwordChangeToken =
                    _tokenService
                        .GeneratePasswordChangeToken(
                            new PasswordChangeTokenUserData(
                                UserId: user.UserId,
                                EmailAddress:
                                    user.EmailAddress));

            return LoginResult.PasswordChangeRequired(
                loginUser,
                passwordChangeToken);
        }

        AccessTokenResult accessToken =
            _tokenService.GenerateAccessToken(
                new AccessTokenUserData(
                    UserId: user.UserId,
                    EmailAddress:
                        user.EmailAddress,
                    RoleCode:
                        user.RoleCode,
                    EmployeeId:
                        user.EmployeeId));

        return LoginResult.Success(
            loginUser,
            accessToken);
    }

    private static bool IsValidInput(
        string emailAddress,
        string password)
    {
        if (string.IsNullOrWhiteSpace(emailAddress)
            || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        if (emailAddress.Length
            > MaximumEmailAddressLength)
        {
            return false;
        }

        if (password.Length
            > MaximumPasswordLength)
        {
            return false;
        }

        return true;
    }

    private static LoginResult? ValidateAccount(
        AuthenticationUserData user,
        DateTime utcNow)
    {
        if (user.LockoutEndAtUtc is DateTime
            lockoutEndAtUtc
            && lockoutEndAtUtc > utcNow)
        {
            return LoginResult.Failure(
                LoginErrorCode.AccountLocked,
                lockoutEndAtUtc);
        }

        if (!user.IsActive)
        {
            return LoginResult.Failure(
                LoginErrorCode.AccountInactive);
        }

        if (!user.IsEmailConfirmed)
        {
            return LoginResult.Failure(
                LoginErrorCode.EmailNotConfirmed);
        }

        if (!user.IsRoleActive)
        {
            return LoginResult.Failure(
                LoginErrorCode.RoleInactive);
        }

        if (user.EmployeeId.HasValue
            && user.IsEmployeeActive != true)
        {
            return LoginResult.Failure(
                LoginErrorCode.EmployeeInactive);
        }

        if (user.RequiresPasswordChange)
        {
            if (user.TemporaryPasswordExpiresAtUtc
                is not DateTime
                temporaryPasswordExpiresAtUtc
                || temporaryPasswordExpiresAtUtc
                    <= utcNow)
            {
                return LoginResult.Failure(
                    LoginErrorCode
                        .TemporaryPasswordExpired);
            }
        }

        return null;
    }

    private static LoginUserData MapLoginUser(
        AuthenticationUserData user)
    {
        return new LoginUserData(
            UserId: user.UserId,
            EmailAddress: user.EmailAddress,
            RoleCode: user.RoleCode,
            RoleDisplayName:
                user.RoleDisplayName,
            EmployeeId: user.EmployeeId,
            FirstName: user.FirstName,
            LastName: user.LastName,
            JobTitle: user.JobTitle,
            ProfileImagePath:
                user.ProfileImagePath,
            DepartmentId:
                user.DepartmentId,
            DepartmentCode:
                user.DepartmentCode,
            DepartmentName:
                user.DepartmentName);
    }
}