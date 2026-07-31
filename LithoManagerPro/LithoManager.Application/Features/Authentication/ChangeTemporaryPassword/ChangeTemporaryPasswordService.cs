using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Abstractions.Security;

namespace LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;

public sealed class ChangeTemporaryPasswordService
    : IChangeTemporaryPasswordService
{
    private const int MinimumPasswordLength = 12;
    private const int MaximumPasswordLength = 128;

    private readonly IAuthenticationRepository
        _authenticationRepository;

    private readonly IPasswordService
        _passwordService;

    public ChangeTemporaryPasswordService(
        IAuthenticationRepository authenticationRepository,
        IPasswordService passwordService)
    {
        ArgumentNullException.ThrowIfNull(
            authenticationRepository);

        ArgumentNullException.ThrowIfNull(
            passwordService);

        _authenticationRepository =
            authenticationRepository;

        _passwordService =
            passwordService;
    }

    public async Task<ChangeTemporaryPasswordResult>
        ChangeAsync(
            ChangeTemporaryPasswordCommand command,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (command.UserId <= 0
            || command.RequestContext.CorrelationId
                == Guid.Empty
            || string.IsNullOrEmpty(
                command.NewPassword)
            || string.IsNullOrEmpty(
                command.ConfirmNewPassword))
        {
            return ChangeTemporaryPasswordResult
                .Failure(
                    ChangeTemporaryPasswordErrorCode
                        .InvalidRequest);
        }

        if (!string.Equals(
                command.NewPassword,
                command.ConfirmNewPassword,
                StringComparison.Ordinal))
        {
            return ChangeTemporaryPasswordResult
                .Failure(
                    ChangeTemporaryPasswordErrorCode
                        .PasswordsDoNotMatch);
        }

        if (!IsStrongPassword(
                command.NewPassword))
        {
            return ChangeTemporaryPasswordResult
                .Failure(
                    ChangeTemporaryPasswordErrorCode
                        .WeakPassword);
        }

        string newPasswordHash =
            _passwordService.HashPassword(
                command.NewPassword);

        TemporaryPasswordChangeData result =
            await _authenticationRepository
                .ChangeTemporaryPasswordAsync(
                    userId: command.UserId,
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
                "The temporary password flag was not removed.");
        }

        return ChangeTemporaryPasswordResult
            .Success(
                result.PasswordChangedAtUtc);
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

        if (char.IsWhiteSpace(password[0])
            || char.IsWhiteSpace(password[^1]))
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
                    !char.IsLetterOrDigit(character)
                    && !char.IsWhiteSpace(character));

        return hasUppercase
            && hasLowercase
            && hasDigit
            && hasSpecialCharacter;
    }
}