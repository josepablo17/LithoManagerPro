using LithoManager.Api.Contracts.Authentication;
using LithoManager.Api.Contracts.HumanResources
    .Departments;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Departments;
using LithoManager.IntegrationTests
    .Api.Infrastructure;
using LithoManager.IntegrationTests.Collections;
using LithoManager.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LithoManager.IntegrationTests.Api
    .HumanResources.Departments;

[Collection(
    AuthenticationDatabaseCollection.Name)]
public sealed class DepartmentEndpointsTests
{
    private readonly AuthenticationDatabaseFixture
        _databaseFixture;

    private readonly HttpClient _client;

    public DepartmentEndpointsTests(
        AuthenticationDatabaseFixture databaseFixture,
        LithoManagerWebApplicationFactory applicationFactory)
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
                    BaseAddress =
                        new Uri(
                            "https://localhost"),

                    AllowAutoRedirect = false
                });
    }

    [Fact]
    public async Task CreateDepartment_WhenAccessTokenAndRequestAreValid_ReturnsCreatedDepartmentAndRegistersAudit()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string departmentName =
            CreateDepartmentName();

        Guid correlationId =
            Guid.NewGuid();

        await _databaseFixture
            .RemoveDepartmentTestDataAsync(
                departmentCode);

        CreateDepartmentRequest requestBody =
            new()
            {
                DepartmentCode =
                    departmentCode,
                Name =
                    departmentName,
                Description =
                    "Created by API integration tests."
            };

        try
        {
            string accessToken =
                await LoginAndGetAccessTokenAsync();

            using HttpRequestMessage request =
                CreateAuthorizedJsonRequest(
                    HttpMethod.Post,
                    "/api/human-resources/departments",
                    accessToken,
                    correlationId,
                    requestBody);

            // Act
            HttpResponseMessage response =
                await _client.SendAsync(
                    request);

            // Assert: respuesta HTTP
            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);

            Assert.True(
                response.Headers.TryGetValues(
                    "X-Correlation-ID",
                    out IEnumerable<string>?
                        correlationValues));

            Assert.Contains(
                correlationId.ToString(),
                correlationValues);

            DepartmentResponse? department =
                await response.Content
                    .ReadFromJsonAsync<DepartmentResponse>();

            Assert.NotNull(department);
            Assert.True(department.DepartmentId > 0);
            Assert.Equal(
                departmentCode,
                department.DepartmentCode);
            Assert.Equal(
                departmentName,
                department.Name);
            Assert.True(department.IsActive);
            Assert.Equal(
                _databaseFixture
                    .SuperAdministratorUserId,
                department.CreatedByUserId);
            Assert.Equal(
                8,
                Convert.FromBase64String(
                    department.RowVersion).Length);

            Assert.NotNull(
                response.Headers.Location);
            Assert.Contains(
                department.DepartmentId.ToString(),
                response.Headers.Location!.ToString());

            AuditLogTestData? audit =
                await _databaseFixture
                    .GetAuditLogByCorrelationIdAsync(
                        correlationId);

            Assert.NotNull(audit);
            Assert.Equal(
                "HumanResources",
                audit.ModuleName);
            Assert.Equal(
                "DepartmentCreated",
                audit.ActionName);
            Assert.Equal(
                "Departments",
                audit.EntityName);
            Assert.Equal(
                department.DepartmentId.ToString(),
                audit.EntityId);
        }
        finally
        {
            await _databaseFixture
                .RemoveDepartmentTestDataAsync(
                    departmentCode);
        }
    }

    [Fact]
    public async Task UpdateDepartment_WhenRowVersionIsStale_ReturnsConflict()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        await _databaseFixture
            .RemoveDepartmentTestDataAsync(
                departmentCode);

        try
        {
            DepartmentData department =
                await CreateDepartmentAsync(
                    departmentCode,
                    CreateDepartmentName());

            await _databaseFixture.DepartmentRepository
                .UpdateDepartmentAsync(
                    departmentId:
                        department.DepartmentId,
                    departmentCode:
                        department.DepartmentCode,
                    name:
                        department.Name + " Updated",
                    description:
                        department.Description,
                    expectedRowVersion:
                        department.RowVersion,
                    actorUserId:
                        _databaseFixture
                            .SuperAdministratorUserId,
                    requestContext:
                        CreateRequestContext(
                            "/integration-tests/" +
                            "departments/api-stale-first-update"),
                    cancellationToken:
                        CancellationToken.None);

            string accessToken =
                await LoginAndGetAccessTokenAsync();

            UpdateDepartmentRequest requestBody =
                new()
                {
                    DepartmentCode =
                        department.DepartmentCode,
                    Name =
                        department.Name + " Stale",
                    Description =
                        department.Description,
                    ExpectedRowVersion =
                        Convert.ToBase64String(
                            department.RowVersion)
                };

            using HttpRequestMessage request =
                CreateAuthorizedJsonRequest(
                    HttpMethod.Put,
                    "/api/human-resources/departments/" +
                    department.DepartmentId,
                    accessToken,
                    Guid.NewGuid(),
                    requestBody);

            // Act
            HttpResponseMessage response =
                await _client.SendAsync(
                    request);

            // Assert
            Assert.Equal(
                HttpStatusCode.Conflict,
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
                "concurrency_conflict",
                errorCode?.ToString());
        }
        finally
        {
            await _databaseFixture
                .RemoveDepartmentTestDataAsync(
                    departmentCode);
        }
    }

    [Fact]
    public async Task SetDepartmentStatus_WhenDepartmentHasActiveEmployees_ReturnsConflict()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            "DEPT" + Guid.NewGuid()
                .ToString("N")[..12]
                .ToUpperInvariant();

        await _databaseFixture
            .RemoveDepartmentTestDataAsync(
                departmentCode,
                identificationNumber);

        try
        {
            DepartmentData department =
                await CreateDepartmentAsync(
                    departmentCode,
                    CreateDepartmentName());

            await _databaseFixture
                .CreateActiveEmployeeForDepartmentAsync(
                    department.DepartmentId,
                    identificationNumber);

            string accessToken =
                await LoginAndGetAccessTokenAsync();

            SetDepartmentStatusRequest requestBody =
                new()
                {
                    IsActive =
                        false,
                    ExpectedRowVersion =
                        Convert.ToBase64String(
                            department.RowVersion)
                };

            using HttpRequestMessage request =
                CreateAuthorizedJsonRequest(
                    HttpMethod.Patch,
                    "/api/human-resources/departments/" +
                    department.DepartmentId +
                    "/status",
                    accessToken,
                    Guid.NewGuid(),
                    requestBody);

            // Act
            HttpResponseMessage response =
                await _client.SendAsync(
                    request);

            // Assert
            Assert.Equal(
                HttpStatusCode.Conflict,
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
                "department_has_active_employees",
                errorCode?.ToString());
        }
        finally
        {
            await _databaseFixture
                .RemoveDepartmentTestDataAsync(
                    departmentCode,
                    identificationNumber);
        }
    }

    private Task<DepartmentData> CreateDepartmentAsync(
        string departmentCode,
        string departmentName)
    {
        return _databaseFixture.DepartmentRepository
            .CreateDepartmentAsync(
                departmentCode:
                    departmentCode,
                name:
                    departmentName,
                description:
                    "Created by API integration tests.",
                actorUserId:
                    _databaseFixture
                        .SuperAdministratorUserId,
                requestContext:
                    CreateRequestContext(
                        "/integration-tests/" +
                        "departments/api-create-helper"),
                cancellationToken:
                    CancellationToken.None);
    }

    private static AuthenticationRequestContext
        CreateRequestContext(
            string requestPath)
    {
        return AuthenticationDatabaseFixture
            .CreateRequestContext(requestPath);
    }

    private async Task<string>
        LoginAndGetAccessTokenAsync()
    {
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

        LoginResponse? loginBody =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginBody);

        Assert.False(
            string.IsNullOrWhiteSpace(
                loginBody.AccessToken));

        return loginBody.AccessToken;
    }

    private static HttpRequestMessage
        CreateAuthorizedJsonRequest<TRequest>(
            HttpMethod method,
            string requestUri,
            string accessToken,
            Guid correlationId,
            TRequest body)
    {
        HttpRequestMessage request =
            new(
                method,
                requestUri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                scheme: "Bearer",
                parameter: accessToken);

        request.Headers.Add(
            "X-Correlation-ID",
            correlationId.ToString());

        request.Content =
            JsonContent.Create(
                body);

        return request;
    }

    private static string CreateDepartmentCode()
    {
        return "IT" + Guid.NewGuid()
            .ToString("N")[..12]
            .ToUpperInvariant();
    }

    private static string CreateDepartmentName()
    {
        return "API Department " +
            Guid.NewGuid().ToString("N")[..12];
    }
}
