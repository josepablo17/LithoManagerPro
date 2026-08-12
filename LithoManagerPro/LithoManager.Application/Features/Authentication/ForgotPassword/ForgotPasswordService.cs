using System.Net.Mail;
using LithoManager.Application.Abstractions
    .Notifications;
using LithoManager.Application.Abstractions
    .Persistence;
using LithoManager.Application.Abstractions
    .Security;

namespace LithoManager.Application.Features.Authentication
    .ForgotPassword;

public sealed class ForgotPasswordService
    : IForgotPasswordService
{
    private const int MaximumEmailAddressLength =
        254;

    private readonly IAuthenticationRepository
        _authenticationRepository;

    private readonly IPasswordResetTokenService
        _passwordResetTokenService;

    private readonly IPasswordResetEmailSender
        _passwordResetEmailSender;

    private readonly AuthenticationSecurityOptions
        _securityOptions;

    private readonly TimeProvider
        _timeProvider;

    public ForgotPasswordService(
        IAuthenticationRepository
            authenticationRepository,
        IPasswordResetTokenService
            passwordResetTokenService,
        IPasswordResetEmailSender
            passwordResetEmailSender,
        AuthenticationSecurityOptions securityOptions,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            authenticationRepository);

        ArgumentNullException.ThrowIfNull(
            passwordResetTokenService);

        ArgumentNullException.ThrowIfNull(
            passwordResetEmailSender);

        ArgumentNullException.ThrowIfNull(
            securityOptions);

        ArgumentNullException.ThrowIfNull(
            timeProvider);

        _authenticationRepository =
            authenticationRepository;

        _passwordResetTokenService =
            passwordResetTokenService;

        _passwordResetEmailSender =
            passwordResetEmailSender;

        _securityOptions =
            securityOptions;

        _timeProvider =
            timeProvider;
    }

    public async Task<ForgotPasswordResult>
        RequestAsync(
            ForgotPasswordCommand command,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (!IsValidRequest(command))
        {
            return ForgotPasswordResult.Failure(
                ForgotPasswordErrorCode
                    .InvalidRequest);
        }

        string normalizedEmailAddress =
            command.EmailAddress
                .Trim()
                .ToLowerInvariant();

        /*
            The token is generated before determining
            whether the account is eligible.

            This also helps keep the execution path
            more similar for existing and nonexistent
            email addresses.
        */
        GeneratedPasswordResetToken
            generatedToken =
                _passwordResetTokenService
                    .GenerateToken();

        DateTime expiresAtUtc =
            _timeProvider
                .GetUtcNow()
                .AddMinutes(
                    _securityOptions
                        .PasswordResetTokenExpirationMinutes)
                .UtcDateTime;

        CreatePasswordResetTokenData
            tokenCreation =
                await _authenticationRepository
                    .CreatePasswordResetTokenAsync(
                        emailAddress:
                            normalizedEmailAddress,
                        tokenHash:
                            generatedToken.TokenHash,
                        expiresAtUtc:
                            expiresAtUtc,
                        requestContext:
                            command.RequestContext,
                        cancellationToken:
                            cancellationToken);

        ValidateRepositoryResult(
            tokenCreation);

        if (tokenCreation.WasCreated)
        {
            bool emailWasSent =
                await _passwordResetEmailSender
                    .TrySendAsync(
                        emailAddress:
                            tokenCreation.EmailAddress!,
                        token:
                            generatedToken.Token,
                        expiresAtUtc:
                            tokenCreation
                                .ExpiresAtUtc!.Value,
                        correlationId:
                            command.RequestContext
                                .CorrelationId,
                        cancellationToken:
                            cancellationToken);

            if (!emailWasSent)
            {
                RevokePasswordResetTokenData revocation =
                    await _authenticationRepository
                        .RevokePasswordResetTokenAfterDeliveryFailureAsync(
                            passwordResetTokenId:
                                tokenCreation
                                    .PasswordResetTokenId!
                                    .Value,
                            requestContext:
                                command.RequestContext,
                            cancellationToken:
                                cancellationToken);

                ValidateRevocationResult(
                    tokenCreation,
                    revocation);
            }
        }

        /*
            The public result is intentionally identical
            whether the email exists, is ineligible or
            the email delivery could not be completed.
        */
        return ForgotPasswordResult.Success();
    }

    private static bool IsValidRequest(
        ForgotPasswordCommand command)
    {
        if (command.RequestContext.CorrelationId
            == Guid.Empty)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(
                command.EmailAddress))
        {
            return false;
        }

        string trimmedEmailAddress =
            command.EmailAddress.Trim();

        if (trimmedEmailAddress.Length
            > MaximumEmailAddressLength)
        {
            return false;
        }

        if (!MailAddress.TryCreate(
                trimmedEmailAddress,
                out MailAddress? parsedAddress))
        {
            return false;
        }

        return string.Equals(
            parsedAddress.Address,
            trimmedEmailAddress,
            StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateRepositoryResult(
        CreatePasswordResetTokenData result)
    {
        ArgumentNullException.ThrowIfNull(
            result);

        if (!result.WasCreated)
        {
            if (result.PasswordResetTokenId
                    is not null
                || result.UserId is not null
                || result.EmailAddress is not null
                || result.ExpiresAtUtc is not null)
            {
                throw new InvalidOperationException(
                    "The password reset repository " +
                    "returned data for a token that " +
                    "was not created.");
            }

            return;
        }

        if (result.PasswordResetTokenId is not > 0)
        {
            throw new InvalidOperationException(
                "The password reset repository " +
                "returned an invalid token identifier.");
        }

        if (result.UserId is not > 0)
        {
            throw new InvalidOperationException(
                "The password reset repository " +
                "returned an invalid user identifier.");
        }

        if (string.IsNullOrWhiteSpace(
                result.EmailAddress))
        {
            throw new InvalidOperationException(
                "The password reset repository " +
                "returned an invalid email address.");
        }

        if (result.ExpiresAtUtc is null)
        {
            throw new InvalidOperationException(
                "The password reset repository " +
                "did not return the token expiration.");
        }

        if (result.ExpiresAtUtc.Value.Kind
            != DateTimeKind.Utc)
        {
            throw new InvalidOperationException(
                "The password reset expiration " +
                "returned by the repository must use UTC.");
        }
    }

    private static void ValidateRevocationResult(
    CreatePasswordResetTokenData tokenCreation,
    RevokePasswordResetTokenData revocation)
    {
        ArgumentNullException.ThrowIfNull(
            tokenCreation);

        ArgumentNullException.ThrowIfNull(
            revocation);

        if (revocation.PasswordResetTokenId
            != tokenCreation.PasswordResetTokenId)
        {
            throw new InvalidOperationException(
                "The password reset revocation " +
                "returned an unexpected token identifier.");
        }

        if (revocation.UserId
            != tokenCreation.UserId)
        {
            throw new InvalidOperationException(
                "The password reset revocation " +
                "returned an unexpected user identifier.");
        }

        if (!revocation.IsInactive)
        {
            throw new InvalidOperationException(
                "The password reset token remained " +
                "active after the email delivery failure.");
        }

        if (revocation.WasRevoked
            && revocation.RevokedAtUtc is null)
        {
            throw new InvalidOperationException(
                "The password reset revocation did " +
                "not return its UTC timestamp.");
        }

        if (revocation.RevokedAtUtc
                is DateTime revokedAtUtc
            && revokedAtUtc.Kind
                != DateTimeKind.Utc)
        {
            throw new InvalidOperationException(
                "The password reset revocation " +
                "timestamp must use UTC.");
        }
    }
}
