using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LithoManager.IntegrationTests.Api.Infrastructure;

public sealed class LithoManagerWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?>
        _previousEnvironmentValues =
            new();

    private bool _environmentRestored;

    public LithoManagerWebApplicationFactory()
    {
        IConfiguration secrets =
            new ConfigurationBuilder()
                .AddUserSecrets<
                    LithoManagerWebApplicationFactory>()
                .Build();

        string connectionString =
            secrets.GetConnectionString(
                "LithoManagerDatabase")
            ?? throw new InvalidOperationException(
                "The IntegrationTests connection " +
                "string was not found in User Secrets.");

        ValidateTestConnectionString(
            connectionString);

        /*
         * WebApplication.CreateBuilder reads environment
         * variables before Program.cs registers Infrastructure.
         *
         * Double underscores represent nested configuration:
         * ConnectionStrings__LithoManagerDatabase
         * becomes
         * ConnectionStrings:LithoManagerDatabase.
         */
        SetEnvironmentVariable(
            "ConnectionStrings__LithoManagerDatabase",
            connectionString);

        SetEnvironmentVariable(
            "Authentication__Jwt__Issuer",
            "LithoManager.IntegrationTests");

        SetEnvironmentVariable(
            "Authentication__Jwt__Audience",
            "LithoManager.IntegrationTests.Client");

        SetEnvironmentVariable(
            "Authentication__Jwt__" +
            "AccessTokenExpirationMinutes",
            "30");

        SetEnvironmentVariable(
            "Authentication__Jwt__" +
            "PasswordChangeTokenExpirationMinutes",
            "10");

        SetEnvironmentVariable(
            "Authentication__Session__" +
            "RefreshTokenExpirationDays",
            "1");

        SetEnvironmentVariable(
            "Authentication__Security__" +
            "PasswordResetTokenExpirationMinutes",
            "15");

        SetEnvironmentVariable(
            "Authentication__Security__" +
            "MaximumFailedLoginAttempts",
            "5");

        SetEnvironmentVariable(
            "Authentication__Security__" +
            "LockoutDurationMinutes",
            "15");

        SetEnvironmentVariable(
            "Authentication__Jwt__SigningKeyBase64",
            CreateTestSigningKeyBase64());
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment(
            "Testing");
    }

    protected override void Dispose(
        bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        finally
        {
            if (disposing)
            {
                RestoreEnvironmentVariables();
            }
        }
    }

    private void SetEnvironmentVariable(
        string name,
        string value)
    {
        _previousEnvironmentValues[name] =
            Environment.GetEnvironmentVariable(
                name);

        Environment.SetEnvironmentVariable(
            name,
            value);
    }

    private void RestoreEnvironmentVariables()
    {
        if (_environmentRestored)
        {
            return;
        }

        foreach (
            KeyValuePair<string, string?>
                environmentVariable
            in _previousEnvironmentValues)
        {
            Environment.SetEnvironmentVariable(
                environmentVariable.Key,
                environmentVariable.Value);
        }

        _environmentRestored = true;
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
        bool containsTestDatabase =
            connectionString.Contains(
                "Database=LithoManagerProTests",
                StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains(
                "Initial Catalog=LithoManagerProTests",
                StringComparison.OrdinalIgnoreCase);

        if (!containsTestDatabase)
        {
            throw new InvalidOperationException(
                "The HTTP integration tests must " +
                "only use LithoManagerProTests.");
        }
    }
}
