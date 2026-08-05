using LithoManager.Api.Extensions;
using LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi;
using LithoManager.Application.Features.Authentication
    .GetCurrentUser;
using LithoManager.Application.Features.Authentication
    .ChangePassword;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "LithoManager API",
            Version = "v1",
            Description =
                "API para la administración de LithoManager."
        });

    options.AddSecurityDefinition(
        "bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description =
                "Ingrese únicamente el JWT. " +
                "Swagger agregará automáticamente " +
                "el prefijo Bearer."
        });

    options.AddSecurityRequirement(
        document =>
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference(
                        "bearer",
                        document)
                ] = []
            });
});

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddScoped<
    IAuthenticationService,
    AuthenticationService>();

builder.Services.AddScoped<
    IChangeTemporaryPasswordService,
    ChangeTemporaryPasswordService>();

builder.Services.AddScoped<
    IChangePasswordService,
    ChangePasswordService>();

builder.Services.AddScoped<
    IGetCurrentUserService,
    GetCurrentUserService>();

builder.Services.AddJwtAuthentication(
    builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "LithoManager API v1");

        options.DocumentTitle =
            "LithoManager API";
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}