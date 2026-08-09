using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Departments;

namespace LithoManager.UnitTests.TestDoubles.Persistence;

public sealed class FakeDepartmentRepository
    : IDepartmentRepository
{
    public DepartmentData DepartmentToReturn
    {
        get;
        set;
    } = CreateDefaultDepartment();

    public IReadOnlyList<DepartmentData>
        DepartmentsToReturn
    {
        get;
        set;
    } = [CreateDefaultDepartment()];

    public DepartmentData? DepartmentByIdToReturn
    {
        get;
        set;
    } = CreateDefaultDepartment();

    public DepartmentPersistenceException?
        ExceptionToThrow
    {
        get;
        set;
    }

    public int CreateDepartmentCallCount
    {
        get;
        private set;
    }

    public int GetDepartmentByIdCallCount
    {
        get;
        private set;
    }

    public int GetDepartmentsCallCount
    {
        get;
        private set;
    }

    public int UpdateDepartmentCallCount
    {
        get;
        private set;
    }

    public int SetDepartmentStatusCallCount
    {
        get;
        private set;
    }

    public string? LastDepartmentCode
    {
        get;
        private set;
    }

    public string? LastName
    {
        get;
        private set;
    }

    public string? LastDescription
    {
        get;
        private set;
    }

    public int? LastDepartmentId
    {
        get;
        private set;
    }

    public int? LastActorUserId
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

    public AuthenticationRequestContext?
        LastRequestContext
    {
        get;
        private set;
    }

    public Task<DepartmentData> CreateDepartmentAsync(
        string departmentCode,
        string name,
        string? description,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        CreateDepartmentCallCount++;
        LastDepartmentCode = departmentCode;
        LastName = name;
        LastDescription = description;
        LastActorUserId = actorUserId;
        LastRequestContext = requestContext;

        ThrowIfConfigured();

        return Task.FromResult(
            DepartmentToReturn);
    }

    public Task<DepartmentData?> GetDepartmentByIdAsync(
        int departmentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetDepartmentByIdCallCount++;
        LastDepartmentId = departmentId;

        return Task.FromResult(
            DepartmentByIdToReturn);
    }

    public Task<IReadOnlyList<DepartmentData>>
        GetDepartmentsAsync(
            string? searchTerm,
            bool? isActive,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GetDepartmentsCallCount++;
        LastSearchTerm = searchTerm;
        LastIsActiveFilter = isActive;

        return Task.FromResult(
            DepartmentsToReturn);
    }

    public Task<DepartmentData> UpdateDepartmentAsync(
        int departmentId,
        string departmentCode,
        string name,
        string? description,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        UpdateDepartmentCallCount++;
        LastDepartmentId = departmentId;
        LastDepartmentCode = departmentCode;
        LastName = name;
        LastDescription = description;
        LastExpectedRowVersion =
            (byte[])expectedRowVersion.Clone();
        LastActorUserId = actorUserId;
        LastRequestContext = requestContext;

        ThrowIfConfigured();

        return Task.FromResult(
            DepartmentToReturn);
    }

    public Task<DepartmentData> SetDepartmentStatusAsync(
        int departmentId,
        bool isActive,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SetDepartmentStatusCallCount++;
        LastDepartmentId = departmentId;
        LastIsActive = isActive;
        LastExpectedRowVersion =
            (byte[])expectedRowVersion.Clone();
        LastActorUserId = actorUserId;
        LastRequestContext = requestContext;

        ThrowIfConfigured();

        return Task.FromResult(
            DepartmentToReturn);
    }

    private void ThrowIfConfigured()
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }
    }

    private static DepartmentData CreateDefaultDepartment()
    {
        return new DepartmentData
        {
            DepartmentId = 10,
            DepartmentCode = "HR",
            Name = "Human Resources",
            Description =
                "Human resources department.",
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
