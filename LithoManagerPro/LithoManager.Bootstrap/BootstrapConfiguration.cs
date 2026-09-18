using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace LithoManager.Bootstrap;

internal sealed record BootstrapConfiguration(
    string ConnectionString,
    string DataSource,
    string DatabaseName,
    int TemporaryPasswordExpirationHours)
{
    private const int MinimumExpirationHours = 1;
    private const int MaximumExpirationHours = 168;

    public static BootstrapConfiguration Load()
    {
        IConfigurationRoot configuration =
            new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(
                    "appsettings.json",
                    optional: false,
                    reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

        string? connectionString =
            configuration[
                "ConnectionStrings:LithoManagerDatabase"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new BootstrapConfigurationException(
                "Define the environment variable " +
                "ConnectionStrings__LithoManagerDatabase " +
                "with the connection string for the target database.");
        }

        SqlConnectionStringBuilder connectionStringBuilder;

        try
        {
            connectionStringBuilder =
                new SqlConnectionStringBuilder(
                    connectionString);
        }
        catch (ArgumentException exception)
        {
            throw new BootstrapConfigurationException(
                "ConnectionStrings:LithoManagerDatabase is invalid.",
                exception);
        }

        if (string.IsNullOrWhiteSpace(
                connectionStringBuilder.DataSource))
        {
            throw new BootstrapConfigurationException(
                "The bootstrap connection string must specify a SQL " +
                "Server data source.");
        }

        if (string.IsNullOrWhiteSpace(
                connectionStringBuilder.InitialCatalog))
        {
            throw new BootstrapConfigurationException(
                "The bootstrap connection string must explicitly " +
                "specify the target database.");
        }

        string? expirationHoursValue =
            configuration[
                "Bootstrap:TemporaryPasswordExpirationHours"];

        if (!int.TryParse(
                expirationHoursValue,
                out int expirationHours)
            || expirationHours is < MinimumExpirationHours
                or > MaximumExpirationHours)
        {
            throw new BootstrapConfigurationException(
                "Bootstrap:TemporaryPasswordExpirationHours " +
                $"must be between {MinimumExpirationHours} and " +
                $"{MaximumExpirationHours}.");
        }

        return new BootstrapConfiguration(
            connectionString.Trim(),
            connectionStringBuilder.DataSource,
            connectionStringBuilder.InitialCatalog,
            expirationHours);
    }
}

internal sealed class BootstrapConfigurationException(
    string message,
    Exception? innerException = null)
    : Exception(message, innerException);
