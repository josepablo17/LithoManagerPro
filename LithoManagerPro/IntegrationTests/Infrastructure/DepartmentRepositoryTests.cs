using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Departments;
using LithoManager.IntegrationTests.Collections;
using LithoManager.IntegrationTests.Fixtures;
using Xunit;

namespace LithoManager.IntegrationTests.Infrastructure
    .Persistence;

[Collection(
    AuthenticationDatabaseCollection.Name)]
public sealed class DepartmentRepositoryTests
{
    private readonly AuthenticationDatabaseFixture
        _fixture;

    public DepartmentRepositoryTests(
        AuthenticationDatabaseFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _fixture = fixture;
    }

    [Fact]
    public async Task CreateDepartmentAsync_WhenRequestIsValid_PersistsDepartmentAndAudit()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string departmentName =
            CreateDepartmentName();

        AuthenticationRequestContext requestContext =
            CreateRequestContext(
                "/integration-tests/departments/create");

        await _fixture.RemoveDepartmentTestDataAsync(
            departmentCode);

        try
        {
            // Act
            DepartmentData department =
                await _fixture.DepartmentRepository
                    .CreateDepartmentAsync(
                        departmentCode:
                            departmentCode,
                        name:
                            departmentName,
                        description:
                            "Created by integration tests.",
                        actorUserId:
                            _fixture
                                .SuperAdministratorUserId,
                        requestContext:
                            requestContext,
                        cancellationToken:
                            CancellationToken.None);

            // Assert
            Assert.True(department.DepartmentId > 0);
            Assert.Equal(
                departmentCode,
                department.DepartmentCode);
            Assert.Equal(
                departmentName,
                department.Name);
            Assert.True(department.IsActive);
            Assert.Equal(
                _fixture.SuperAdministratorUserId,
                department.CreatedByUserId);
            Assert.Equal(8, department.RowVersion.Length);

            DepartmentData? persisted =
                await _fixture.DepartmentRepository
                    .GetDepartmentByIdAsync(
                        department.DepartmentId,
                        CancellationToken.None);

            Assert.NotNull(persisted);
            Assert.Equal(
                departmentCode,
                persisted.DepartmentCode);

            AuditLogTestData? audit =
                await _fixture
                    .GetAuditLogByCorrelationIdAsync(
                        requestContext.CorrelationId);

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
            await _fixture.RemoveDepartmentTestDataAsync(
                departmentCode);
        }
    }

    [Fact]
    public async Task GetDepartmentsAsync_WhenFilterMatches_ReturnsCreatedDepartment()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string departmentName =
            CreateDepartmentName();

        await _fixture.RemoveDepartmentTestDataAsync(
            departmentCode);

        try
        {
            await CreateDepartmentAsync(
                departmentCode,
                departmentName);

            // Act
            IReadOnlyList<DepartmentData> departments =
                await _fixture.DepartmentRepository
                    .GetDepartmentsAsync(
                        searchTerm:
                            departmentCode,
                        isActive:
                            true,
                        cancellationToken:
                            CancellationToken.None);

            // Assert
            DepartmentData department =
                Assert.Single(
                    departments,
                    item =>
                        item.DepartmentCode
                            == departmentCode);

            Assert.Equal(
                departmentName,
                department.Name);
            Assert.True(department.IsActive);
        }
        finally
        {
            await _fixture.RemoveDepartmentTestDataAsync(
                departmentCode);
        }
    }

    [Fact]
    public async Task UpdateDepartmentAsync_WhenRowVersionIsStale_ThrowsConcurrencyConflict()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        await _fixture.RemoveDepartmentTestDataAsync(
            departmentCode);

        try
        {
            DepartmentData department =
                await CreateDepartmentAsync(
                    departmentCode,
                    CreateDepartmentName());

            await _fixture.DepartmentRepository
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
                        _fixture
                            .SuperAdministratorUserId,
                    requestContext:
                        CreateRequestContext(
                            "/integration-tests/" +
                            "departments/stale-first-update"),
                    cancellationToken:
                        CancellationToken.None);

            // Act and assert
            DepartmentPersistenceException exception =
                await Assert.ThrowsAsync<
                    DepartmentPersistenceException>(
                        () => _fixture.DepartmentRepository
                            .UpdateDepartmentAsync(
                                departmentId:
                                    department.DepartmentId,
                                departmentCode:
                                    department.DepartmentCode,
                                name:
                                    department.Name +
                                    " Stale",
                                description:
                                    department.Description,
                                expectedRowVersion:
                                    department.RowVersion,
                                actorUserId:
                                    _fixture
                                        .SuperAdministratorUserId,
                                requestContext:
                                    CreateRequestContext(
                                        "/integration-tests/" +
                                        "departments/stale-second-update"),
                                cancellationToken:
                                    CancellationToken.None));

            Assert.Equal(
                DepartmentErrorCode.ConcurrencyConflict,
                exception.ErrorCode);
        }
        finally
        {
            await _fixture.RemoveDepartmentTestDataAsync(
                departmentCode);
        }
    }

    [Fact]
    public async Task SetDepartmentStatusAsync_WhenDepartmentHasActiveEmployees_ThrowsDepartmentHasActiveEmployees()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            "DEPT-" + Guid.NewGuid()
                .ToString("N")[..12]
                .ToUpperInvariant();

        await _fixture.RemoveDepartmentTestDataAsync(
            departmentCode,
            identificationNumber);

        try
        {
            DepartmentData department =
                await CreateDepartmentAsync(
                    departmentCode,
                    CreateDepartmentName());

            await _fixture
                .CreateActiveEmployeeForDepartmentAsync(
                    department.DepartmentId,
                    identificationNumber);

            // Act and assert
            DepartmentPersistenceException exception =
                await Assert.ThrowsAsync<
                    DepartmentPersistenceException>(
                        () => _fixture.DepartmentRepository
                            .SetDepartmentStatusAsync(
                                departmentId:
                                    department.DepartmentId,
                                isActive:
                                    false,
                                expectedRowVersion:
                                    department.RowVersion,
                                actorUserId:
                                    _fixture
                                        .SuperAdministratorUserId,
                                requestContext:
                                    CreateRequestContext(
                                        "/integration-tests/" +
                                        "departments/status-active-employees"),
                                cancellationToken:
                                    CancellationToken.None));

            Assert.Equal(
                DepartmentErrorCode
                    .DepartmentHasActiveEmployees,
                exception.ErrorCode);
        }
        finally
        {
            await _fixture.RemoveDepartmentTestDataAsync(
                departmentCode,
                identificationNumber);
        }
    }

    private Task<DepartmentData> CreateDepartmentAsync(
        string departmentCode,
        string departmentName)
    {
        return _fixture.DepartmentRepository
            .CreateDepartmentAsync(
                departmentCode:
                    departmentCode,
                name:
                    departmentName,
                description:
                    "Created by integration tests.",
                actorUserId:
                    _fixture.SuperAdministratorUserId,
                requestContext:
                    CreateRequestContext(
                        "/integration-tests/" +
                        "departments/create-helper"),
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

    private static string CreateDepartmentCode()
    {
        return "IT" + Guid.NewGuid()
            .ToString("N")[..12]
            .ToUpperInvariant();
    }

    private static string CreateDepartmentName()
    {
        return "Integration Department " +
            Guid.NewGuid().ToString("N")[..12];
    }
}
