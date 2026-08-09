using LithoManager.Application.Abstractions.Security;
using LithoManager.Application.Features.Authentication
    .ChangePassword;
using LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;
using LithoManager.Application.Features.Authentication
    .ForgotPassword;
using LithoManager.Application.Features.Authentication
    .GetCurrentUser;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.Authentication
    .ResetPassword;
using LithoManager.Application.Security;

namespace LithoManager.Api.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<
            IPasswordPolicy,
            PasswordPolicy>();

        services.AddScoped<
            IAuthenticationService,
            AuthenticationService>();

        services.AddScoped<
            IChangeTemporaryPasswordService,
            ChangeTemporaryPasswordService>();

        services.AddScoped<
            IChangePasswordService,
            ChangePasswordService>();

        services.AddScoped<
            IForgotPasswordService,
            ForgotPasswordService>();

        services.AddScoped<
            IResetPasswordService,
            ResetPasswordService>();

        services.AddScoped<
            IGetCurrentUserService,
            GetCurrentUserService>();

        return services;
    }
}
