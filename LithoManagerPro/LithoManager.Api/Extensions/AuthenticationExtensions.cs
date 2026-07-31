using LithoManager.Api.Authorization;
using LithoManager.Infrastructure.Security.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace LithoManager.Api.Extensions;

public static class AuthenticationExtensions
{
    private const string RoleClaimType = "role";

    private const string TokenUseClaimType =
    "token_use";

    private const string AccessTokenUse =
        "access";

    private const string PasswordChangeTokenUse =
        "password_change";

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection jwtSection =
            configuration.GetRequiredSection(
                JwtOptions.SectionName);

        string issuer =
            jwtSection[nameof(JwtOptions.Issuer)]
            ?? throw new InvalidOperationException(
                "Authentication:Jwt:Issuer was not found.");

        string audience =
            jwtSection[nameof(JwtOptions.Audience)]
            ?? throw new InvalidOperationException(
                "Authentication:Jwt:Audience was not found.");

        string signingKeyBase64 =
            jwtSection[nameof(JwtOptions.SigningKeyBase64)]
            ?? throw new InvalidOperationException(
                "Authentication:Jwt:SigningKeyBase64 was not found.");

        byte[] signingKeyBytes;

        try
        {
            signingKeyBytes =
                Convert.FromBase64String(signingKeyBase64);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:SigningKeyBase64 " +
                "is not valid Base64.",
                exception);
        }

        if (signingKeyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:SigningKeyBase64 " +
                "must contain at least 32 bytes.");
        }

        SymmetricSecurityKey signingKey =
            new(signingKeyBytes);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                /*
                 * Conserva los nombres originales:
                 * sub, email, role, employee_id.
                 */
                options.MapInboundClaims = false;

                options.SaveToken = false;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        RequireSignedTokens = true,
                        RequireExpirationTime = true,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,

                        ValidateIssuer = true,
                        ValidIssuer = issuer,

                        ValidateAudience = true,
                        ValidAudience = audience,

                        ValidateLifetime = true,

                        /*
                         * Evita aceptar durante varios minutos
                         * un token que ya venció.
                         */
                        ClockSkew = TimeSpan.FromSeconds(30),

                        NameClaimType =
                            JwtRegisteredClaimNames.Email,

                        RoleClaimType =
                            RoleClaimType
                    };
            });

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy =
                new AuthorizationPolicyBuilder(
                    JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .RequireClaim(
                        TokenUseClaimType,
                        AccessTokenUse)
                    .Build();

            options.AddPolicy(
                AuthorizationPolicyNames
                    .PasswordChangeOnly,
                policy =>
                {
                    policy.AddAuthenticationSchemes(
                        JwtBearerDefaults
                            .AuthenticationScheme);

                    policy.RequireAuthenticatedUser();

                    policy.RequireClaim(
                        TokenUseClaimType,
                        PasswordChangeTokenUse);
                });
        });

        return services;
    }
}