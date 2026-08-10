using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Employees;

namespace LithoManager.UnitTests.TestDoubles.Persistence;

public sealed class FakeEmployeeRepository
    : IEmployeeRepository
{
    public EmployeeData EmployeeToReturn
    {
        get;
        set;
    } = CreateDefaultEmployee();

    public IReadOnlyList<EmployeeData>
        EmployeesToReturn
    {
        get;
        set;
    } = [CreateDefaultEmployee()];

    public EmployeeData? EmployeeByIdToReturn
    {
        get;
        set;
    } = CreateDefaultEmployee();

    public EmployeePersistenceException?
        ExceptionToThrow
    {
        get;
        set;
    }

    public int CreateEmployeeCallCount
    {
        get;
        private set;
    }

    public int GetEmployeeByIdCallCount
    {
        get;
        private set;
    }

    public int GetEmployeesCallCount
    {
        get;
        private set;
    }

    public int UpdateEmployeeCallCount
    {
        get;
        private set;
    }

    public int SetEmployeeStatusCallCount
    {
        get;
        private set;
    }

    public int? LastEmployeeId
    {
        get;
        private set;
    }

    public int? LastUserId
    {
        get;
        private set;
    }

    public int? LastDepartmentId
    {
        get;
        private set;
    }

    public string? LastIdentificationNumber
    {
        get;
        private set;
    }

    public string? LastFirstName
    {
        get;
        private set;
    }

    public string? LastLastName
    {
        get;
        private set;
    }

    public bool? LastIsActive
    {
        get;
        private set;
    }

    public string? LastSearchTerm
    {
        get;
        private set;
    }

    public bool? LastIsActiveFilter
    {
        get;
        private set;
    }

    public byte[]? LastExpectedRowVersion
    {
        get;
        private set;
    }

    public int? LastActorUserId
    {
        get;
        private set;
    }

    public AuthenticationRequestContext?
        LastRequestContext
    {
        get;
        private set;
    }

    public Task<EmployeeData> CreateEmployeeAsync(
        int? userId,
        int departmentId,
        string identificationNumber,
        string firstName,
        string lastName,
        string? phoneNumber,
        DateTime? birthDate,
        DateTime hireDate,
        DateTime? terminationDate,
        string jobTitle,
        decimal baseSalary,
        string? profileImagePath,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CreateEmployeeCallCount++;
        LastUserId = userId;
        LastDepartmentId = departmentId;
        LastIdentificationNumber =
            identificationNumber;
        LastFirstName = firstName;
        LastLastName = lastName;
        LastActorUserId = actorUserId;
        LastRequestContext = requestContext;

        ThrowIfConfigured();

        return Task.FromResult(
            EmployeeToReturn);
    }

    public Task<EmployeeData?> GetEmployeeByIdAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetEmployeeByIdCallCount++;
        LastEmployeeId = employeeId;

        return Task.FromResult(
            EmployeeByIdToReturn);
    }

    public Task<IReadOnlyList<EmployeeData>>
        GetEmployeesAsync(
            string? searchTerm,
            int? departmentId,
            bool? isActive,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetEmployeesCallCount++;
        LastSearchTerm = searchTerm;
        LastDepartmentId = departmentId;
        LastIsActiveFilter = isActive;

        return Task.FromResult(
            EmployeesToReturn);
    }

    public Task<EmployeeData> UpdateEmployeeAsync(
        int employeeId,
        int? userId,
        int departmentId,
        string identificationNumber,
        string firstName,
        string lastName,
        string? phoneNumber,
        DateTime? birthDate,
        DateTime hireDate,
        DateTime? terminationDate,
        string jobTitle,
        decimal baseSalary,
        string? profileImagePath,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        UpdateEmployeeCallCount++;
        LastEmployeeId = employeeId;
        LastUserId = userId;
        LastDepartmentId = departmentId;
        LastIdentificationNumber =
            identificationNumber;
        LastFirstName = firstName;
        LastLastName = lastName;
        LastExpectedRowVersion =
            (byte[])expectedRowVersion.Clone();
        LastActorUserId = actorUserId;
        LastRequestContext = requestContext;

        ThrowIfConfigured();

        return Task.FromResult(
            EmployeeToReturn);
    }

    public Task<EmployeeData> SetEmployeeStatusAsync(
        int employeeId,
        bool isActive,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SetEmployeeStatusCallCount++;
        LastEmployeeId = employeeId;
        LastIsActive = isActive;
        LastExpectedRowVersion =
            (byte[])expectedRowVersion.Clone();
        LastActorUserId = actorUserId;
        LastRequestContext = requestContext;

        ThrowIfConfigured();

        return Task.FromResult(
            EmployeeToReturn);
    }

    private void ThrowIfConfigured()
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }
    }

    private static EmployeeData CreateDefaultEmployee()
    {
        return new EmployeeData
        {
            EmployeeId = 20,
            UserId = null,
            EmailAddress = null,
            DepartmentId = 10,
            DepartmentCode = "HR",
            DepartmentName = "Human Resources",
            IsDepartmentActive = true,
            IdentificationNumber = "EMP-001",
            FirstName = "Ana",
            LastName = "Rivera",
            PhoneNumber = "5555-0101",
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
            TerminationDate = null,
            JobTitle = "HR Specialist",
            BaseSalary = 1200.00m,
            ProfileImagePath = null,
            IsActive = true,
            CreatedAtUtc =
                new DateTime(
                    2026,
                    8,
                    9,
                    18,
                    0,
                    0,
                    DateTimeKind.Utc),
            CreatedByUserId = 1,
            UpdatedAtUtc = null,
            UpdatedByUserId = null,
            RowVersion =
            [
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8
            ]
        };
    }
}
