using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Abstractions.Security;

namespace LithoManager.Application.Features.Authentication
    .ResetPassword;

public sealed class ResetPasswordService
    : IResetPasswordService
{
    private const int MinimumPasswordLength = 12;
    private const int MaximumPasswordLength = 128;

    /*
        The currently generated password-reset token
        is much shorter than this limit.

        This upper bound is defensive and prevents
        excessively large arbitrary inputs from being
        hashed.
    */
    private const int MaximumTokenLength = 512;

    private const int MaximumPasswordHashLength = 500;

    private readonly IAuthenticationRepository
        _authenticationRepository;

    private readonly IPasswordService
        _passwordService;

    private readonly IPasswordResetTokenService
        _passwordResetTokenService;

    public ResetPasswordService(
        IAuthenticationRepository authenticationRepository,
        IPasswordService passwordService,
        IPasswordResetTokenService passwordResetTokenService)
    {
        ArgumentNullException.ThrowIfNull(
            authenticationRepository);

        ArgumentNullException.ThrowIfNull(
            passwordService);

        ArgumentNullException.ThrowIfNull(
            passwordResetTokenService);

        _authenticationRepository =
            authenticationRepository;

        _passwordService =
            passwordService;

        _passwordResetTokenService =
            passwordResetTokenService;
    }

    public async Task<ResetPasswordResult>
        ResetAsync(
            ResetPasswordCommand command,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!IsValidRequest(command))
        {
            return ResetPasswordResult.Failure(
                ResetPasswordErrorCode
                    .InvalidRequest);
        }

        if (!string.Equals(
                command.NewPassword,
                command.ConfirmNewPassword,
                StringComparison.Ordinal))
        {
            return ResetPasswordResult.Failure(
                ResetPasswordErrorCode
                    .PasswordsDoNotMatch);
        }

        if (!IsStrongPassword(
                command.NewPassword))
        {
            return ResetPasswordResult.Failure(
                ResetPasswordErrorCode
                    .WeakPassword);
        }

        /*
            SHA-256 is used only for the recovery token.

            The password itself continues to use
            IPasswordService.
        */
        byte[] tokenHash =
            _passwordResetTokenService
                .ComputeTokenHash(
                    command.Token);

        PasswordResetContextData? context =
            await _authenticationRepository
                .GetPasswordResetContextByTokenHashAsync(
                    tokenHash,
                    cancellationToken);

        if (context is null)
        {
            return ResetPasswordResult.Failure(
                ResetPasswordErrorCode
                    .PasswordResetNotAvailable);
        }

        ValidatePasswordResetContext(
            context);

        /*
            Prevent reuse of the currently stored
            password.

            Password verification belongs in .NET
            because SQL Server never receives the
            plaintext password.
        */
        bool isPasswordReused =
            _passwordService.VerifyPassword(
                context.PasswordHash,
                command.NewPassword);

        if (isPasswordReused)
        {
            return ResetPasswordResult.Failure(
                ResetPasswordErrorCode
                    .PasswordReuseNotAllowed);
        }

        string newPasswordHash =
            _passwordService.HashPassword(
                command.NewPassword);

        ValidateGeneratedPasswordHash(
            newPasswordHash);

        /*
            CompletePasswordReset performs the final
            authoritative validation again inside its
            SQL transaction.

            ExpectedPasswordHash protects the decision
            made above from a concurrent password
            change.
        */
        CompletePasswordResetData completion =
            await _authenticationRepository
                .CompletePasswordResetAsync(
                    tokenHash:
                        tokenHash,
                    expectedPasswordHash:
                        context.PasswordHash,
                    newPasswordHash:
                        newPasswordHash,
                    requestContext:
                        command.RequestContext,
                    cancellationToken:
                        cancellationToken);

        ValidateCompletionResult(
            context,
            completion);

        if (!completion.WasCompleted)
        {
            return ResetPasswordResult.Failure(
                ResetPasswordErrorCode
                    .PasswordResetNotAvailable);
        }

        return ResetPasswordResult.Success(
            completion.PasswordChangedAtUtc!.Value);
    }

    private static bool IsValidRequest(
        ResetPasswordCommand command)
    {
        if (command.RequestContext.CorrelationId
            == Guid.Empty)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                command.Token))
        {
            return false;
        }

        if (command.Token.Length
            > MaximumTokenLength)
        {
            return false;
        }

        if (string.IsNullOrEmpty(
                command.NewPassword)
            || string.IsNullOrEmpty(
                command.ConfirmNewPassword))
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

    private static void
        ValidatePasswordResetContext(
            PasswordResetContextData context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        if (context.PasswordResetTokenId <= 0)
        {
            throw new InvalidOperationException(
                "The password reset context returned " +
                "an invalid token identifier.");
        }

        if (context.UserId <= 0)
        {
            throw new InvalidOperationException(
                "The password reset context returned " +
                "an invalid user identifier.");
        }

        if (string.IsNullOrWhiteSpace(
                context.PasswordHash))
        {
            throw new InvalidOperationException(
                "The password reset context returned " +
                "an invalid password hash.");
        }

        if (context.PasswordHash.Length
            > MaximumPasswordHashLength)
        {
            throw new InvalidOperationException(
                "The password reset context returned " +
                "a password hash that exceeds the " +
                "supported length.");
        }

        if (context.ExpiresAtUtc.Kind
            != DateTimeKind.Utc)
        {
            throw new InvalidOperationException(
                "The password reset expiration " +
                "returned by the repository must " +
                "use UTC.");
        }
    }

    private static void
        ValidateGeneratedPasswordHash(
            string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(
                passwordHash))
        {
            throw new InvalidOperationException(
                "The password service returned " +
                "an invalid password hash.");
        }

        if (passwordHash.Length
            > MaximumPasswordHashLength)
        {
            throw new InvalidOperationException(
                "The password service returned " +
                "a password hash that exceeds the " +
                "supported database length.");
        }
    }

    private static void ValidateCompletionResult(
        PasswordResetContextData context,
        CompletePasswordResetData completion)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        ArgumentNullException.ThrowIfNull(
            completion);

        if (!completion.WasCompleted)
        {
            if (completion.PasswordResetTokenId
                    is not null
                || completion.UserId
                    is not null
                || completion.PasswordChangedAtUtc
                    is not null
                || completion.RequiresPasswordChange
                    is not null)
            {
                throw new InvalidOperationException(
                    "An unsuccessful password reset " +
                    "returned completion data.");
            }

            return;
        }

        if (completion.PasswordResetTokenId
            != context.PasswordResetTokenId)
        {
            throw new InvalidOperationException(
                "The completed password reset " +
                "returned an unexpected token " +
                "identifier.");
        }

        if (completion.UserId
            != context.UserId)
        {
            throw new InvalidOperationException(
                "The completed password reset " +
                "returned an unexpected user " +
                "identifier.");
        }

        if (completion.PasswordChangedAtUtc
            is null)
        {
            throw new InvalidOperationException(
                "The completed password reset did " +
                "not return PasswordChangedAtUtc.");
        }

        if (completion.PasswordChangedAtUtc.Value.Kind
            != DateTimeKind.Utc)
        {
            throw new InvalidOperationException(
                "The password reset completion " +
                "timestamp must use UTC.");
        }

        if (completion.RequiresPasswordChange
            != false)
        {
            throw new InvalidOperationException(
                "The completed password reset did " +
                "not remove the temporary password " +
                "requirement.");
        }
    }
}