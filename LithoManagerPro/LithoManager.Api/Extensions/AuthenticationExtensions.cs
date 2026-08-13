using LithoManager.Api.Authorization;
using LithoManager.Infrastructure.Security.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication.Login;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;

namespace LithoManager.Api.Extensions;

public static class AuthenticationExtensions
{
    private const string RoleClaimType = "role";

    private const string TokenUseClaimType =
    "token_use";

    private const string TokenVersionClaimType =
        "token_version";

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

                options.Events =
                    new JwtBearerEvents
                    {
                        OnTokenValidated =
                            ValidateTokenVersionAsync
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

            options.AddPolicy(
                AuthorizationPolicyNames
                    .HumanResourcesDepartments,
                policy =>
                {
                    policy.AddAuthenticationSchemes(
                        JwtBearerDefaults
                            .AuthenticationScheme);

                    policy.RequireAuthenticatedUser();

                    policy.RequireClaim(
                        TokenUseClaimType,
                        AccessTokenUse);

                    policy.RequireRole(
                        "SuperAdministrator",
                        "HumanResourcesAdministrator");
                });

            options.AddPolicy(
                AuthorizationPolicyNames
                    .HumanResourcesEmployees,
                policy =>
                {
                    policy.AddAuthenticationSchemes(
                        JwtBearerDefaults
                            .AuthenticationScheme);

                    policy.RequireAuthenticatedUser();

                    policy.RequireClaim(
                        TokenUseClaimType,
                        AccessTokenUse);

                    policy.RequireRole(
                        "SuperAdministrator",
                        "HumanResourcesAdministrator");
                });

            options.AddPolicy(
                AuthorizationPolicyNames
                    .LeaveManagementAdministration,
                policy =>
                {
                    policy.AddAuthenticationSchemes(
                        JwtBearerDefaults
                            .AuthenticationScheme);

                    policy.RequireAuthenticatedUser();

                    policy.RequireClaim(
                        TokenUseClaimType,
                        AccessTokenUse);

                    policy.RequireRole(
                        "SuperAdministrator",
                        "HumanResourcesAdministrator",
                        "HumanResourcesStaff");
                });

            options.AddPolicy(
                AuthorizationPolicyNames
                    .LeaveManagementAdministrationMutation,
                policy =>
                {
                    policy.AddAuthenticationSchemes(
                        JwtBearerDefaults
                            .AuthenticationScheme);

                    policy.RequireAuthenticatedUser();

                    policy.RequireClaim(
                        TokenUseClaimType,
                        AccessTokenUse);

                    policy.RequireRole(
                        "SuperAdministrator",
                        "HumanResourcesAdministrator");
                });
        });

        return services;
    }

    private static async Task ValidateTokenVersionAsync(
        TokenValidatedContext context)
    {
        string? userIdValue =
            context.Principal?
                .FindFirst(
                    JwtRegisteredClaimNames.Sub)?
                .Value;

        string? tokenVersionValue =
            context.Principal?
                .FindFirst(
                    TokenVersionClaimType)?
                .Value;

        if (!int.TryParse(
                userIdValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int userId)
            || userId <= 0
            || !int.TryParse(
                tokenVersionValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int tokenVersion)
            || tokenVersion <= 0)
        {
            context.Fail(
                "The token identity is not valid.");

            return;
        }

        IAuthenticationRepository repository =
            context.HttpContext
                .RequestServices
                .GetRequiredService<
                    IAuthenticationRepository>();

        UserTokenValidationData? user =
            await repository
                .GetUserTokenValidationByIdAsync(
                    userId,
                    context.HttpContext
                        .RequestAborted);

        if (user is null
            || user.TokenVersion != tokenVersion)
        {
            context.Fail(
                "The token is no longer valid.");

            return;
        }

        if (!user.IsUserActive
            || !user.IsRoleActive
            || (user.EmployeeId is not null
                && user.IsEmployeeActive != true))
        {
            context.Fail(
                "The token principal is not active.");
        }
    }
}
