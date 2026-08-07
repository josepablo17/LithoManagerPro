using System.Globalization;
using System.Text.Encodings.Web;
using LithoManager.Application.Abstractions
    .Notifications;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LithoManager.Infrastructure.Notifications
    .Email;

public sealed class PasswordResetEmailSender
    : IPasswordResetEmailSender
{
    private readonly EmailOptions
        _options;

    private readonly ILogger<
        PasswordResetEmailSender>
        _logger;

    public PasswordResetEmailSender(
        IOptions<EmailOptions> options,
        ILogger<PasswordResetEmailSender>
            logger)
    {
        ArgumentNullException.ThrowIfNull(
            options);

        ArgumentNullException.ThrowIfNull(
            logger);

        _options =
            options.Value;

        _logger =
            logger;
    }

    public async Task<bool> TrySendAsync(
        string emailAddress,
        string token,
        DateTime expiresAtUtc,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            emailAddress);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            token);

        if (expiresAtUtc.Kind
            != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The expiration date must use UTC.",
                nameof(expiresAtUtc));
        }

        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException(
                "CorrelationId is required.",
                nameof(correlationId));
        }

        if (!_options.IsEnabled)
        {
            _logger.LogWarning(
                "Password reset email delivery is " +
                "disabled. CorrelationId: " +
                "{CorrelationId}.",
                correlationId);

            return false;
        }

        string passwordResetUrl =
            BuildPasswordResetUrl(
                token);

        MimeMessage message =
            CreateMessage(
                emailAddress,
                passwordResetUrl,
                expiresAtUtc);

        using SmtpClient smtpClient =
            new();

        smtpClient.Timeout =
            _options.TimeoutMilliseconds;

        try
        {
            await smtpClient.ConnectAsync(
                host:
                    _options.Host,
                port:
                    _options.Port,
                options:
                    ResolveSocketOptions(
                        _options.SecurityMode),
                cancellationToken:
                    cancellationToken);

            if (!string.IsNullOrWhiteSpace(
                    _options.UserName))
            {
                await smtpClient
                    .AuthenticateAsync(
                        userName:
                            _options.UserName,
                        password:
                            _options.Password
                            ?? string.Empty,
                        cancellationToken:
                            cancellationToken);
            }

            await smtpClient.SendAsync(
                message,
                cancellationToken);

            await smtpClient.DisconnectAsync(
                quit: true,
                cancellationToken:
                    cancellationToken);

            _logger.LogInformation(
                "Password reset email sent. " +
                "CorrelationId: {CorrelationId}.",
                correlationId);

            return true;
        }
        catch (OperationCanceledException)
            when (cancellationToken
                .IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Password reset email delivery " +
                "failed. CorrelationId: " +
                "{CorrelationId}.",
                correlationId);

            return false;
        }
        finally
        {
            if (smtpClient.IsConnected)
            {
                try
                {
                    await smtpClient
                        .DisconnectAsync(
                            quit: true,
                            cancellationToken:
                                CancellationToken
                                    .None);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "SMTP disconnection failed. " +
                        "CorrelationId: " +
                        "{CorrelationId}.",
                        correlationId);
                }
            }
        }
    }

    private MimeMessage CreateMessage(
        string emailAddress,
        string passwordResetUrl,
        DateTime expiresAtUtc)
    {
        MimeMessage message =
            new();

        message.From.Add(
            new MailboxAddress(
                _options.FromName,
                _options.FromAddress));

        message.To.Add(
            MailboxAddress.Parse(
                emailAddress));

        message.Subject =
            "Restablecimiento de contraseña " +
            "de LithoManager";

        BodyBuilder bodyBuilder =
            new()
            {
                TextBody =
                    BuildTextBody(
                        passwordResetUrl,
                        expiresAtUtc),

                HtmlBody =
                    BuildHtmlBody(
                        passwordResetUrl,
                        expiresAtUtc)
            };

        message.Body =
            bodyBuilder.ToMessageBody();

        return message;
    }

    private string BuildPasswordResetUrl(
        string token)
    {
        UriBuilder uriBuilder =
            new(
                _options
                    .PasswordResetBaseUrl);

        string currentQuery =
            uriBuilder.Query
                .TrimStart('?');

        string tokenParameter =
            "token="
            + Uri.EscapeDataString(
                token);

        uriBuilder.Query =
            string.IsNullOrWhiteSpace(
                currentQuery)
                ? tokenParameter
                : currentQuery
                    + "&"
                    + tokenParameter;

        return uriBuilder
            .Uri
            .AbsoluteUri;
    }

    private static string BuildTextBody(
        string passwordResetUrl,
        DateTime expiresAtUtc)
    {
        string formattedExpiration =
            expiresAtUtc.ToString(
                "yyyy-MM-dd HH:mm:ss 'UTC'",
                CultureInfo.InvariantCulture);

        return
            "Se recibió una solicitud para " +
            "restablecer tu contraseña de " +
            "LithoManager."
            + Environment.NewLine
            + Environment.NewLine
            + "Utiliza el siguiente enlace:"
            + Environment.NewLine
            + passwordResetUrl
            + Environment.NewLine
            + Environment.NewLine
            + "El enlace vence el "
            + formattedExpiration
            + "."
            + Environment.NewLine
            + Environment.NewLine
            + "Si no solicitaste este cambio, " +
            "puedes ignorar este mensaje.";
    }

    private static string BuildHtmlBody(
        string passwordResetUrl,
        DateTime expiresAtUtc)
    {
        string safeUrl =
            HtmlEncoder.Default.Encode(
                passwordResetUrl);

        string formattedExpiration =
            expiresAtUtc.ToString(
                "yyyy-MM-dd HH:mm:ss 'UTC'",
                CultureInfo.InvariantCulture);

        string safeExpiration =
            HtmlEncoder.Default.Encode(
                formattedExpiration);

        return
            """
            <!DOCTYPE html>
            <html lang="es">
            <head>
                <meta charset="utf-8">
                <title>Restablecimiento de contraseña</title>
            </head>
            <body style="font-family:Arial,sans-serif;
                         line-height:1.5;
                         color:#222222;">
                <h2>Restablecimiento de contraseña</h2>

                <p>
                    Se recibió una solicitud para
                    restablecer tu contraseña de
                    LithoManager.
                </p>

                <p>
                    <a href="
            """
            + safeUrl
            +
            """
            "
                       style="display:inline-block;
                              padding:12px 18px;
                              background:#1f5fbf;
                              color:#ffffff;
                              text-decoration:none;
                              border-radius:4px;">
                        Restablecer contraseña
                    </a>
                </p>

                <p>
                    Este enlace vence el
                    <strong>
            """
            + safeExpiration
            +
            """
                    </strong>.
                </p>

                <p>
                    Si no solicitaste este cambio,
                    puedes ignorar este mensaje.
                </p>
            </body>
            </html>
            """;
    }

    private static SecureSocketOptions
        ResolveSocketOptions(
            SmtpSecurityMode securityMode)
    {
        return securityMode switch
        {
            SmtpSecurityMode.Auto =>
                SecureSocketOptions.Auto,

            SmtpSecurityMode.StartTls =>
                SecureSocketOptions.StartTls,

            SmtpSecurityMode.SslOnConnect =>
                SecureSocketOptions.SslOnConnect,

            _ =>
                throw new InvalidOperationException(
                    "The SMTP security mode " +
                    "is not supported.")
        };
    }
}