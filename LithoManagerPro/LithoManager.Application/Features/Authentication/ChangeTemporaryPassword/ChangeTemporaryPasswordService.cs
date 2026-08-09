using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Abstractions.Security;

namespace LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;

public sealed class ChangeTemporaryPasswordService
    : IChangeTemporaryPasswordService
{
    private readonly IAuthenticationRepository
        _authenticationRepository;

    private readonly IPasswordService
        _passwordService;

    private readonly IPasswordPolicy
        _passwordPolicy;

    public ChangeTemporaryPasswordService(
        IAuthenticationRepository authenticationRepository,
        IPasswordService passwordService,
        IPasswordPolicy passwordPolicy)
    {
        ArgumentNullException.ThrowIfNull(
            authenticationRepository);

        ArgumentNullException.ThrowIfNull(
            passwordService);

        ArgumentNullException.ThrowIfNull(
            passwordPolicy);

        _authenticationRepository =
            authenticationRepository;

        _passwordService =
            passwordService;

        _passwordPolicy =
            passwordPolicy;
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

        if (!_passwordPolicy.IsStrongPassword(
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
}
