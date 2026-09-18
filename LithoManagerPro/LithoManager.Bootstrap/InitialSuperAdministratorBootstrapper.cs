using System.Data;
using LithoManager.Application.Abstractions.Security;
using Microsoft.Data.SqlClient;

namespace LithoManager.Bootstrap;

internal sealed class InitialSuperAdministratorBootstrapper(
    IPasswordService passwordService,
    TimeProvider timeProvider)
{
    private readonly IPasswordService _passwordService =
        passwordService
        ?? throw new ArgumentNullException(
            nameof(passwordService));

    private readonly TimeProvider _timeProvider =
        timeProvider
        ?? throw new ArgumentNullException(
            nameof(timeProvider));

    public async Task<InitialSuperAdministratorResult> ExecuteAsync(
        BootstrapConfiguration configuration,
        BootstrapCredentials credentials,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(credentials);

        string passwordHash =
            _passwordService.HashPassword(
                credentials.TemporaryPassword);

        DateTime expiresAtUtc =
            _timeProvider
                .GetUtcNow()
                .AddHours(
                    configuration
                        .TemporaryPasswordExpirationHours)
                .UtcDateTime;

        Guid correlationId = Guid.NewGuid();

        await using SqlConnection connection =
            new(configuration.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using SqlCommand command = new(
            "Security.CreateInitialSuperAdministrator",
            connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.Add(
                "@EmailAddress",
                SqlDbType.NVarChar,
                254)
            .Value = credentials.EmailAddress;

        command.Parameters.Add(
                "@PasswordHash",
                SqlDbType.NVarChar,
                500)
            .Value = passwordHash;

        command.Parameters.Add(
                "@TemporaryPasswordExpiresAtUtc",
                SqlDbType.DateTime2)
            .Value = expiresAtUtc;

        command.Parameters.Add(
                "@CorrelationId",
                SqlDbType.UniqueIdentifier)
            .Value = correlationId;

        command.Parameters.Add(
                "@ClientIpAddress",
                SqlDbType.NVarChar,
                45)
            .Value = DBNull.Value;

        command.Parameters.Add(
                "@UserAgent",
                SqlDbType.NVarChar,
                512)
            .Value = "LithoManager.Bootstrap";

        command.Parameters.Add(
                "@RequestPath",
                SqlDbType.NVarChar,
                500)
            .Value =
                "bootstrap://initial-super-administrator";

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                CommandBehavior.SingleRow,
                cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "The bootstrap procedure did not return the created user.");
        }

        return new InitialSuperAdministratorResult(
            UserId: reader.GetInt32(
                reader.GetOrdinal("UserId")),
            EmailAddress: reader.GetString(
                reader.GetOrdinal("EmailAddress")),
            RoleCode: reader.GetString(
                reader.GetOrdinal("RoleCode")),
            RequiresPasswordChange: reader.GetBoolean(
                reader.GetOrdinal("RequiresPasswordChange")),
            TemporaryPasswordExpiresAtUtc:
                reader.GetDateTime(
                    reader.GetOrdinal(
                        "TemporaryPasswordExpiresAtUtc")),
            CorrelationId: correlationId);
    }
}

internal sealed record InitialSuperAdministratorResult(
    int UserId,
    string EmailAddress,
    string RoleCode,
    bool RequiresPasswordChange,
    DateTime TemporaryPasswordExpiresAtUtc,
    Guid CorrelationId);
