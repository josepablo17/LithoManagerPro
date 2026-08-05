using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Abstractions.Security;
using LithoManager.Application.Features.Authentication.Login;

namespace LithoManager.Application.Features.Authentication
    .ChangePassword;

public sealed class ChangePasswordService
    : IChangePasswordService
{
    private const int MinimumPasswordLength = 12;
    private const int MaximumPasswordLength = 128;
    private const int MaximumCurrentPasswordLength = 1024;

    private readonly IAuthenticationRepository
        _authenticationRepository;

    private readonly IPasswordService
        _passwordService;

    private readonly TimeProvider
        _timeProvider;

    public ChangePasswordService(
        IAuthenticationRepository authenticationRepository,
        IPasswordService passwordService,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            authenticationRepository);

        ArgumentNullException.ThrowIfNull(
            passwordService);

        ArgumentNullException.ThrowIfNull(
            timeProvider);

        _authenticationRepository =
            authenticationRepository;

        _passwordService =
            passwordService;

        _timeProvider =
            timeProvider;
    }

    public async Task<ChangePasswordResult> ChangeAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!IsValidRequest(command))
        {
            return ChangePasswordResult.Failure(
                ChangePasswordErrorCode.InvalidRequest);
        }

        if (!string.Equals(
                command.NewPassword,
                command.ConfirmNewPassword,
                StringComparison.Ordinal))
        {
            return ChangePasswordResult.Failure(
                ChangePasswordErrorCode
                    .PasswordsDoNotMatch);
        }

        if (!IsStrongPassword(
                command.NewPassword))
        {
            return ChangePasswordResult.Failure(
                ChangePasswordErrorCode.WeakPassword);
        }

        AuthenticationUserData? user =
            await _authenticationRepository
                .GetUserForAuthenticationByIdAsync(
                    command.UserId,
                    cancellationToken);

        if (user is null)
        {
            return ChangePasswordResult.Failure(
                ChangePasswordErrorCode
                    .AccessNotAvailable);
        }

        if (user.UserId != command.UserId)
        {
            throw new InvalidOperationException(
                "The authentication lookup returned an unexpected UserId.");
        }

        DateTime utcNow =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        if (!IsAccessAvailable(
                user,
                utcNow))
        {
            return ChangePasswordResult.Failure(
                ChangePasswordErrorCode
                    .AccessNotAvailable);
        }

        bool isCurrentPasswordValid =
            _passwordService.VerifyPassword(
                user.PasswordHash,
                command.CurrentPassword);

        if (!isCurrentPasswordValid)
        {
            return ChangePasswordResult.Failure(
                ChangePasswordErrorCode
                    .CurrentPasswordInvalid);
        }

        bool isPasswordReused =
            _passwordService.VerifyPassword(
                user.PasswordHash,
                command.NewPassword);

        if (isPasswordReused)
        {
            return ChangePasswordResult.Failure(
                ChangePasswordErrorCode
                    .PasswordReuseNotAllowed);
        }

        string newPasswordHash =
            _passwordService.HashPassword(
                command.NewPassword);

        ChangePasswordData result =
            await _authenticationRepository
                .ChangePasswordAsync(
                    userId:
                        command.UserId,
                    newPasswordHash:
                        newPasswordHash,
                    requestContext:
                        command.RequestContext,
                    cancellationToken:
                        cancellationToken);

        if (result.UserId != command.UserId)
        {
            throw new InvalidOperationException(
                "The password change returned an unexpected UserId.");
        }

        if (result.RequiresPasswordChange)
        {
            throw new InvalidOperationException(
                "The voluntary password change returned an invalid temporary password flag.");
        }

        return ChangePasswordResult.Success(
            result.PasswordChangedAtUtc);
    }

    private static bool IsValidRequest(
        ChangePasswordCommand command)
    {
        if (command.UserId <= 0)
        {
            return false;
        }

        if (command.RequestContext.CorrelationId
            == Guid.Empty)
        {
            return false;
        }

        if (string.IsNullOrEmpty(
                command.CurrentPassword)
            || string.IsNullOrEmpty(
                command.NewPassword)
            || string.IsNullOrEmpty(
                command.ConfirmNewPassword))
        {
            return false;
        }

        if (command.CurrentPassword.Length
            > MaximumCurrentPasswordLength)
        {
            return false;
        }

        return true;
    }

    private static bool IsAccessAvailable(
        AuthenticationUserData user,
        DateTime utcNow)
    {
        if (!user.IsActive)
        {
            return false;
        }

        if (!user.IsEmailConfirmed)
        {
            return false;
        }

        if (!user.IsRoleActive)
        {
            return false;
        }

        if (user.EmployeeId.HasValue
            && user.IsEmployeeActive != true)
        {
            return false;
        }

        if (user.RequiresPasswordChange)
        {
            return false;
        }

        if (user.LockoutEndAtUtc is DateTime
            lockoutEndAtUtc
            && lockoutEndAtUtc > utcNow)
        {
            return false;
        }

        return true;
    }

    private static bool IsStrongPassword(
        string password)
    {
        if (password.Length
                < MinimumPasswordLength
            || password.Length
                > MaximumPasswordLength)
        {
            return false;
        }

        if (char.IsWhiteSpace(
                password[0])
            || char.IsWhiteSpace(
                password[^1]))
        {
            return false;
        }

        bool hasUppercase =
            password.Any(char.IsUpper);

        bool hasLowercase =
            password.Any(char.IsLower);

        bool hasDigit =
            password.Any(char.IsDigit);

        bool hasSpecialCharacter =
            password.Any(
                character =>
                    !char.IsLetterOrDigit(
                        character)
                    && !char.IsWhiteSpace(
                        character));

        return hasUppercase
            && hasLowercase
            && hasDigit
            && hasSpecialCharacter;
    }
}