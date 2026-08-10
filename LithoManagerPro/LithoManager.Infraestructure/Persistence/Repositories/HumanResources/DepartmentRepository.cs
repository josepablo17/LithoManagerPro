using System.Data;
using Dapper;
using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Departments;
using LithoManager.Infrastructure.Persistence.Dapper;
using Microsoft.Data.SqlClient;

namespace LithoManager.Infrastructure.Persistence
    .Repositories.HumanResources;

public sealed class DepartmentRepository
    : IDepartmentRepository
{
    private const string CreateDepartmentProcedure =
        "HumanResources.CreateDepartment";

    private const string GetDepartmentByIdProcedure =
        "HumanResources.GetDepartmentById";

    private const string GetDepartmentsProcedure =
        "HumanResources.GetDepartments";

    private const string UpdateDepartmentProcedure =
        "HumanResources.UpdateDepartment";

    private const string SetDepartmentStatusProcedure =
        "HumanResources.SetDepartmentStatus";

    private readonly ISqlConnectionFactory _connectionFactory;

    public DepartmentRepository(
        ISqlConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(
            connectionFactory);

        _connectionFactory = connectionFactory;
    }

    public async Task<DepartmentData> CreateDepartmentAsync(
        string departmentCode,
        string name,
        string? description,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            departmentCode);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            name);

        ValidateActorUserId(actorUserId);
        ValidateRequestContext(requestContext);

        DynamicParameters parameters =
            CreateDepartmentParameters(
                departmentCode,
                name,
                description,
                actorUserId,
                requestContext);

        CommandDefinition command = new(
            commandText:
                CreateDepartmentProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        DepartmentData result =
            await QuerySingleDepartmentAsync(
                connection,
                command);

        return NormalizeDates(result);
    }

    public async Task<DepartmentData?> GetDepartmentByIdAsync(
        int departmentId,
        CancellationToken cancellationToken)
    {
        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(departmentId),
                "DepartmentId must be greater than zero.");
        }

        var parameters = new
        {
            DepartmentId = departmentId
        };

        CommandDefinition command = new(
            commandText:
                GetDepartmentByIdProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        DepartmentData? result =
            await connection.QuerySingleOrDefaultAsync<
                DepartmentData>(command);

        return result is null
            ? null
            : NormalizeDates(result);
    }

    public async Task<IReadOnlyList<DepartmentData>>
        GetDepartmentsAsync(
            string? searchTerm,
            bool? isActive,
            CancellationToken cancellationToken)
    {
        var parameters = new
        {
            SearchTerm =
                string.IsNullOrWhiteSpace(searchTerm)
                    ? null
                    : searchTerm.Trim(),

            IsActive = isActive
        };

        CommandDefinition command = new(
            commandText:
                GetDepartmentsProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        IEnumerable<DepartmentData> departments =
            await connection.QueryAsync<DepartmentData>(
                command);

        return departments
            .Select(NormalizeDates)
            .ToList();
    }

    public async Task<DepartmentData> UpdateDepartmentAsync(
        int departmentId,
        string departmentCode,
        string name,
        string? description,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ValidateDepartmentId(departmentId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            departmentCode);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            name);

        ValidateRowVersion(expectedRowVersion);
        ValidateActorUserId(actorUserId);
        ValidateRequestContext(requestContext);

        DynamicParameters parameters =
            CreateDepartmentParameters(
                departmentCode,
                name,
                description,
                actorUserId,
                requestContext);

        parameters.Add(
            "DepartmentId",
            departmentId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "ExpectedRowVersion",
            expectedRowVersion,
            DbType.Binary,
            ParameterDirection.Input,
            size: 8);

        CommandDefinition command = new(
            commandText:
                UpdateDepartmentProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        DepartmentData result =
            await QuerySingleDepartmentAsync(
                connection,
                command);

        return NormalizeDates(result);
    }

    public async Task<DepartmentData> SetDepartmentStatusAsync(
        int departmentId,
        bool isActive,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ValidateDepartmentId(departmentId);
        ValidateRowVersion(expectedRowVersion);
        ValidateActorUserId(actorUserId);
        ValidateRequestContext(requestContext);

        DynamicParameters parameters = new();

        parameters.Add(
            "DepartmentId",
            departmentId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "IsActive",
            isActive,
            DbType.Boolean,
            ParameterDirection.Input);

        parameters.Add(
            "ExpectedRowVersion",
            expectedRowVersion,
            DbType.Binary,
            ParameterDirection.Input,
            size: 8);

        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = new(
            commandText:
                SetDepartmentStatusProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        DepartmentData result =
            await QuerySingleDepartmentAsync(
                connection,
                command);

        return NormalizeDates(result);
    }

    private static DynamicParameters CreateDepartmentParameters(
        string departmentCode,
        string name,
        string? description,
        int actorUserId,
        AuthenticationRequestContext requestContext)
    {
        DynamicParameters parameters = new();

        parameters.Add(
            "DepartmentCode",
            departmentCode.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "Name",
            name.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "Description",
            string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        return parameters;
    }

    private static void AddActorAndRequestContextParameters(
        DynamicParameters parameters,
        int actorUserId,
        AuthenticationRequestContext requestContext)
    {
        parameters.Add(
            "ActorUserId",
            actorUserId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "CorrelationId",
            requestContext.CorrelationId,
            DbType.Guid,
            ParameterDirection.Input);

        parameters.Add(
            "ClientIpAddress",
            requestContext.ClientIpAddress,
            DbType.String,
            ParameterDirection.Input,
            size: 45);

        parameters.Add(
            "UserAgent",
            requestContext.UserAgent,
            DbType.String,
            ParameterDirection.Input,
            size: 512);

        parameters.Add(
            "RequestPath",
            requestContext.RequestPath,
            DbType.String,
            ParameterDirection.Input,
            size: 500);
    }

    private static void ValidateDepartmentId(
        int departmentId)
    {
        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(departmentId),
                "DepartmentId must be greater than zero.");
        }
    }

    private static void ValidateActorUserId(
        int actorUserId)
    {
        if (actorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actorUserId),
                "ActorUserId must be greater than zero.");
        }
    }

    private static void ValidateRequestContext(
        AuthenticationRequestContext requestContext)
    {
        ArgumentNullException.ThrowIfNull(
            requestContext);

        if (requestContext.CorrelationId == Guid.Empty)
        {
            throw new ArgumentException(
                "CorrelationId is required.",
                nameof(requestContext));
        }
    }

    private static void ValidateRowVersion(
        byte[] expectedRowVersion)
    {
        ArgumentNullException.ThrowIfNull(
            expectedRowVersion);

        if (expectedRowVersion.Length != 8)
        {
            throw new ArgumentException(
                "ExpectedRowVersion must contain exactly 8 bytes.",
                nameof(expectedRowVersion));
        }
    }

    private static DepartmentData NormalizeDates(
        DepartmentData department)
    {
        return new DepartmentData
        {
            DepartmentId =
                department.DepartmentId,

            DepartmentCode =
                department.DepartmentCode,

            Name =
                department.Name,

            Description =
                department.Description,

            IsActive =
                department.IsActive,

            CreatedAtUtc =
                DateTime.SpecifyKind(
                    department.CreatedAtUtc,
                    DateTimeKind.Utc),

            CreatedByUserId =
                department.CreatedByUserId,

            UpdatedAtUtc =
                department.UpdatedAtUtc is DateTime
                    updatedAtUtc
                        ? DateTime.SpecifyKind(
                            updatedAtUtc,
                            DateTimeKind.Utc)
                        : null,

            UpdatedByUserId =
                department.UpdatedByUserId,

            RowVersion =
                department.RowVersion
        };
    }

    private static async Task<DepartmentData>
        QuerySingleDepartmentAsync(
            System.Data.Common.DbConnection connection,
            CommandDefinition command)
    {
        try
        {
            return await connection.QuerySingleAsync<
                DepartmentData>(command);
        }
        catch (SqlException exception)
            when (TryMapSqlException(
                exception,
                out DepartmentErrorCode errorCode))
        {
            throw new DepartmentPersistenceException(
                errorCode,
                exception.Message,
                exception);
        }
    }

    private static bool TryMapSqlException(
        SqlException exception,
        out DepartmentErrorCode errorCode)
    {
        errorCode = exception.Number switch
        {
            52008 or 52009 or 52010
                or 52011 or 52012 or 52013
                or 52040 or 52041 or 52042
                or 52043 or 52044 or 52045
                or 52065 or 52066 or 52067
                or 52068 or 52069 or 52070 =>
                    DepartmentErrorCode.AccessNotAvailable,

            52014 or 52048 =>
                DepartmentErrorCode.DuplicateDepartmentCode,

            52015 or 52049 =>
                DepartmentErrorCode.DuplicateDepartmentName,

            52046 or 52071 =>
                DepartmentErrorCode.DepartmentNotFound,

            52047 or 52072 =>
                DepartmentErrorCode.ConcurrencyConflict,

            52074 =>
                DepartmentErrorCode.DepartmentHasActiveEmployees,

            _ =>
                DepartmentErrorCode.None
        };

        return errorCode != DepartmentErrorCode.None;
    }
}
