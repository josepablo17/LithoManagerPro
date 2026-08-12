using LithoManager.Api.Contracts.Authentication;
using LithoManager.Application.Abstractions.Security;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.IntegrationTests
    .Api.Infrastructure;
using LithoManager.IntegrationTests.Collections;
using LithoManager.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace LithoManager.IntegrationTests.Api
    .Authentication;

[Collection(
    AuthenticationDatabaseCollection.Name)]
public sealed class AuthenticationEndpointsTests
{
    private const string RefreshTokenCookieName =
        "__Host-LithoManager.RefreshToken";

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

            using HttpRequestMessage staleTokenRequest =
                new(
                    HttpMethod.Get,
                    "/api/auth/me");

            staleTokenRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    scheme: "Bearer",
                    parameter: accessToken);

            HttpResponseMessage staleTokenResponse =
                await _client.SendAsync(
                    staleTokenRequest);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                staleTokenResponse.StatusCode);

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
    public async Task Login_WhenCredentialsAreValid_SetsHttpOnlyRefreshCookieAndDoesNotExposeItInJson()
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

        string responseJson =
            await response.Content
                .ReadAsStringAsync();

        LoginResponse? body =
            JsonSerializer.Deserialize<LoginResponse>(
                responseJson,
                JsonSerializerOptions.Web);

        Assert.NotNull(body);
        Assert.False(body.RequiresPasswordChange);
        Assert.False(
            string.IsNullOrWhiteSpace(
                body.AccessToken));

        string refreshCookie =
            AssertSingleRefreshSetCookie(
                response);

        Assert.Contains(
            "httponly",
            refreshCookie,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "secure",
            refreshCookie,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "samesite=none",
            refreshCookie,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "path=/",
            refreshCookie,
            StringComparison.OrdinalIgnoreCase);

        string refreshCookieValue =
            ExtractCookieValue(refreshCookie);

        Assert.False(
            string.IsNullOrWhiteSpace(
                refreshCookieValue));

        Assert.DoesNotContain(
            refreshCookieValue,
            responseJson,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "refreshToken",
            responseJson,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WhenPasswordChangeIsRequired_DoesNotCreateRefreshCookieAndClearsPreviousCookie()
    {
        // Arrange
        await _databaseFixture
            .RestoreTestPasswordAsync();

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

        HttpResponseMessage initialLoginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            initialLoginResponse.StatusCode);

        AssertSingleRefreshSetCookie(
            initialLoginResponse);

        await _databaseFixture
            .RequireTemporaryPasswordChangeAsync();

        try
        {
            // Act
            HttpResponseMessage response =
                await _client.PostAsJsonAsync(
                    "/api/auth/login",
                    request);

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            LoginResponse? body =
                await response.Content
                    .ReadFromJsonAsync<LoginResponse>();

            Assert.NotNull(body);
            Assert.True(
                body.RequiresPasswordChange);
            Assert.Null(
                body.AccessToken);
            Assert.False(
                string.IsNullOrWhiteSpace(
                    body.PasswordChangeToken));

            string refreshCookie =
                AssertSingleRefreshSetCookie(
                    response);

            AssertRefreshCookieDeletion(
                refreshCookie);
        }
        finally
        {
            await _databaseFixture
                .RestoreTestPasswordAsync();
        }
    }

    [Fact]
    public async Task Refresh_WhenCookieIsMissing_ReturnsUnauthorizedAndClearsCookie()
    {
        // Act
        HttpResponseMessage response =
            await _client.PostAsync(
                "/api/auth/refresh",
                content: null);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        ProblemDetails? problem =
            await response.Content
                .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.True(
            problem.Extensions.TryGetValue(
                "errorCode",
                out object? errorCode));
        Assert.Equal(
            "invalid_refresh_token",
            errorCode?.ToString());

        string refreshCookie =
            AssertSingleRefreshSetCookie(
                response);

        AssertRefreshCookieDeletion(
            refreshCookie);
    }

    [Fact]
    public async Task Refresh_WhenCookieIsValid_ReturnsNewAccessTokenAndReplacesRefreshCookie()
    {
        // Arrange
        await _databaseFixture
            .ResetLoginStateAsync();

        LoginRequest loginRequest =
            new()
            {
                EmailAddress =
                    AuthenticationDatabaseFixture
                        .TestEmailAddress,

                Password =
                    AuthenticationDatabaseFixture
                        .TestPassword
            };

        HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        string originalCookie =
            AssertSingleRefreshSetCookie(
                loginResponse);

        string originalCookiePair =
            ExtractCookiePair(
                originalCookie);

        // Act
        using HttpRequestMessage refreshRequest =
            new(
                HttpMethod.Post,
                "/api/auth/refresh");

        refreshRequest.Headers.Add(
            "Cookie",
            originalCookiePair);

        HttpResponseMessage response =
            await _client.SendAsync(
                refreshRequest);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

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
        Assert.Null(
            body.PasswordChangeToken);
        Assert.Equal(
            _databaseFixture
                .SuperAdministratorUserId,
            body.User.UserId);

        string replacementCookie =
            AssertSingleRefreshSetCookie(
                response);

        Assert.Contains(
            "httponly",
            replacementCookie,
            StringComparison.OrdinalIgnoreCase);

        Assert.NotEqual(
            ExtractCookieValue(originalCookie),
            ExtractCookieValue(replacementCookie));
    }

    [Fact]
    public async Task Logout_WhenCookieExists_RevokesSessionAndClearsCookie()
    {
        // Arrange
        await _databaseFixture
            .ResetLoginStateAsync();

        LoginRequest loginRequest =
            new()
            {
                EmailAddress =
                    AuthenticationDatabaseFixture
                        .TestEmailAddress,

                Password =
                    AuthenticationDatabaseFixture
                        .TestPassword
            };

        HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        string refreshCookie =
            AssertSingleRefreshSetCookie(
                loginResponse);

        string refreshCookiePair =
            ExtractCookiePair(
                refreshCookie);

        using HttpRequestMessage logoutRequest =
            new(
                HttpMethod.Post,
                "/api/auth/logout");

        logoutRequest.Headers.Add(
            "Cookie",
            refreshCookiePair);

        // Act
        HttpResponseMessage response =
            await _client.SendAsync(
                logoutRequest);

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        string deletedCookie =
            AssertSingleRefreshSetCookie(
                response);

        AssertRefreshCookieDeletion(
            deletedCookie);

        using HttpRequestMessage refreshRequest =
            new(
                HttpMethod.Post,
                "/api/auth/refresh");

        refreshRequest.Headers.Add(
            "Cookie",
            refreshCookiePair);

        HttpResponseMessage refreshResponse =
            await _client.SendAsync(
                refreshRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            refreshResponse.StatusCode);
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

    [Fact]
    public async Task GetCurrentUser_WhenUserBecomesInactiveAfterTokenIsIssued_ReturnsUnauthorized()
    {
        await AssertCurrentUserIsUnauthorizedAfterTokenStateChangeAsync(
            prepareTokenStateAsync:
                () => _databaseFixture.SetTestUserActiveAsync(
                    isActive: true),
            changeTokenStateAsync:
                () => _databaseFixture.SetTestUserActiveAsync(
                    isActive: false),
            restoreTokenStateAsync:
                () => _databaseFixture.SetTestUserActiveAsync(
                    isActive: true));
    }

    [Fact]
    public async Task GetCurrentUser_WhenRoleBecomesInactiveAfterTokenIsIssued_ReturnsUnauthorized()
    {
        await AssertCurrentUserIsUnauthorizedAfterTokenStateChangeAsync(
            prepareTokenStateAsync:
                () => _databaseFixture.SetTestUserRoleActiveAsync(
                    isActive: true),
            changeTokenStateAsync:
                () => _databaseFixture.SetTestUserRoleActiveAsync(
                    isActive: false),
            restoreTokenStateAsync:
                () => _databaseFixture.SetTestUserRoleActiveAsync(
                    isActive: true));
    }

    [Fact]
    public async Task GetCurrentUser_WhenEmployeeBecomesInactiveAfterTokenIsIssued_ReturnsUnauthorized()
    {
        await AssertCurrentUserIsUnauthorizedAfterTokenStateChangeAsync(
            prepareTokenStateAsync:
                _databaseFixture.RemoveTestEmployeeAsync,
            changeTokenStateAsync:
                _databaseFixture.CreateInactiveTestEmployeeAsync,
            restoreTokenStateAsync:
                _databaseFixture.RemoveTestEmployeeAsync);
    }

    private async Task
        AssertCurrentUserIsUnauthorizedAfterTokenStateChangeAsync(
            Func<Task> prepareTokenStateAsync,
            Func<Task> changeTokenStateAsync,
            Func<Task> restoreTokenStateAsync)
    {
        await prepareTokenStateAsync();

        await _databaseFixture
            .ResetLoginStateAsync();

        string accessToken =
            await LoginAndGetAccessTokenAsync();

        try
        {
            await changeTokenStateAsync();

            HttpResponseMessage response =
                await SendCurrentUserAsync(
                    accessToken);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }
        finally
        {
            await restoreTokenStateAsync();

            await _databaseFixture
                .ResetLoginStateAsync();
        }
    }

    private async Task<HttpResponseMessage>
SendResetPasswordAsync(
    string token,
    string newPassword,
    Guid correlationId)
    {
        ResetPasswordRequest requestBody =
            new()
            {
                Token = token,
                NewPassword = newPassword,
                ConfirmNewPassword =
                    newPassword
            };

        using HttpRequestMessage request =
            new(
                HttpMethod.Post,
                "/api/auth/reset-password");

        request.Headers.Add(
            "X-Correlation-ID",
            correlationId.ToString());

        request.Content =
            JsonContent.Create(
                requestBody);

        return await _client.SendAsync(
            request);
    }

    private async Task<HttpResponseMessage>
    SendCurrentUserAsync(
        string accessToken)
    {
        using HttpRequestMessage request =
            new(
                HttpMethod.Get,
                "/api/auth/me");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                scheme: "Bearer",
                parameter: accessToken);

        return await _client.SendAsync(
            request);
    }

    private static string AssertSingleRefreshSetCookie(
        HttpResponseMessage response)
    {
        Assert.True(
            response.Headers.TryGetValues(
                "Set-Cookie",
                out IEnumerable<string>?
                    setCookieValues));

        string refreshCookie =
            Assert.Single(
                setCookieValues,
                value =>
                    value.StartsWith(
                        RefreshTokenCookieName + "=",
                        StringComparison.Ordinal));

        return refreshCookie;
    }

    private static string ExtractCookiePair(
        string setCookieHeader)
    {
        int separatorIndex =
            setCookieHeader.IndexOf(
                ';',
                StringComparison.Ordinal);

        return separatorIndex < 0
            ? setCookieHeader
            : setCookieHeader[..separatorIndex];
    }

    private static string ExtractCookieValue(
        string setCookieHeader)
    {
        string cookiePair =
            ExtractCookiePair(
                setCookieHeader);

        string prefix =
            RefreshTokenCookieName + "=";

        Assert.StartsWith(
            prefix,
            cookiePair,
            StringComparison.Ordinal);

        return cookiePair[prefix.Length..];
    }

    private static void AssertRefreshCookieDeletion(
        string setCookieHeader)
    {
        Assert.Equal(
            string.Empty,
            ExtractCookieValue(
                setCookieHeader));

        Assert.Contains(
            "expires=",
            setCookieHeader,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "httponly",
            setCookieHeader,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "secure",
            setCookieHeader,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "samesite=none",
            setCookieHeader,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "path=/",
            setCookieHeader,
            StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public async Task ResetPassword_WhenTokenAndPasswordAreValid_ChangesPasswordAndRegistersAudit()
    {
        // Arrange
        await _databaseFixture
            .RestoreTestPasswordAsync();

        GeneratedPasswordResetToken resetToken =
            await _databaseFixture
                .CreatePasswordResetTokenAsync(
                    "/integration-tests/" +
                    "create-reset-token");

        Guid correlationId =
            Guid.NewGuid();

        DateTime startedAtUtc =
            DateTime.UtcNow.AddSeconds(-2);

        try
        {
            // Act
            HttpResponseMessage response =
                await SendResetPasswordAsync(
                    token:
                        resetToken.Token,
                    newPassword:
                        AuthenticationDatabaseFixture
                            .ChangedTestPassword,
                    correlationId:
                        correlationId);

            DateTime completedAtUtc =
                DateTime.UtcNow.AddSeconds(2);

            // Assert: HTTP
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

            // La contraseña anterior ya no funciona.
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

            // La nueva sí funciona.
            string accessToken =
                await LoginAndGetAccessTokenAsync(
                    AuthenticationDatabaseFixture
                        .ChangedTestPassword);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    accessToken));

            // Assert: persistencia
            AuthenticationUserData? user =
                await _databaseFixture.Repository
                    .GetUserForAuthenticationByIdAsync(
                        _databaseFixture
                            .SuperAdministratorUserId,
                        CancellationToken.None);

            Assert.NotNull(user);

            Assert.True(
                _databaseFixture.PasswordService
                    .VerifyPassword(
                        user.PasswordHash,
                        AuthenticationDatabaseFixture
                            .ChangedTestPassword));

            Assert.False(
                user.RequiresPasswordChange);

            Assert.Null(
                user.TemporaryPasswordExpiresAtUtc);

            Assert.Equal(
                0,
                user.FailedLoginAttempts);

            Assert.Null(
                user.LockoutEndAtUtc);

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
                "PasswordResetCompleted",
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
                "Anonymous",
                auditLog.ActorType);

            Assert.Null(
                auditLog.ActorUserId);

            Assert.Equal(
                AuthenticationDatabaseFixture
                    .TestEmailAddress,
                auditLog.ActorEmailAddress);

            Assert.True(
                auditLog.IsSuccessful);

            Assert.Equal(
                "POST",
                auditLog.HttpMethod);

            Assert.Equal(
                "/api/auth/reset-password",
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
    public async Task ResetPassword_WhenTokenWasAlreadyUsed_ReturnsGenericUnavailableError()
    {
        // Arrange
        await _databaseFixture
            .RestoreTestPasswordAsync();

        GeneratedPasswordResetToken resetToken =
            await _databaseFixture
                .CreatePasswordResetTokenAsync(
                    "/integration-tests/" +
                    "create-single-use-reset-token");

        try
        {
            HttpResponseMessage firstResponse =
                await SendResetPasswordAsync(
                    resetToken.Token,
                    AuthenticationDatabaseFixture
                        .ChangedTestPassword,
                    Guid.NewGuid());

            Assert.Equal(
                HttpStatusCode.NoContent,
                firstResponse.StatusCode);

            // Act: mismo token una segunda vez
            HttpResponseMessage secondResponse =
                await SendResetPasswordAsync(
                    resetToken.Token,
                    "AnotherIntegration3!",
                    Guid.NewGuid());

            // Assert
            Assert.Equal(
                HttpStatusCode.BadRequest,
                secondResponse.StatusCode);

            ProblemDetails? problem =
                await secondResponse.Content
                    .ReadFromJsonAsync<
                        ProblemDetails>();

            Assert.NotNull(problem);

            Assert.True(
                problem.Extensions.TryGetValue(
                    "errorCode",
                    out object? errorCode));

            Assert.Equal(
                "password_reset_not_available",
                errorCode?.ToString());

            // La segunda petición NO cambió otra vez
            // la contraseña.
            string accessToken =
                await LoginAndGetAccessTokenAsync(
                    AuthenticationDatabaseFixture
                        .ChangedTestPassword);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    accessToken));
        }
        finally
        {
            await _databaseFixture
                .RestoreTestPasswordAsync();
        }
    }

    [Fact]
    public async Task ResetPassword_WhenTokenDoesNotExist_ReturnsGenericUnavailableError()
    {
        // Arrange
        GeneratedPasswordResetToken nonexistentToken =
            _databaseFixture
                .PasswordResetTokenService
                .GenerateToken();

        // El token se genera pero deliberadamente
        // NO se guarda en Security.PasswordResetTokens.

        // Act
        HttpResponseMessage response =
            await SendResetPasswordAsync(
                nonexistentToken.Token,
                AuthenticationDatabaseFixture
                    .ChangedTestPassword,
                Guid.NewGuid());

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        ProblemDetails? problem =
            await response.Content
                .ReadFromJsonAsync<
                    ProblemDetails>();

        Assert.NotNull(problem);

        Assert.True(
            problem.Extensions.TryGetValue(
                "errorCode",
                out object? errorCode));

        Assert.Equal(
            "password_reset_not_available",
            errorCode?.ToString());
    }

    [Fact]
    public async Task ResetPassword_WhenNewPasswordMatchesCurrentPassword_DoesNotConsumeToken()
    {
        // Arrange
        await _databaseFixture
            .RestoreTestPasswordAsync();

        GeneratedPasswordResetToken resetToken =
            await _databaseFixture
                .CreatePasswordResetTokenAsync(
                    "/integration-tests/" +
                    "create-password-reuse-token");

        try
        {
            // Act 1: intenta reutilizar la actual
            HttpResponseMessage reuseResponse =
                await SendResetPasswordAsync(
                    resetToken.Token,
                    AuthenticationDatabaseFixture
                        .TestPassword,
                    Guid.NewGuid());

            // Assert 1
            Assert.Equal(
                HttpStatusCode.BadRequest,
                reuseResponse.StatusCode);

            ProblemDetails? problem =
                await reuseResponse.Content
                    .ReadFromJsonAsync<
                        ProblemDetails>();

            Assert.NotNull(problem);

            Assert.True(
                problem.Extensions.TryGetValue(
                    "errorCode",
                    out object? errorCode));

            Assert.Equal(
                "password_reuse_not_allowed",
                errorCode?.ToString());

            /*
             * Act 2:
             * usamos EL MISMO TOKEN con una contraseña
             * diferente.
             *
             * Si ahora funciona, demostramos que el
             * intento anterior no consumió el token.
             */
            HttpResponseMessage validResponse =
                await SendResetPasswordAsync(
                    resetToken.Token,
                    AuthenticationDatabaseFixture
                        .ChangedTestPassword,
                    Guid.NewGuid());

            // Assert 2
            Assert.Equal(
                HttpStatusCode.NoContent,
                validResponse.StatusCode);

            string accessToken =
                await LoginAndGetAccessTokenAsync(
                    AuthenticationDatabaseFixture
                        .ChangedTestPassword);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    accessToken));
        }
        finally
        {
            await _databaseFixture
                .RestoreTestPasswordAsync();
        }
    }
}
