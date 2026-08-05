using LithoManager.Api.Contracts.Authentication;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.IntegrationTests
    .Api.Infrastructure;
using LithoManager.IntegrationTests.Collections;
using LithoManager.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;
using LithoManager.Application.Features.Authentication.Login;

namespace LithoManager.IntegrationTests.Api
    .Authentication;

[Collection(
    AuthenticationDatabaseCollection.Name)]
public sealed class AuthenticationEndpointsTests
{
    private readonly AuthenticationDatabaseFixture
        _databaseFixture;

    private readonly HttpClient _client;

    public AuthenticationEndpointsTests(
        AuthenticationDatabaseFixture
            databaseFixture,
        LithoManagerWebApplicationFactory
            applicationFactory)
    {
        ArgumentNullException.ThrowIfNull(
            databaseFixture);

        ArgumentNullException.ThrowIfNull(
            applicationFactory);

        _databaseFixture =
            databaseFixture;

        _client =
            applicationFactory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    /*
                     * Usamos HTTPS como dirección lógica
                     * para evitar que UseHttpsRedirection
                     * interfiera con las pruebas.
                     */
                    BaseAddress =
                        new Uri(
                            "https://localhost"),

                    AllowAutoRedirect = false
                });
    }

    [Fact]
    public async Task ChangePassword_WhenAccessTokenAndCredentialsAreValid_ChangesPasswordAndRegistersAudit()
    {
        // Arrange
        await _databaseFixture
            .RestoreTestPasswordAsync();

        string accessToken =
            await LoginAndGetAccessTokenAsync(
                AuthenticationDatabaseFixture
                    .TestPassword);

        Guid correlationId =
            Guid.NewGuid();

        ChangePasswordRequest requestBody =
            new()
            {
                CurrentPassword =
                    AuthenticationDatabaseFixture
                        .TestPassword,

                NewPassword =
                    AuthenticationDatabaseFixture
                        .ChangedTestPassword,

                ConfirmNewPassword =
                    AuthenticationDatabaseFixture
                        .ChangedTestPassword
            };

        using HttpRequestMessage request =
            new(
                HttpMethod.Post,
                "/api/auth/change-password");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                scheme: "Bearer",
                parameter: accessToken);

        request.Headers.Add(
            "X-Correlation-ID",
            correlationId.ToString());

        request.Content =
            JsonContent.Create(
                requestBody);

        DateTime startedAtUtc =
            DateTime.UtcNow.AddSeconds(-2);

        try
        {
            // Act
            HttpResponseMessage response =
                await _client.SendAsync(
                    request);

            DateTime completedAtUtc =
                DateTime.UtcNow.AddSeconds(2);

            // Assert: respuesta HTTP
            Assert.Equal(
                HttpStatusCode.NoContent,
                response.StatusCode);

            Assert.True(
                response.Headers.TryGetValues(
                    "X-Correlation-ID",
                    out IEnumerable<string>?
                        correlationValues));

            Assert.Equal(
                correlationId.ToString(),
                Assert.Single(
                    correlationValues));

            Assert.True(
                response.Headers.CacheControl?.NoStore);

            Assert.True(
                response.Headers.TryGetValues(
                    "Pragma",
                    out IEnumerable<string>?
                        pragmaValues));

            Assert.Contains(
                pragmaValues,
                value =>
                    value.Contains(
                        "no-cache",
                        StringComparison
                            .OrdinalIgnoreCase));

            // La contraseña anterior deja de funcionar.
            LoginRequest oldPasswordLogin =
                new()
                {
                    EmailAddress =
                        AuthenticationDatabaseFixture
                            .TestEmailAddress,

                    Password =
                        AuthenticationDatabaseFixture
                            .TestPassword
                };

            HttpResponseMessage oldPasswordResponse =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    oldPasswordLogin);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                oldPasswordResponse.StatusCode);

            // La contraseña nueva sí permite iniciar sesión.
            string newAccessToken =
                await LoginAndGetAccessTokenAsync(
                    AuthenticationDatabaseFixture
                        .ChangedTestPassword);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    newAccessToken));

            // Assert: persistencia
            AuthenticationUserData? updatedUser =
                await _databaseFixture.Repository
                    .GetUserForAuthenticationByIdAsync(
                        _databaseFixture
                            .SuperAdministratorUserId,
                        CancellationToken.None);

            Assert.NotNull(updatedUser);

            Assert.True(
                _databaseFixture.PasswordService
                    .VerifyPassword(
                        updatedUser.PasswordHash,
                        AuthenticationDatabaseFixture
                            .ChangedTestPassword));

            Assert.False(
                _databaseFixture.PasswordService
                    .VerifyPassword(
                        updatedUser.PasswordHash,
                        AuthenticationDatabaseFixture
                            .TestPassword));

            // Assert: auditoría
            AuditLogTestData? auditLog =
                await _databaseFixture
                    .GetAuditLogByCorrelationIdAsync(
                        correlationId);

            Assert.NotNull(auditLog);

            Assert.Equal(
                correlationId,
                auditLog.CorrelationId);

            Assert.Equal(
                "Security",
                auditLog.ModuleName);

            Assert.Equal(
                "PasswordChanged",
                auditLog.ActionName);

            Assert.Equal(
                "Users",
                auditLog.EntityName);

            Assert.Equal(
                _databaseFixture
                    .SuperAdministratorUserId
                    .ToString(),
                auditLog.EntityId);

            Assert.Equal(
                "User",
                auditLog.ActorType);

            Assert.Equal(
                _databaseFixture
                    .SuperAdministratorUserId,
                auditLog.ActorUserId);

            Assert.Equal(
                AuthenticationDatabaseFixture
                    .TestEmailAddress,
                auditLog.ActorEmailAddress);

            Assert.Equal(
                "SuperAdministrator",
                auditLog.ActorRoleCode);

            Assert.True(
                auditLog.IsSuccessful);

            Assert.Equal(
                "POST",
                auditLog.HttpMethod);

            Assert.Equal(
                "/api/auth/change-password",
                auditLog.RequestPath);

            Assert.InRange(
                auditLog.OccurredAtUtc,
                startedAtUtc,
                completedAtUtc);
        }
        finally
        {
            await _databaseFixture
                .RestoreTestPasswordAsync();
        }
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsAccessToken()
    {
        // Arrange
        await _databaseFixture
            .ResetLoginStateAsync();

        LoginRequest request =
            new()
            {
                EmailAddress =
                    AuthenticationDatabaseFixture
                        .TestEmailAddress,

                Password =
                    AuthenticationDatabaseFixture
                        .TestPassword
            };

        // Act
        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.True(
            response.Headers.TryGetValues(
                "X-Correlation-ID",
                out IEnumerable<string>?
                    correlationValues));

        string correlationId =
            Assert.Single(
                correlationValues);

        Assert.True(
            Guid.TryParse(
                correlationId,
                out Guid parsedCorrelationId));

        Assert.NotEqual(
            Guid.Empty,
            parsedCorrelationId);

        Assert.True(
            response.Headers.CacheControl?.NoStore);

        Assert.True(
            response.Headers.TryGetValues(
                "Pragma",
                out IEnumerable<string>?
                    pragmaValues));

        Assert.Contains(
            pragmaValues,
            value =>
                value.Contains(
                    "no-cache",
                    StringComparison.OrdinalIgnoreCase));

        LoginResponse? body =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(body);

        Assert.False(
            body.RequiresPasswordChange);

        Assert.Equal(
            "Bearer",
            body.TokenType);

        Assert.False(
            string.IsNullOrWhiteSpace(
                body.AccessToken));

        Assert.NotNull(
            body.AccessTokenExpiresAtUtc);

        Assert.True(
            body.AccessTokenExpiresAtUtc
            > DateTimeOffset.UtcNow);

        Assert.Null(
            body.PasswordChangeToken);

        Assert.Null(
            body.PasswordChangeTokenExpiresAtUtc);

        Assert.Equal(
            _databaseFixture
                .SuperAdministratorUserId,
            body.User.UserId);

        Assert.Equal(
            AuthenticationDatabaseFixture
                .TestEmailAddress,
            body.User.EmailAddress);

        Assert.Equal(
            "SuperAdministrator",
            body.User.RoleCode);
    }

    [Fact]
    public async Task GetCurrentUser_WhenTokenIsMissing_ReturnsUnauthorized()
    {
        // Act
        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/auth/me");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WhenAccessTokenIsValid_ReturnsCurrentUser()
    {
        // Arrange
        await _databaseFixture
            .ResetLoginStateAsync();

        string accessToken =
            await LoginAndGetAccessTokenAsync();

        Guid correlationId =
            Guid.NewGuid();

        using HttpRequestMessage request =
            new(
                HttpMethod.Get,
                "/api/auth/me");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                scheme: "Bearer",
                parameter: accessToken);

        request.Headers.Add(
            "X-Correlation-ID",
            correlationId.ToString());

        // Act
        HttpResponseMessage response =
            await _client.SendAsync(
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.True(
            response.Headers.TryGetValues(
                "X-Correlation-ID",
                out IEnumerable<string>?
                    correlationValues));

        Assert.Equal(
            correlationId.ToString(),
            Assert.Single(
                correlationValues));

        Assert.True(
            response.Headers.CacheControl?.NoStore);

        CurrentUserResponse? body =
            await response.Content
                .ReadFromJsonAsync<
                    CurrentUserResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            _databaseFixture
                .SuperAdministratorUserId,
            body.UserId);

        Assert.Equal(
            AuthenticationDatabaseFixture
                .TestEmailAddress,
            body.EmailAddress);

        Assert.Equal(
            "SuperAdministrator",
            body.RoleCode);

        Assert.False(
            string.IsNullOrWhiteSpace(
                body.RoleDisplayName));

        Assert.Null(body.EmployeeId);
        Assert.Null(body.FirstName);
        Assert.Null(body.LastName);
        Assert.Null(body.JobTitle);

        Assert.Null(body.DepartmentId);
        Assert.Null(body.DepartmentCode);
        Assert.Null(body.DepartmentName);
    }

    private async Task<string>
        LoginAndGetAccessTokenAsync(
            string password =
                AuthenticationDatabaseFixture
                    .TestPassword)
    {
        LoginRequest loginRequest =
            new()
            {
                EmailAddress =
                    AuthenticationDatabaseFixture
                        .TestEmailAddress,

                Password =
                    password
            };

        HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        LoginResponse? loginBody =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginBody);

        Assert.False(
            string.IsNullOrWhiteSpace(
                loginBody.AccessToken));

        return loginBody.AccessToken;
    }
}