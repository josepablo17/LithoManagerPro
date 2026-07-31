using System.Data;
using Dapper;
using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Infrastructure.Persistence.Dapper;
using LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;

namespace LithoManager.Infrastructure.Persistence.Repositories.Security;

public sealed class AuthenticationRepository
    : IAuthenticationRepository
{
    private const string GetUserForAuthenticationProcedure =
    "Security.GetUserForAuthentication";

    private const string RegisterSuccessfulLoginProcedure =
        "Security.RegisterSuccessfulLogin";

    private const string RegisterFailedLoginProcedure =
        "Security.RegisterFailedLogin";

    private const string ChangeTemporaryPasswordProcedure =
    "Security.ChangeTemporaryPassword";

    private readonly ISqlConnectionFactory _connectionFactory;

    public AuthenticationRepository(
        ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AuthenticationUserData?>
        GetUserForAuthenticationAsync(
            string emailAddress,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailAddress);

        var parameters = new
        {
            EmailAddress = emailAddress.Trim()
        };

        var command = new CommandDefinition(
            commandText: GetUserForAuthenticationProcedure,
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await connection
            .QuerySingleOrDefaultAsync<AuthenticationUserData>(
                command);
    }

    public async Task<SuccessfulLoginRegistrationData>
    RegisterSuccessfulLoginAsync(
        int userId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "UserId must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(requestContext);

        var parameters = new
        {
            UserId = userId,
            requestContext.CorrelationId,
            requestContext.ClientIpAddress,
            requestContext.UserAgent,
            requestContext.RequestPath
        };

        var command = new CommandDefinition(
            commandText: RegisterSuccessfulLoginProcedure,
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await connection
            .QuerySingleAsync<SuccessfulLoginRegistrationData>(
                command);
    }

    public async Task<FailedLoginRegistrationData>
    RegisterFailedLoginAsync(
        string attemptedEmailAddress,
        int? userId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            attemptedEmailAddress);

        if (userId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "UserId must be greater than zero when provided.");
        }

        ArgumentNullException.ThrowIfNull(requestContext);

        var parameters = new
        {
            AttemptedEmailAddress = attemptedEmailAddress.Trim(),
            UserId = userId,
            requestContext.CorrelationId,
            requestContext.ClientIpAddress,
            requestContext.UserAgent,
            requestContext.RequestPath
        };

        var command = new CommandDefinition(
            commandText: RegisterFailedLoginProcedure,
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await connection
            .QuerySingleAsync<FailedLoginRegistrationData>(
                command);
    }

    public async Task<TemporaryPasswordChangeData>
    ChangeTemporaryPasswordAsync(
        int userId,
        string newPasswordHash,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "UserId must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            newPasswordHash);

        ArgumentNullException.ThrowIfNull(
            requestContext);

        var parameters = new
        {
            UserId = userId,
            NewPasswordHash = newPasswordHash,
            requestContext.CorrelationId,
            requestContext.ClientIpAddress,
            requestContext.UserAgent,
            requestContext.RequestPath
        };

        var command = new CommandDefinition(
            commandText:
                ChangeTemporaryPasswordProcedure,
            parameters: parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await connection
            .QuerySingleAsync<
                TemporaryPasswordChangeData>(
                    command);
    }

}