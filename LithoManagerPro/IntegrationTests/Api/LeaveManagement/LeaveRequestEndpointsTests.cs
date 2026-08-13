using LithoManager.Api.Contracts.Authentication;
using LithoManager.Api.Contracts.LeaveManagement;
using LithoManager.Application.Features
    .HumanResources.Departments;
using LithoManager.Application.Features.LeaveManagement;
using LithoManager.IntegrationTests
    .Api.Infrastructure;
using LithoManager.IntegrationTests.Collections;
using LithoManager.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace LithoManager.IntegrationTests.Api
    .LeaveManagement;

[Collection(
    AuthenticationDatabaseCollection.Name)]
public sealed class LeaveRequestEndpointsTests
{
    private readonly AuthenticationDatabaseFixture
        _databaseFixture;

    private readonly HttpClient _client;

    public LeaveRequestEndpointsTests(
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
    public async Task CreateLeaveRequest_WhenRequestIsValid_ReturnsCreatedRequestAndRegistersAudit()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

        Guid correlationId =
            Guid.NewGuid();

        await _databaseFixture
            .RemoveLeaveManagementTestDataAsync(
                identificationNumber);

        await _databaseFixture
            .RemoveDepartmentTestDataAsync(
                departmentCode,
                identificationNumber);

        try
        {
            int employeeId =
                await CreateAdministratorEmployeeAsync(
                    departmentCode,
                    identificationNumber);

            await _databaseFixture
                .LeaveManagementRepository
                .AdjustEmployeeLeaveBalanceAsync(
                    employeeId:
                        employeeId,
                    leaveTypeCode:
                        "Vacation",
                    adjustedDaysDelta:
                        12,
                    actorUserId:
                        _databaseFixture
                            .SuperAdministratorUserId,
                    requestContext:
                        AuthenticationDatabaseFixture
                            .CreateRequestContext(
                                "/integration-tests/" +
                                "leave-management/api/" +
                                "adjust-balance"),
                    cancellationToken:
                        CancellationToken.None);

            string accessToken =
                await LoginAndGetAccessTokenAsync();

            CreateLeaveRequestRequest requestBody =
                new()
                {
                    StartDate =
                        new DateTime(2026, 11, 9),
                    EndDate =
                        new DateTime(2026, 11, 11),
                    LeaveTypeCode =
                        null
                };

            using HttpRequestMessage request =
                CreateAuthorizedJsonRequest(
                    HttpMethod.Post,
                    "/api/leave-management/requests",
                    accessToken,
                    correlationId,
                    requestBody);

            // Act
            HttpResponseMessage response =
                await _client.SendAsync(
                    request);

            // Assert
            Assert.Equal(
                HttpStatusCode.Created,
                response.StatusCode);

            LeaveRequestResponse? leaveRequest =
                await response.Content
                    .ReadFromJsonAsync<
                        LeaveRequestResponse>();

            Assert.NotNull(leaveRequest);
            Assert.True(
                leaveRequest.LeaveRequestId > 0);
            Assert.Equal(
                employeeId,
                leaveRequest.EmployeeId);
            Assert.Equal(
                "Vacation",
                leaveRequest.LeaveTypeCode);
            Assert.Equal(
                "Pending",
                leaveRequest.LeaveRequestStatusCode);
            Assert.Equal(
                3,
                leaveRequest.RequestedDays);
            Assert.Equal(
                8,
                Convert.FromBase64String(
                    leaveRequest.RowVersion).Length);

            AuditLogTestData? audit =
                await _databaseFixture
                    .GetAuditLogByCorrelationIdAsync(
                        correlationId);

            Assert.NotNull(audit);
            Assert.Equal(
                "LeaveManagement",
                audit.ModuleName);
            Assert.Equal(
                "LeaveRequestCreated",
                audit.ActionName);
            Assert.Equal(
                leaveRequest.LeaveRequestId.ToString(),
                audit.EntityId);
        }
        finally
        {
            await _databaseFixture
                .RemoveLeaveManagementTestDataAsync(
                    identificationNumber);

            await _databaseFixture
                .RemoveDepartmentTestDataAsync(
                    departmentCode,
                    identificationNumber);
        }
    }

    private async Task<int>
        CreateAdministratorEmployeeAsync(
            string departmentCode,
            string identificationNumber)
    {
        DepartmentData department =
            await _databaseFixture.DepartmentRepository
                .CreateDepartmentAsync(
                    departmentCode:
                        departmentCode,
                    name:
                        "Leave Management API Tests",
                    description:
                        "Created by API integration tests.",
                    actorUserId:
                        _databaseFixture
                            .SuperAdministratorUserId,
                    requestContext:
                        AuthenticationDatabaseFixture
                            .CreateRequestContext(
                                "/integration-tests/" +
                                "leave-management/api/" +
                                "create-department"),
                    cancellationToken:
                        CancellationToken.None);

        await _databaseFixture
            .CreateActiveEmployeeForDepartmentAsync(
                department.DepartmentId,
                identificationNumber);

        return await _databaseFixture
            .GetEmployeeIdByIdentificationNumberAsync(
                identificationNumber);
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
        return "LA" + Guid.NewGuid()
            .ToString("N")[..12]
            .ToUpperInvariant();
    }

    private static string CreateIdentificationNumber()
    {
        return "LAPI-" + Guid.NewGuid()
            .ToString("N")[..12]
            .ToUpperInvariant();
    }
}
