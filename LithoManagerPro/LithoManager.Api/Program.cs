using LithoManager.Api.Extensions;
using LithoManager.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi;

const string FrontendCorsPolicy =
    "FrontendCorsPolicy";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

string[] allowedFrontendOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        FrontendCorsPolicy,
        policy =>
        {
            if (allowedFrontendOrigins.Length == 0)
            {
                return;
            }

            policy
                .WithOrigins(allowedFrontendOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

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

builder.Services.AddApplicationServices(
    builder.Configuration);

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

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
