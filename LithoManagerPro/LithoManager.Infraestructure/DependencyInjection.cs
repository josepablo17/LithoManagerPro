using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Abstractions.Security;
using LithoManager.Infrastructure.Persistence.Dapper;
using LithoManager.Infrastructure.Persistence.Repositories.Security;
using LithoManager.Infrastructure.Security;
using LithoManager.Infrastructure.Security.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LithoManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string? connectionString =
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

        services.AddSingleton<
            IPasswordService,
            PasswordService>();

        services
            .AddOptions<JwtOptions>()
            .Bind(
                configuration.GetRequiredSection(
                    JwtOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.Issuer),
                "Authentication:Jwt:Issuer is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.Audience),
                "Authentication:Jwt:Audience is required.")
            .Validate(
                options =>
                    options.AccessTokenExpirationMinutes
                        is > 0 and <= 1440,
                "Authentication:Jwt:AccessTokenExpirationMinutes " +
                "must be between 1 and 1440.")
            .Validate(
                options =>
                    options.PasswordChangeTokenExpirationMinutes
                         is > 0 and <= 60,
                "Authentication:Jwt:" +
                "PasswordChangeTokenExpirationMinutes " +
                "must be between 1 and 60.")
            .Validate(
                options =>
                    HasValidSigningKey(
                        options.SigningKeyBase64),
                "Authentication:Jwt:SigningKeyBase64 must be " +
                "a valid Base64 key containing at least 32 bytes.")
            .ValidateOnStart();

        services.AddSingleton<TimeProvider>(
            TimeProvider.System);

        services.AddSingleton<
            ITokenService,
            TokenService>();

        return services;
    }

    private static bool HasValidSigningKey(
        string? signingKeyBase64)
    {
        if (string.IsNullOrWhiteSpace(signingKeyBase64))
        {
            return false;
        }

        try
        {
            byte[] signingKeyBytes =
                Convert.FromBase64String(
                    signingKeyBase64);

            return signingKeyBytes.Length >= 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}