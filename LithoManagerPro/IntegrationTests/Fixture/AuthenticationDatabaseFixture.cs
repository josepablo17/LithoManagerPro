using System.Data;
using System.Data.Common;
using Dapper;
using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Abstractions.Security;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Infrastructure;
using LithoManager.Infrastructure.Persistence.Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LithoManager.IntegrationTests.Fixtures;

public sealed class AuthenticationDatabaseFixture
    : IAsyncLifetime
{
    public const string TestEmailAddress =
        "integration.admin@lithomanager.local";

    public const string TestPassword =
        "IntegrationTest1!";

    public const string ChangedTestPassword =
    "IntegrationChanged2!";

    private ServiceProvider
        _serviceProvider = null!;

    private IServiceScope
        _serviceScope = null!;

    private ISqlConnectionFactory
        _connectionFactory = null!;

    public IAuthenticationRepository Repository
    {
        get;
        private set;
    } = null!;

    public IPasswordService PasswordService
    {
        get;
        private set;
    } = null!;

    public ITokenService TokenService
    {
        get;
        private set;
    } = null!;

    public TimeProvider TimeProvider
    {
        get;
        private set;
    } = null!;

    public int SuperAdministratorUserId
    {
        get;
        private set;
    }

    public async Task InitializeAsync()
    {
        IConfiguration configuration =
            CreateConfiguration();

        string connectionString =
            configuration.GetConnectionString(
                "LithoManagerDatabase")
            ?? throw new InvalidOperationException(
                "The IntegrationTests connection " +
                "string was not found in User Secrets.");

        ValidateTestConnectionString(
            connectionString);

        ServiceCollection services =
            new();

        services.AddInfrastructure(
            configuration);

        _serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true,
                    ValidateOnBuild = true
                });

        _serviceScope =
            _serviceProvider.CreateScope();

        IServiceProvider scopedServices =
            _serviceScope.ServiceProvider;

        _connectionFactory =
            scopedServices.GetRequiredService<
                ISqlConnectionFactory>();

        Repository =
            scopedServices.GetRequiredService<
                IAuthenticationRepository>();

        PasswordService =
            scopedServices.GetRequiredService<
                IPasswordService>();

        TokenService =
            scopedServices.GetRequiredService<
                ITokenService>();

        TimeProvider =
            scopedServices.GetRequiredService<
                TimeProvider>();

        await EnsureTestAdministratorAsync();
    }

    public async Task DisposeAsync()
    {
        _serviceScope.Dispose();

        await _serviceProvider.DisposeAsync();
    }

    public async Task ResetLoginStateAsync()
    {
        await Repository.RegisterSuccessfulLoginAsync(
            userId:
                SuperAdministratorUserId,
            requestContext:
                CreateRequestContext(
                    "/integration-tests/reset-login-state"),
            cancellationToken:
                CancellationToken.None);
    }

    public async Task ChangeTestPasswordDirectlyAsync(
    string password,
    string requestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            password);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            requestPath);

        /*
         * Garantiza que el usuario no esté bloqueado
         * antes de ejecutar Security.ChangePassword.
         */
        await ResetLoginStateAsync();

        string passwordHash =
            PasswordService.HashPassword(
                password);

        await Repository.ChangePasswordAsync(
            userId:
                SuperAdministratorUserId,
            newPasswordHash:
                passwordHash,
            requestContext:
                CreateRequestContext(
                    requestPath),
            cancellationToken:
                CancellationToken.None);
    }

    public async Task RestoreTestPasswordAsync()
    {
        await ChangeTestPasswordDirectlyAsync(
            password:
                TestPassword,
            requestPath:
                "/integration-tests/" +
                "restore-original-password");
    }

    public static AuthenticationRequestContext
        CreateRequestContext(
            string requestPath)
    {
        return new AuthenticationRequestContext(
            CorrelationId:
                Guid.NewGuid(),
            ClientIpAddress:
                "127.0.0.1",
            UserAgent:
                "LithoManager.IntegrationTests",
            RequestPath:
                requestPath);
    }

    private static IConfiguration
        CreateConfiguration()
    {
        Dictionary<string, string?>
            testSettings =
                new()
                {
                    ["Authentication:Jwt:Issuer"] =
                        "LithoManager.IntegrationTests",

                    ["Authentication:Jwt:Audience"] =
                        "LithoManager.IntegrationTests.Client",

                    [
                        "Authentication:Jwt:" +
                        "AccessTokenExpirationMinutes"
                    ] = "30",

                    [
                        "Authentication:Jwt:" +
                        "PasswordChangeTokenExpirationMinutes"
                    ] = "10",

                    [
                        "Authentication:Jwt:" +
                        "SigningKeyBase64"
                    ] = CreateTestSigningKeyBase64()
                };

        return new ConfigurationBuilder()
            .AddUserSecrets<
                AuthenticationDatabaseFixture>()
            .AddInMemoryCollection(
                testSettings)
            .Build();
    }

    private async Task
        EnsureTestAdministratorAsync()
    {
        AuthenticationUserData? user =
            await Repository
                .GetUserForAuthenticationAsync(
                    TestEmailAddress,
                    CancellationToken.None);

        if (user is null)
        {
            await CreateInitialAdministratorAsync();

            user =
                await Repository
                    .GetUserForAuthenticationAsync(
                        TestEmailAddress,
                        CancellationToken.None);
        }

        if (user is null)
        {
            throw new InvalidOperationException(
                "The integration-test administrator " +
                "could not be created.");
        }

        SuperAdministratorUserId =
            user.UserId;

        if (user.RequiresPasswordChange)
        {
            string newPasswordHash =
                PasswordService.HashPassword(
                    TestPassword);

            await Repository
                .ChangeTemporaryPasswordAsync(
                    userId:
                        user.UserId,
                    newPasswordHash:
                        newPasswordHash,
                    requestContext:
                        CreateRequestContext(
                            "/integration-tests/" +
                            "prepare-password"),
                    cancellationToken:
                        CancellationToken.None);

            user =
                await Repository
                    .GetUserForAuthenticationAsync(
                        TestEmailAddress,
                        CancellationToken.None);
        }

        if (user is null)
        {
            throw new InvalidOperationException(
                "The prepared integration-test user " +
                "could not be retrieved.");
        }

        bool isPasswordValid =
            PasswordService.VerifyPassword(
                user.PasswordHash,
                TestPassword);

        if (!isPasswordValid)
        {
            throw new InvalidOperationException(
                "The existing integration-test account " +
                "does not contain the expected password. " +
                "Recreate the LithoManagerProTests database.");
        }

        if (user.RequiresPasswordChange)
        {
            throw new InvalidOperationException(
                "The integration-test administrator " +
                "still requires a password change.");
        }

        await ResetLoginStateAsync();
    }

    private async Task
        CreateInitialAdministratorAsync()
    {
        string passwordHash =
            PasswordService.HashPassword(
                TestPassword);

        var parameters = new
        {
            EmailAddress =
                TestEmailAddress,

            PasswordHash =
                passwordHash,

            TemporaryPasswordExpiresAtUtc =
                DateTime.UtcNow.AddHours(2),

            CorrelationId =
                Guid.NewGuid(),

            ClientIpAddress =
                "127.0.0.1",

            UserAgent =
                "LithoManager.IntegrationTests",

            RequestPath =
                "/integration-tests/bootstrap"
        };

        CommandDefinition command =
            new(
                commandText:
                    "Security." +
                    "CreateInitialSuperAdministrator",

                parameters:
                    parameters,

                commandType:
                    CommandType.StoredProcedure,

                cancellationToken:
                    CancellationToken.None);

        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        InitialAdministratorData createdUser =
            await connection
                .QuerySingleAsync<
                    InitialAdministratorData>(
                        command);

        SuperAdministratorUserId =
            createdUser.UserId;
    }

    private static string
        CreateTestSigningKeyBase64()
    {
        byte[] signingKeyBytes =
            Enumerable
                .Range(1, 32)
                .Select(
                    value => (byte)value)
                .ToArray();

        return Convert.ToBase64String(
            signingKeyBytes);
    }

    private static void ValidateTestConnectionString(
        string connectionString)
    {
        bool containsDatabaseName =
            connectionString.Contains(
                "Database=LithoManagerProTests",
                StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains(
                "Initial Catalog=LithoManagerProTests",
                StringComparison.OrdinalIgnoreCase);

        if (!containsDatabaseName)
        {
            throw new InvalidOperationException(
                "IntegrationTests must only run " +
                "against LithoManagerProTests.");
        }
    }

    private sealed class InitialAdministratorData
    {
        public int UserId
        {
            get;
            init;
        }
    }

    public async Task<AuditLogTestData?>
    GetAuditLogByCorrelationIdAsync(
        Guid correlationId)
    {
        if (correlationId == Guid.Empty)
        {
            throw new ArgumentException(
                "CorrelationId cannot be empty.",
                nameof(correlationId));
        }

        var parameters = new
        {
            CorrelationId = correlationId
        };

        CommandDefinition command =
            new(
                commandText:
                    "Audit.GetAuditLogByCorrelationId",
                parameters:
                    parameters,
                commandType:
                    CommandType.StoredProcedure,
                cancellationToken:
                    CancellationToken.None);

        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        return await connection
            .QuerySingleOrDefaultAsync<
                AuditLogTestData>(
                    command);
    }
}