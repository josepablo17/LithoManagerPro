using System.Data;
using Dapper;
using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Infrastructure.Persistence.Dapper;

namespace LithoManager.Infrastructure.Persistence.Repositories.Security;

public sealed class AuthenticationRepository
    : IAuthenticationRepository
{
    private const string GetUserForAuthenticationProcedure =
        "Security.GetUserForAuthentication";

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

    private const string RegisterSuccessfulLoginProcedure =
    "Security.RegisterSuccessfulLogin";

    private const string RegisterFailedLoginProcedure =
        "Security.RegisterFailedLogin";
}