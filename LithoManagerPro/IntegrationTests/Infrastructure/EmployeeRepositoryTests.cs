using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Departments;
using LithoManager.Application.Features
    .HumanResources.Employees;
using LithoManager.IntegrationTests.Collections;
using LithoManager.IntegrationTests.Fixtures;
using Xunit;

namespace LithoManager.IntegrationTests.Infrastructure
    .Persistence;

[Collection(
    AuthenticationDatabaseCollection.Name)]
public sealed class EmployeeRepositoryTests
{
    private readonly AuthenticationDatabaseFixture
        _fixture;

    public EmployeeRepositoryTests(
        AuthenticationDatabaseFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);

        _fixture = fixture;
    }

    [Fact]
    public async Task CreateEmployeeAsync_WhenUserIdIsNull_PersistsEmployeeAndAudit()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

        AuthenticationRequestContext requestContext =
            CreateRequestContext(
                "/integration-tests/employees/create");

        await _fixture.RemoveDepartmentTestDataAsync(
            departmentCode,
            identificationNumber);

        try
        {
            DepartmentData department =
                await CreateDepartmentAsync(
                    departmentCode,
                    CreateDepartmentName());

            // Act
            EmployeeData employee =
                await CreateEmployeeAsync(
                    department.DepartmentId,
                    identificationNumber,
                    requestContext);

            // Assert
            Assert.True(employee.EmployeeId > 0);
            Assert.Null(employee.UserId);
            Assert.Null(employee.EmailAddress);
            Assert.Equal(
                department.DepartmentId,
                employee.DepartmentId);
            Assert.Equal(
                identificationNumber,
                employee.IdentificationNumber);
            Assert.True(employee.IsActive);
            Assert.Equal(
                _fixture.SuperAdministratorUserId,
                employee.CreatedByUserId);
            Assert.Equal(8, employee.RowVersion.Length);

            EmployeeData? persisted =
                await _fixture.EmployeeRepository
                    .GetEmployeeByIdAsync(
                        employee.EmployeeId,
                        CancellationToken.None);

            Assert.NotNull(persisted);
            Assert.Equal(
                identificationNumber,
                persisted.IdentificationNumber);
            Assert.Null(persisted.UserId);

            AuditLogTestData? audit =
                await _fixture
                    .GetAuditLogByCorrelationIdAsync(
                        requestContext.CorrelationId);

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
            await _fixture.RemoveDepartmentTestDataAsync(
                departmentCode,
                identificationNumber);
        }
    }

    [Fact]
    public async Task GetEmployeesAsync_WhenFilterMatches_ReturnsCreatedEmployee()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

        await _fixture.RemoveDepartmentTestDataAsync(
            departmentCode,
            identificationNumber);

        try
        {
            DepartmentData department =
                await CreateDepartmentAsync(
                    departmentCode,
                    CreateDepartmentName());

            await CreateEmployeeAsync(
                department.DepartmentId,
                identificationNumber,
                CreateRequestContext(
                    "/integration-tests/" +
                    "employees/list-create"));

            // Act
            IReadOnlyList<EmployeeData> employees =
                await _fixture.EmployeeRepository
                    .GetEmployeesAsync(
                        searchTerm:
                            identificationNumber,
                        departmentId:
                            department.DepartmentId,
                        isActive:
                            true,
                        cancellationToken:
                            CancellationToken.None);

            // Assert
            EmployeeData employee =
                Assert.Single(
                    employees,
                    item =>
                        item.IdentificationNumber
                            == identificationNumber);

            Assert.Equal("Ana", employee.FirstName);
            Assert.Equal(
                department.DepartmentId,
                employee.DepartmentId);
            Assert.True(employee.IsActive);
        }
        finally
        {
            await _fixture.RemoveDepartmentTestDataAsync(
                departmentCode,
                identificationNumber);
        }
    }

    [Fact]
    public async Task UpdateEmployeeAsync_WhenRowVersionIsStale_ThrowsConcurrencyConflict()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

        await _fixture.RemoveDepartmentTestDataAsync(
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
                    identificationNumber,
                    CreateRequestContext(
                        "/integration-tests/" +
                        "employees/stale-create"));

            await _fixture.EmployeeRepository
                .UpdateEmployeeAsync(
                    employeeId:
                        employee.EmployeeId,
                    userId:
                        null,
                    departmentId:
                        department.DepartmentId,
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
                        _fixture.SuperAdministratorUserId,
                    requestContext:
                        CreateRequestContext(
                            "/integration-tests/" +
                            "employees/stale-first-update"),
                    cancellationToken:
                        CancellationToken.None);

            // Act and assert
            EmployeePersistenceException exception =
                await Assert.ThrowsAsync<
                    EmployeePersistenceException>(
                        () => _fixture.EmployeeRepository
                            .UpdateEmployeeAsync(
                                employeeId:
                                    employee.EmployeeId,
                                userId:
                                    null,
                                departmentId:
                                    department.DepartmentId,
                                identificationNumber:
                                    employee.IdentificationNumber,
                                firstName:
                                    employee.FirstName,
                                lastName:
                                    employee.LastName +
                                    " Stale",
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
                                    _fixture
                                        .SuperAdministratorUserId,
                                requestContext:
                                    CreateRequestContext(
                                        "/integration-tests/" +
                                        "employees/stale-second-update"),
                                cancellationToken:
                                    CancellationToken.None));

            Assert.Equal(
                EmployeeErrorCode.ConcurrencyConflict,
                exception.ErrorCode);
        }
        finally
        {
            await _fixture.RemoveDepartmentTestDataAsync(
                departmentCode,
                identificationNumber);
        }
    }

    [Fact]
    public async Task CreateEmployeeAsync_WhenIdentificationIsDuplicated_ThrowsDuplicateIdentificationNumber()
    {
        // Arrange
        string departmentCode =
            CreateDepartmentCode();

        string identificationNumber =
            CreateIdentificationNumber();

        await _fixture.RemoveDepartmentTestDataAsync(
            departmentCode,
            identificationNumber);

        try
        {
            DepartmentData department =
                await CreateDepartmentAsync(
                    departmentCode,
                    CreateDepartmentName());

            await CreateEmployeeAsync(
                department.DepartmentId,
                identificationNumber,
                CreateRequestContext(
                    "/integration-tests/" +
                    "employees/duplicate-first"));

            // Act and assert
            EmployeePersistenceException exception =
                await Assert.ThrowsAsync<
                    EmployeePersistenceException>(
                        () => CreateEmployeeAsync(
                            department.DepartmentId,
                            identificationNumber,
                            CreateRequestContext(
                                "/integration-tests/" +
                                "employees/duplicate-second")));

            Assert.Equal(
                EmployeeErrorCode
                    .DuplicateIdentificationNumber,
                exception.ErrorCode);
        }
        finally
        {
            await _fixture.RemoveDepartmentTestDataAsync(
                departmentCode,
                identificationNumber);
        }
    }

    private Task<EmployeeData> CreateEmployeeAsync(
        int departmentId,
        string identificationNumber,
        AuthenticationRequestContext requestContext)
    {
        return _fixture.EmployeeRepository
            .CreateEmployeeAsync(
                userId:
                    null,
                departmentId:
                    departmentId,
                identificationNumber:
                    identificationNumber,
                firstName:
                    "Ana",
                lastName:
                    "Rivera",
                phoneNumber:
                    "5555-0101",
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
                    _fixture.SuperAdministratorUserId,
                requestContext:
                    requestContext,
                cancellationToken:
                    CancellationToken.None);
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
                    "Created by employee integration tests.",
                actorUserId:
                    _fixture.SuperAdministratorUserId,
                requestContext:
                    CreateRequestContext(
                        "/integration-tests/" +
                        "employees/department-helper"),
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
        return "IE" + Guid.NewGuid()
            .ToString("N")[..12]
            .ToUpperInvariant();
    }

    private static string CreateDepartmentName()
    {
        return "Employee Integration Department " +
            Guid.NewGuid().ToString("N")[..12];
    }

    private static string CreateIdentificationNumber()
    {
        return "EMP-" + Guid.NewGuid()
            .ToString("N")[..12]
            .ToUpperInvariant();
    }
}
