using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Infrastructure.Persistence.Dapper;
using LithoManager.Infrastructure.Persistence.Repositories.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LithoManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString(
                "LithoManagerDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'LithoManagerDatabase' was not found.");
        }

        services.AddSingleton<ISqlConnectionFactory>(
            new SqlConnectionFactory(connectionString));

        services.AddScoped<
            IAuthenticationRepository,
            AuthenticationRepository>();

        return services;
    }
}