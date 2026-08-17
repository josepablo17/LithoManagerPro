using LithoManager.Api.Contracts.Authentication;
using LithoManager.Api.Contracts.HumanResources
    .Employees;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Departments;
using LithoManager.Application.Features
    .HumanResources.Employees;
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
    .HumanResources.Employees;

[Collection(
    AuthenticationDatabaseCollection.Name)]
public sealed class EmployeeEndpointsTests
{
    private readonly AuthenticationDatabaseFixture
        _databaseFixture;

    private readonly HttpClient _client;

    public EmployeeEndpointsTests(
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
    public async Task CreateEmployee_WhenAccessTokenAndRequestAreValid_ReturnsCreatedEmployeeAndRegistersAudit()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

        Guid correlationId =
            Guid.NewGuid();

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

            CreateEmployeeRequest requestBody =
                new()
                {
                    UserId =
                        null,
                    DepartmentId =
                        department.DepartmentId,
                    IdentificationType =
                        "CEDULA_FISICA",
                    IdentificationNumber =
                        identificationNumber,
                    FirstName =
                        "Ana",
                    LastName =
                        "Rivera",
                    PhoneNumber =
                        "55550101",
                    BirthDate =
                        new DateTime(
                            1990,
                            1,
                            15),
                    HireDate =
                        new DateTime(
                            2026,
                            8,
                            9),
                    TerminationDate =
                        null,
                    JobTitle =
                        "HR Specialist",
                    BaseSalary =
                        1200.00m,
                    ProfileImagePath =
                        null
                };

            string accessToken =
                await LoginAndGetAccessTokenAsync();

            using HttpRequestMessage request =
                CreateAuthorizedJsonRequest(
                    HttpMethod.Post,
                    "/api/human-resources/employees",
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

            EmployeeResponse? employee =
                await response.Content
                    .ReadFromJsonAsync<EmployeeResponse>();

            Assert.NotNull(employee);
            Assert.True(employee.EmployeeId > 0);
            Assert.Null(employee.UserId);
            Assert.Null(employee.EmailAddress);
            Assert.Equal(
                department.DepartmentId,
                employee.DepartmentId);
            Assert.Equal(
                "CEDULA_FISICA",
                employee.IdentificationType);
            Assert.Equal(
                identificationNumber,
                employee.IdentificationNumber);
            Assert.True(employee.IsActive);
            Assert.Equal(
                _databaseFixture
                    .SuperAdministratorUserId,
                employee.CreatedByUserId);
            Assert.Equal(
                8,
                Convert.FromBase64String(
                    employee.RowVersion).Length);

            Assert.NotNull(
                response.Headers.Location);
            Assert.Contains(
                employee.EmployeeId.ToString(),
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
                "EmployeeCreated",
                audit.ActionName);
            Assert.Equal(
                "Employees",
                audit.EntityName);
            Assert.Equal(
                employee.EmployeeId.ToString(),
                audit.EntityId);
        }
        finally
        {
            await _databaseFixture
                .RemoveDepartmentTestDataAsync(
                    departmentCode,
                    identificationNumber);
        }
    }

    [Fact]
    public async Task UpdateEmployee_WhenRowVersionIsStale_ReturnsConflict()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

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

            EmployeeData employee =
                await CreateEmployeeAsync(
                    department.DepartmentId,
                    identificationNumber);

            await _databaseFixture.EmployeeRepository
                .UpdateEmployeeAsync(
                    employeeId:
                        employee.EmployeeId,
                    userId:
                        null,
                    departmentId:
                        department.DepartmentId,
                    identificationType:
                        employee.IdentificationType,
                    identificationNumber:
                        employee.IdentificationNumber,
                    firstName:
                        employee.FirstName,
                    lastName:
                        employee.LastName + " Updated",
                    phoneNumber:
                        employee.PhoneNumber,
                    birthDate:
                        employee.BirthDate,
                    hireDate:
                        employee.HireDate,
                    terminationDate:
                        employee.TerminationDate,
                    jobTitle:
                        employee.JobTitle,
                    baseSalary:
                        employee.BaseSalary,
                    profileImagePath:
                        employee.ProfileImagePath,
                    expectedRowVersion:
                        employee.RowVersion,
                    actorUserId:
                        _databaseFixture
                            .SuperAdministratorUserId,
                    requestContext:
                        CreateRequestContext(
                            "/integration-tests/" +
                            "employees/api-stale-first-update"),
                    cancellationToken:
                        CancellationToken.None);

            string accessToken =
                await LoginAndGetAccessTokenAsync();

            UpdateEmployeeRequest requestBody =
                new()
                {
                    UserId =
                        null,
                    DepartmentId =
                        department.DepartmentId,
                    IdentificationType =
                        employee.IdentificationType,
                    IdentificationNumber =
                        employee.IdentificationNumber,
                    FirstName =
                        employee.FirstName,
                    LastName =
                        employee.LastName + " Stale",
                    PhoneNumber =
                        employee.PhoneNumber,
                    BirthDate =
                        employee.BirthDate,
                    HireDate =
                        employee.HireDate,
                    TerminationDate =
                        employee.TerminationDate,
                    JobTitle =
                        employee.JobTitle,
                    BaseSalary =
                        employee.BaseSalary,
                    ProfileImagePath =
                        employee.ProfileImagePath,
                    ExpectedRowVersion =
                        Convert.ToBase64String(
                            employee.RowVersion)
                };

            using HttpRequestMessage request =
                CreateAuthorizedJsonRequest(
                    HttpMethod.Put,
                    "/api/human-resources/employees/" +
                    employee.EmployeeId,
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
                    departmentCode,
                    identificationNumber);
        }
    }

    [Fact]
    public async Task GetEmployeeIdentificationTypes_WhenAccessTokenIsValid_ReturnsIdentificationTypes()
    {
        // Arrange
        string accessToken =
            await LoginAndGetAccessTokenAsync();

        using HttpRequestMessage request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/human-resources/employees/" +
                "identification-types",
                accessToken,
                Guid.NewGuid());

        // Act
        HttpResponseMessage response =
            await _client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        IReadOnlyList<EmployeeIdentificationTypeResponse>?
            identificationTypes =
                await response.Content.ReadFromJsonAsync<
                    IReadOnlyList<
                        EmployeeIdentificationTypeResponse>>();

        Assert.NotNull(identificationTypes);

        Assert.Contains(
            identificationTypes,
            identificationType =>
                identificationType.IdentificationType
                    == "CEDULA_FISICA"
                && identificationType.IsNumericOnly
                && identificationType.MinLength == 9
                && identificationType.MaxLength == 9);

        Assert.Contains(
            identificationTypes,
            identificationType =>
                identificationType.IdentificationType
                    == "DIMEX");

        Assert.Contains(
            identificationTypes,
            identificationType =>
                identificationType.IdentificationType
                    == "PASAPORTE"
                && !identificationType.IsNumericOnly);
    }

    [Fact]
    public async Task GetEmployeeSalaryHistory_WhenEmployeeExists_ReturnsSalaryHistory()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

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

            EmployeeData employee =
                await CreateEmployeeAsync(
                    department.DepartmentId,
                    identificationNumber);

            string accessToken =
                await LoginAndGetAccessTokenAsync();

            using HttpRequestMessage request =
                CreateAuthorizedRequest(
                    HttpMethod.Get,
                    "/api/human-resources/employees/" +
                    employee.EmployeeId +
                    "/salary-history",
                    accessToken,
                    Guid.NewGuid());

            // Act
            HttpResponseMessage response =
                await _client.SendAsync(request);

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            IReadOnlyList<EmployeeSalaryHistoryResponse>?
                salaryHistory =
                    await response.Content.ReadFromJsonAsync<
                        IReadOnlyList<
                            EmployeeSalaryHistoryResponse>>();

            Assert.NotNull(salaryHistory);

            EmployeeSalaryHistoryResponse currentSalary =
                Assert.Single(salaryHistory);

            Assert.Equal(
                employee.EmployeeId,
                currentSalary.EmployeeId);
            Assert.Equal(
                employee.IdentificationType,
                currentSalary.IdentificationType);
            Assert.Equal(
                identificationNumber,
                currentSalary.IdentificationNumber);
            Assert.Equal(
                employee.BaseSalary,
                currentSalary.BaseSalary);
            Assert.True(currentSalary.IsCurrent);
            Assert.Equal(
                8,
                Convert.FromBase64String(
                    currentSalary.RowVersion).Length);
        }
        finally
        {
            await _databaseFixture
                .RemoveDepartmentTestDataAsync(
                    departmentCode,
                    identificationNumber);
        }
    }

    [Fact]
    public async Task GetEmployeeSalaryHistory_WhenDateRangeIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        string accessToken =
            await LoginAndGetAccessTokenAsync();

        using HttpRequestMessage request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/human-resources/employees/1/" +
                "salary-history?effectiveFromDate=2026-08-10" +
                "&effectiveToDate=2026-08-09",
                accessToken,
                Guid.NewGuid());

        // Act
        HttpResponseMessage response =
            await _client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
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
            "invalid_request",
            errorCode?.ToString());
    }

    private Task<EmployeeData> CreateEmployeeAsync(
        int departmentId,
        string identificationNumber)
    {
        return _databaseFixture.EmployeeRepository
            .CreateEmployeeAsync(
                userId:
                    null,
                departmentId:
                    departmentId,
                identificationType:
                    "CEDULA_FISICA",
                identificationNumber:
                    identificationNumber,
                firstName:
                    "Ana",
                lastName:
                    "Rivera",
                phoneNumber:
                    "55550101",
                birthDate:
                    new DateTime(
                        1990,
                        1,
                        15),
                hireDate:
                    new DateTime(
                        2026,
                        8,
                        9),
                terminationDate:
                    null,
                jobTitle:
                    "HR Specialist",
                baseSalary:
                    1200.00m,
                profileImagePath:
                    null,
                actorUserId:
                    _databaseFixture
                        .SuperAdministratorUserId,
                requestContext:
                    CreateRequestContext(
                        "/integration-tests/" +
                        "employees/api-create-helper"),
                cancellationToken:
                    CancellationToken.None);
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
                    "Created by employee API integration tests.",
                actorUserId:
                    _databaseFixture
                        .SuperAdministratorUserId,
                requestContext:
                    CreateRequestContext(
                        "/integration-tests/" +
                        "employees/api-department-helper"),
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

    private static HttpRequestMessage
        CreateAuthorizedRequest(
            HttpMethod method,
            string requestUri,
            string accessToken,
            Guid correlationId)
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

        return request;
    }

    private static string CreateDepartmentCode()
    {
        return "EA" + Guid.NewGuid()
            .ToString("N")[..12]
            .ToUpperInvariant();
    }

    private static string CreateDepartmentName()
    {
        return "API Employee Department " +
            Guid.NewGuid().ToString("N")[..12];
    }

    private static string CreateIdentificationNumber()
    {
        string digits = new(
            Guid.NewGuid()
                .ToString("N")
                .Where(char.IsDigit)
                .ToArray());

        return "2" + digits.PadRight(8, '0')[..8];
    }
}
