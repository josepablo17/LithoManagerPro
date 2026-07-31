using LithoManager.Api.Extensions;
using LithoManager.Infrastructure;
using LithoManager.Application.Features.Authentication.Login;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddScoped<
    IAuthenticationService,
    AuthenticationService>();

builder.Services.AddJwtAuthentication(
    builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();