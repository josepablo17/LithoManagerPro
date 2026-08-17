using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Departments;
using LithoManager.Application.Features.LeaveManagement;
using LithoManager.IntegrationTests.Collections;
using LithoManager.IntegrationTests.Fixtures;
using Xunit;

namespace LithoManager.IntegrationTests.Infrastructure
    .Persistence;

[Collection(
    AuthenticationDatabaseCollection.Name)]
public sealed class LeaveManagementRepositoryTests
{
    private readonly AuthenticationDatabaseFixture
        _fixture;

    public LeaveManagementRepositoryTests(
        AuthenticationDatabaseFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _fixture = fixture;
    }

    [Fact]
    public async Task CreateLeaveRequestAsync_WhenRequestIsValid_ReservesPendingDaysAndRegistersAudit()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

        Guid correlationId =
            Guid.NewGuid();

        await _fixture
            .RemoveLeaveManagementTestDataAsync(
                identificationNumber);

        await _fixture.RemoveDepartmentTestDataAsync(
            departmentCode,
            identificationNumber);

        try
        {
            int employeeId =
                await CreateAdministratorEmployeeAsync(
                    departmentCode,
                    identificationNumber);

            await AdjustBalanceAsync(
                employeeId,
                adjustedDaysDelta: 12);

            // Act
            LeaveRequestData leaveRequest =
                await _fixture.LeaveManagementRepository
                    .CreateLeaveRequestAsync(
                        startDate:
                            new DateTime(2026, 9, 14),
                        endDate:
                            new DateTime(2026, 9, 16),
                        actorUserId:
                            _fixture
                                .SuperAdministratorUserId,
                        leaveTypeCode:
                            "Vacation",
                        requestContext:
                            CreateRequestContext(
                                correlationId,
                                "/integration-tests/" +
                                "leave-management/create"),
                        cancellationToken:
                            CancellationToken.None);

            // Assert
            Assert.True(
                leaveRequest.LeaveRequestId > 0);
            Assert.Equal(
                employeeId,
                leaveRequest.EmployeeId);
            Assert.Equal(
                "Pending",
                leaveRequest.LeaveRequestStatusCode);
            Assert.Equal(
                3,
                leaveRequest.RequestedDays);
            Assert.Equal(
                8,
                leaveRequest.RowVersion.Length);

            EmployeeLeaveBalanceData? balance =
                await _fixture.LeaveManagementRepository
                    .GetEmployeeLeaveBalanceAsync(
                        employeeId:
                            employeeId,
                        leaveTypeCode:
                            "Vacation",
                        actorUserId:
                            _fixture
                                .SuperAdministratorUserId,
                        cancellationToken:
                            CancellationToken.None);

            Assert.NotNull(balance);
            Assert.Equal(12, balance.AdjustedDays);
            Assert.Equal(3, balance.PendingDays);
            Assert.Equal(0, balance.UsedDays);
            Assert.Equal(9, balance.AvailableDays);

            AuditLogTestData? audit =
                await _fixture
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
                "LeaveRequests",
                audit.EntityName);
            Assert.Equal(
                leaveRequest.LeaveRequestId.ToString(),
                audit.EntityId);
        }
        finally
        {
            await _fixture
                .RemoveLeaveManagementTestDataAsync(
                    identificationNumber);

            await _fixture.RemoveDepartmentTestDataAsync(
                departmentCode,
                identificationNumber);
        }
    }

    [Fact]
    public async Task CancelLeaveRequestAsync_WhenRequestIsPending_ReleasesPendingDays()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

        await _fixture
            .RemoveLeaveManagementTestDataAsync(
                identificationNumber);

        await _fixture.RemoveDepartmentTestDataAsync(
            departmentCode,
            identificationNumber);

        try
        {
            int employeeId =
                await CreateAdministratorEmployeeAsync(
                    departmentCode,
                    identificationNumber);

            await AdjustBalanceAsync(
                employeeId,
                adjustedDaysDelta: 12);

            LeaveRequestData leaveRequest =
                await CreateLeaveRequestAsync();

            // Act
            LeaveRequestData cancelled =
                await _fixture.LeaveManagementRepository
                    .CancelLeaveRequestAsync(
                        leaveRequestId:
                            leaveRequest.LeaveRequestId,
                        expectedRowVersion:
                            leaveRequest.RowVersion,
                        actorUserId:
                            _fixture
                                .SuperAdministratorUserId,
                        requestContext:
                            CreateRequestContext(
                                Guid.NewGuid(),
                                "/integration-tests/" +
                                "leave-management/cancel"),
                        cancellationToken:
                            CancellationToken.None);

            // Assert
            Assert.Equal(
                "Cancelled",
                cancelled.LeaveRequestStatusCode);

            EmployeeLeaveBalanceData? balance =
                await _fixture.LeaveManagementRepository
                    .GetEmployeeLeaveBalanceAsync(
                        employeeId:
                            employeeId,
                        leaveTypeCode:
                            "Vacation",
                        actorUserId:
                            _fixture
                                .SuperAdministratorUserId,
                        cancellationToken:
                            CancellationToken.None);

            Assert.NotNull(balance);
            Assert.Equal(0, balance.PendingDays);
            Assert.Equal(0, balance.UsedDays);
            Assert.Equal(12, balance.AvailableDays);
        }
        finally
        {
            await _fixture
                .RemoveLeaveManagementTestDataAsync(
                    identificationNumber);

            await _fixture.RemoveDepartmentTestDataAsync(
                departmentCode,
                identificationNumber);
        }
    }

    [Fact]
    public async Task RespondLeaveRequestAsync_WhenApproved_MovesPendingDaysToUsedDays()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

        await _fixture
            .RemoveLeaveManagementTestDataAsync(
                identificationNumber);

        await _fixture.RemoveDepartmentTestDataAsync(
            departmentCode,
            identificationNumber);

        try
        {
            int employeeId =
                await CreateAdministratorEmployeeAsync(
                    departmentCode,
                    identificationNumber);

            await AdjustBalanceAsync(
                employeeId,
                adjustedDaysDelta: 12);

            LeaveRequestData leaveRequest =
                await CreateLeaveRequestAsync();

            // Act
            LeaveRequestData approved =
                await _fixture.LeaveManagementRepository
                    .RespondLeaveRequestAsync(
                        leaveRequestId:
                            leaveRequest.LeaveRequestId,
                        isApproved:
                            true,
                        expectedRowVersion:
                            leaveRequest.RowVersion,
                        actorUserId:
                            _fixture
                                .SuperAdministratorUserId,
                        requestContext:
                            CreateRequestContext(
                                Guid.NewGuid(),
                                "/integration-tests/" +
                                "leave-management/approve"),
                        cancellationToken:
                            CancellationToken.None);

            // Assert
            Assert.Equal(
                "Approved",
                approved.LeaveRequestStatusCode);

            EmployeeLeaveBalanceData? balance =
                await _fixture.LeaveManagementRepository
                    .GetEmployeeLeaveBalanceAsync(
                        employeeId:
                            employeeId,
                        leaveTypeCode:
                            "Vacation",
                        actorUserId:
                            _fixture
                                .SuperAdministratorUserId,
                        cancellationToken:
                            CancellationToken.None);

            Assert.NotNull(balance);
            Assert.Equal(0, balance.PendingDays);
            Assert.Equal(3, balance.UsedDays);
            Assert.Equal(9, balance.AvailableDays);
        }
        finally
        {
            await _fixture
                .RemoveLeaveManagementTestDataAsync(
                    identificationNumber);

            await _fixture.RemoveDepartmentTestDataAsync(
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
            await _fixture.DepartmentRepository
                .CreateDepartmentAsync(
                    departmentCode:
                        departmentCode,
                    name:
                        "Leave Management Tests",
                    description:
                        "Created by integration tests.",
                    actorUserId:
                        _fixture
                            .SuperAdministratorUserId,
                    requestContext:
                        AuthenticationDatabaseFixture
                            .CreateRequestContext(
                                "/integration-tests/" +
                                "leave-management/" +
                                "create-department"),
                    cancellationToken:
                        CancellationToken.None);

        await _fixture
            .CreateActiveEmployeeForDepartmentAsync(
                department.DepartmentId,
                identificationNumber);

        return await _fixture
            .GetEmployeeIdByIdentificationNumberAsync(
                identificationNumber);
    }

    private Task<EmployeeLeaveBalanceData>
        AdjustBalanceAsync(
            int employeeId,
            decimal adjustedDaysDelta)
    {
        return _fixture.LeaveManagementRepository
            .AdjustEmployeeLeaveBalanceAsync(
                employeeId:
                    employeeId,
                leaveTypeCode:
                    "Vacation",
                adjustedDaysDelta:
                    adjustedDaysDelta,
                actorUserId:
                    _fixture.SuperAdministratorUserId,
                requestContext:
                    AuthenticationDatabaseFixture
                        .CreateRequestContext(
                            "/integration-tests/" +
                            "leave-management/" +
                            "adjust-balance"),
                cancellationToken:
                    CancellationToken.None);
    }

    private Task<LeaveRequestData>
        CreateLeaveRequestAsync()
    {
        return _fixture.LeaveManagementRepository
            .CreateLeaveRequestAsync(
                startDate:
                    new DateTime(2026, 10, 5),
                endDate:
                    new DateTime(2026, 10, 7),
                actorUserId:
                    _fixture.SuperAdministratorUserId,
                leaveTypeCode:
                    "Vacation",
                requestContext:
                    AuthenticationDatabaseFixture
                        .CreateRequestContext(
                            "/integration-tests/" +
                            "leave-management/" +
                            "create-request-helper"),
                cancellationToken:
                    CancellationToken.None);
    }

    private static AuthenticationRequestContext
        CreateRequestContext(
            Guid correlationId,
            string requestPath)
    {
        return new AuthenticationRequestContext(
            CorrelationId:
                correlationId,
            ClientIpAddress:
                "127.0.0.1",
            UserAgent:
                "LithoManager.IntegrationTests",
            RequestPath:
                requestPath);
    }

    private static string CreateDepartmentCode()
    {
        return "LM" + Guid.NewGuid()
            .ToString("N")[..12]
            .ToUpperInvariant();
    }

    private static string CreateIdentificationNumber()
    {
        return "LEAVE" + Guid.NewGuid()
            .ToString("N")[..12]
            .ToUpperInvariant();
    }
}
