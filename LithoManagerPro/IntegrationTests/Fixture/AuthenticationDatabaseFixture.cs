using Dapper;
using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Abstractions.Security;
using LithoManager.Application.Features.Authentication.ForgotPassword;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features
    .HumanResources.Departments;
using LithoManager.Application.Features
    .HumanResources.Employees;
using LithoManager.Infrastructure;
using LithoManager.Infrastructure.Persistence.Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Data.Common;
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

    public IPasswordResetTokenService
    PasswordResetTokenService
    {
        get;
        private set;
    } = null!;

    public IDepartmentRepository DepartmentRepository
    {
        get;
        private set;
    } = null!;

    public IEmployeeRepository EmployeeRepository
    {
        get;
        private set;
    } = null!;

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

        services.AddLogging();

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

        PasswordResetTokenService =
            scopedServices.GetRequiredService<
                IPasswordResetTokenService>();

        DepartmentRepository =
            scopedServices.GetRequiredService<
                IDepartmentRepository>();

        EmployeeRepository =
            scopedServices.GetRequiredService<
                IEmployeeRepository>();

        TimeProvider =
            scopedServices.GetRequiredService<
                TimeProvider>();

        await EnsureTestAdministratorAsync();
    }

    public async Task DisposeAsync()
    {
        if (_serviceScope is not null)
        {
            _serviceScope.Dispose();
        }

        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }
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

    public async Task SetTestUserActiveAsync(
        bool isActive)
    {
        var parameters = new
        {
            UserId =
                SuperAdministratorUserId,
            IsActive =
                isActive
        };

        CommandDefinition command =
            new(
                commandText:
                    """
                    UPDATE Security.Users
                    SET
                        IsActive = @IsActive,
                        UpdatedAtUtc = SYSUTCDATETIME(),
                        UpdatedByUserId = @UserId
                    WHERE UserId = @UserId;
                    """,
                parameters:
                    parameters,
                commandType:
                    CommandType.Text,
                cancellationToken:
                    CancellationToken.None);

        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(command);
    }

    public async Task SetTestUserRoleActiveAsync(
        bool isActive)
    {
        var parameters = new
        {
            UserId =
                SuperAdministratorUserId,
            IsActive =
                isActive
        };

        CommandDefinition command =
            new(
                commandText:
                    """
                    UPDATE R
                    SET
                        R.IsActive = @IsActive,
                        R.UpdatedAtUtc = SYSUTCDATETIME()
                    FROM Security.Roles AS R
                    INNER JOIN Security.Users AS U
                        ON U.RoleId = R.RoleId
                    WHERE U.UserId = @UserId;
                    """,
                parameters:
                    parameters,
                commandType:
                    CommandType.Text,
                cancellationToken:
                    CancellationToken.None);

        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(command);
    }

    public async Task CreateInactiveTestEmployeeAsync()
    {
        const string departmentCode =
            "INTEGRATION_TESTS";

        const string departmentName =
            "Integration Tests";

        const string identificationNumber =
            "INTEGRATION-ADMIN";

        var parameters = new
        {
            UserId =
                SuperAdministratorUserId,
            DepartmentCode =
                departmentCode,
            DepartmentName =
                departmentName,
            IdentificationNumber =
                identificationNumber
        };

        CommandDefinition command =
            new(
                commandText:
                    """
                    DELETE FROM HumanResources.Employees
                    WHERE UserId = @UserId
                       OR IdentificationNumber = @IdentificationNumber;

                    DECLARE @DepartmentId int;

                    SELECT
                        @DepartmentId = D.DepartmentId
                    FROM HumanResources.Departments AS D
                    WHERE D.DepartmentCode = @DepartmentCode;

                    IF @DepartmentId IS NULL
                    BEGIN
                        INSERT INTO HumanResources.Departments
                        (
                            DepartmentCode,
                            Name,
                            Description,
                            IsActive,
                            CreatedByUserId
                        )
                        VALUES
                        (
                            @DepartmentCode,
                            @DepartmentName,
                            N'Department used by integration tests.',
                            1,
                            @UserId
                        );

                        SET @DepartmentId = CONVERT(
                            int,
                            SCOPE_IDENTITY());
                    END;

                    INSERT INTO HumanResources.Employees
                    (
                        UserId,
                        DepartmentId,
                        IdentificationNumber,
                        FirstName,
                        LastName,
                        HireDate,
                        JobTitle,
                        BaseSalary,
                        IsActive,
                        CreatedByUserId
                    )
                    VALUES
                    (
                        @UserId,
                        @DepartmentId,
                        @IdentificationNumber,
                        N'Integration',
                        N'Administrator',
                        CONVERT(date, SYSUTCDATETIME()),
                        N'Integration Test User',
                        0,
                        0,
                        @UserId
                    );
                    """,
                parameters:
                    parameters,
                commandType:
                    CommandType.Text,
                cancellationToken:
                    CancellationToken.None);

        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(command);
    }

    public async Task RemoveTestEmployeeAsync()
    {
        var parameters = new
        {
            UserId =
                SuperAdministratorUserId,
            IdentificationNumber =
                "INTEGRATION-ADMIN"
        };

        CommandDefinition command =
            new(
                commandText:
                    """
                    DELETE FROM HumanResources.Employees
                    WHERE UserId = @UserId
                       OR IdentificationNumber = @IdentificationNumber;
                    """,
                parameters:
                    parameters,
                commandType:
                    CommandType.Text,
                cancellationToken:
                    CancellationToken.None);

        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(command);
    }

    public async Task RemoveDepartmentTestDataAsync(
        string departmentCode,
        string? identificationNumber = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            departmentCode);

        var parameters = new
        {
            DepartmentCode = departmentCode,
            IdentificationNumber =
                identificationNumber
        };

        CommandDefinition command =
            new(
                commandText:
                    """
                    DELETE E
                    FROM HumanResources.Employees AS E
                    INNER JOIN HumanResources.Departments AS D
                        ON D.DepartmentId = E.DepartmentId
                    WHERE D.DepartmentCode = @DepartmentCode;

                    DELETE FROM HumanResources.Employees
                    WHERE IdentificationNumber = @IdentificationNumber;

                    DELETE FROM HumanResources.Departments
                    WHERE DepartmentCode = @DepartmentCode;
                    """,
                parameters:
                    parameters,
                commandType:
                    CommandType.Text,
                cancellationToken:
                    CancellationToken.None);

        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(command);
    }

    public async Task CreateActiveEmployeeForDepartmentAsync(
        int departmentId,
        string identificationNumber)
    {
        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(departmentId),
                "DepartmentId must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            identificationNumber);

        var parameters = new
        {
            UserId =
                SuperAdministratorUserId,
            DepartmentId =
                departmentId,
            IdentificationNumber =
                identificationNumber
        };

        CommandDefinition command =
            new(
                commandText:
                    """
                    DELETE FROM HumanResources.Employees
                    WHERE UserId = @UserId
                       OR IdentificationNumber = @IdentificationNumber;

                    INSERT INTO HumanResources.Employees
                    (
                        UserId,
                        DepartmentId,
                        IdentificationNumber,
                        FirstName,
                        LastName,
                        HireDate,
                        JobTitle,
                        BaseSalary,
                        IsActive,
                        CreatedByUserId
                    )
                    VALUES
                    (
                        @UserId,
                        @DepartmentId,
                        @IdentificationNumber,
                        N'Integration',
                        N'Administrator',
                        CONVERT(date, SYSUTCDATETIME()),
                        N'Integration Test User',
                        0,
                        1,
                        @UserId
                    );
                    """,
                parameters:
                    parameters,
                commandType:
                    CommandType.Text,
                cancellationToken:
                    CancellationToken.None);

        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(command);
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
                    ] = CreateTestSigningKeyBase64(),

                    ["Notifications:Email:IsEnabled"] =
                        "false"
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

    public async Task<GeneratedPasswordResetToken>
CreatePasswordResetTokenAsync(
    string requestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            requestPath);

        GeneratedPasswordResetToken generatedToken =
            PasswordResetTokenService.GenerateToken();

        DateTime expiresAtUtc =
            DateTime.UtcNow.AddMinutes(15);

        CreatePasswordResetTokenData result =
            await Repository
                .CreatePasswordResetTokenAsync(
                    emailAddress:
                        TestEmailAddress,
                    tokenHash:
                        generatedToken.TokenHash,
                    expiresAtUtc:
                        expiresAtUtc,
                    requestContext:
                        CreateRequestContext(
                            requestPath),
                    cancellationToken:
                        CancellationToken.None);

        if (!result.WasCreated)
        {
            throw new InvalidOperationException(
                "The integration-test password-reset " +
                "token could not be created.");
        }

        return generatedToken;
    }
}
