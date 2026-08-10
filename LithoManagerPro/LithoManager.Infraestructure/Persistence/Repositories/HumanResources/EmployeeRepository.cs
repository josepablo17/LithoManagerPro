using System.Data;
using Dapper;
using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Employees;
using LithoManager.Infrastructure.Persistence.Dapper;
using Microsoft.Data.SqlClient;

namespace LithoManager.Infrastructure.Persistence
    .Repositories.HumanResources;

public sealed class EmployeeRepository
    : IEmployeeRepository
{
    private const string CreateEmployeeProcedure =
        "HumanResources.CreateEmployee";

    private const string GetEmployeeByIdProcedure =
        "HumanResources.GetEmployeeById";

    private const string GetEmployeesProcedure =
        "HumanResources.GetEmployees";

    private const string UpdateEmployeeProcedure =
        "HumanResources.UpdateEmployee";

    private const string SetEmployeeStatusProcedure =
        "HumanResources.SetEmployeeStatus";

    private readonly ISqlConnectionFactory _connectionFactory;

    public EmployeeRepository(
        ISqlConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(
            connectionFactory);

        _connectionFactory = connectionFactory;
    }

    public async Task<EmployeeData> CreateEmployeeAsync(
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
        ValidateEmployeeMutation(
            userId,
            departmentId,
            identificationNumber,
            firstName,
            lastName,
            jobTitle,
            baseSalary,
            actorUserId,
            requestContext);

        DynamicParameters parameters =
            CreateEmployeeParameters(
                userId,
                departmentId,
                identificationNumber,
                firstName,
                lastName,
                phoneNumber,
                birthDate,
                hireDate,
                terminationDate,
                jobTitle,
                baseSalary,
                profileImagePath,
                actorUserId,
                requestContext);

        CommandDefinition command = new(
            commandText:
                CreateEmployeeProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        EmployeeData result =
            await QuerySingleEmployeeAsync(
                connection,
                command);

        return NormalizeDates(result);
    }

    public async Task<EmployeeData?> GetEmployeeByIdAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        ValidateEmployeeId(employeeId);

        var parameters = new
        {
            EmployeeId = employeeId
        };

        CommandDefinition command = new(
            commandText:
                GetEmployeeByIdProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        EmployeeData? result =
            await connection.QuerySingleOrDefaultAsync<
                EmployeeData>(command);

        return result is null
            ? null
            : NormalizeDates(result);
    }

    public async Task<IReadOnlyList<EmployeeData>>
        GetEmployeesAsync(
            string? searchTerm,
            int? departmentId,
            bool? isActive,
            CancellationToken cancellationToken)
    {
        if (departmentId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(departmentId),
                "DepartmentId must be greater than zero.");
        }

        var parameters = new
        {
            SearchTerm =
                string.IsNullOrWhiteSpace(searchTerm)
                    ? null
                    : searchTerm.Trim(),

            DepartmentId = departmentId,

            IsActive = isActive
        };

        CommandDefinition command = new(
            commandText:
                GetEmployeesProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        IEnumerable<EmployeeData> employees =
            await connection.QueryAsync<EmployeeData>(
                command);

        return employees
            .Select(NormalizeDates)
            .ToList();
    }

    public async Task<EmployeeData> UpdateEmployeeAsync(
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
        ValidateEmployeeId(employeeId);

        ValidateEmployeeMutation(
            userId,
            departmentId,
            identificationNumber,
            firstName,
            lastName,
            jobTitle,
            baseSalary,
            actorUserId,
            requestContext);

        ValidateRowVersion(expectedRowVersion);

        DynamicParameters parameters =
            CreateEmployeeParameters(
                userId,
                departmentId,
                identificationNumber,
                firstName,
                lastName,
                phoneNumber,
                birthDate,
                hireDate,
                terminationDate,
                jobTitle,
                baseSalary,
                profileImagePath,
                actorUserId,
                requestContext);

        parameters.Add(
            "EmployeeId",
            employeeId,
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
                UpdateEmployeeProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        EmployeeData result =
            await QuerySingleEmployeeAsync(
                connection,
                command);

        return NormalizeDates(result);
    }

    public async Task<EmployeeData> SetEmployeeStatusAsync(
        int employeeId,
        bool isActive,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ValidateEmployeeId(employeeId);
        ValidateRowVersion(expectedRowVersion);
        ValidateActorUserId(actorUserId);
        ValidateRequestContext(requestContext);

        DynamicParameters parameters = new();

        parameters.Add(
            "EmployeeId",
            employeeId,
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
                SetEmployeeStatusProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        EmployeeData result =
            await QuerySingleEmployeeAsync(
                connection,
                command);

        return NormalizeDates(result);
    }

    private static DynamicParameters CreateEmployeeParameters(
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
        AuthenticationRequestContext requestContext)
    {
        DynamicParameters parameters = new();

        parameters.Add(
            "UserId",
            userId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "DepartmentId",
            departmentId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "IdentificationNumber",
            identificationNumber.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "FirstName",
            firstName.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "LastName",
            lastName.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "PhoneNumber",
            string.IsNullOrWhiteSpace(phoneNumber)
                ? null
                : phoneNumber.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "BirthDate",
            birthDate?.Date,
            DbType.Date,
            ParameterDirection.Input);

        parameters.Add(
            "HireDate",
            hireDate.Date,
            DbType.Date,
            ParameterDirection.Input);

        parameters.Add(
            "TerminationDate",
            terminationDate?.Date,
            DbType.Date,
            ParameterDirection.Input);

        parameters.Add(
            "JobTitle",
            jobTitle.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "BaseSalary",
            baseSalary,
            DbType.Decimal,
            ParameterDirection.Input);

        parameters.Add(
            "ProfileImagePath",
            string.IsNullOrWhiteSpace(profileImagePath)
                ? null
                : profileImagePath.Trim(),
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

    private static void ValidateEmployeeMutation(
        int? userId,
        int departmentId,
        string identificationNumber,
        string firstName,
        string lastName,
        string jobTitle,
        decimal baseSalary,
        int actorUserId,
        AuthenticationRequestContext requestContext)
    {
        if (userId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "UserId must be greater than zero.");
        }

        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(departmentId),
                "DepartmentId must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            identificationNumber);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            firstName);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            lastName);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            jobTitle);

        if (baseSalary < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseSalary),
                "BaseSalary must be greater than or equal to zero.");
        }

        ValidateActorUserId(actorUserId);
        ValidateRequestContext(requestContext);
    }

    private static void ValidateEmployeeId(
        int employeeId)
    {
        if (employeeId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(employeeId),
                "EmployeeId must be greater than zero.");
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

    private static EmployeeData NormalizeDates(
        EmployeeData employee)
    {
        return new EmployeeData
        {
            EmployeeId =
                employee.EmployeeId,

            UserId =
                employee.UserId,

            EmailAddress =
                employee.EmailAddress,

            DepartmentId =
                employee.DepartmentId,

            DepartmentCode =
                employee.DepartmentCode,

            DepartmentName =
                employee.DepartmentName,

            IsDepartmentActive =
                employee.IsDepartmentActive,

            IdentificationNumber =
                employee.IdentificationNumber,

            FirstName =
                employee.FirstName,

            LastName =
                employee.LastName,

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

            IsActive =
                employee.IsActive,

            CreatedAtUtc =
                DateTime.SpecifyKind(
                    employee.CreatedAtUtc,
                    DateTimeKind.Utc),

            CreatedByUserId =
                employee.CreatedByUserId,

            UpdatedAtUtc =
                employee.UpdatedAtUtc is DateTime
                    updatedAtUtc
                        ? DateTime.SpecifyKind(
                            updatedAtUtc,
                            DateTimeKind.Utc)
                        : null,

            UpdatedByUserId =
                employee.UpdatedByUserId,

            RowVersion =
                employee.RowVersion
        };
    }

    private static async Task<EmployeeData>
        QuerySingleEmployeeAsync(
            System.Data.Common.DbConnection connection,
            CommandDefinition command)
    {
        try
        {
            return await connection.QuerySingleAsync<
                EmployeeData>(command);
        }
        catch (SqlException exception)
            when (TryMapSqlException(
                exception,
                out EmployeeErrorCode errorCode))
        {
            throw new EmployeePersistenceException(
                errorCode,
                exception.Message,
                exception);
        }
    }

    private static bool TryMapSqlException(
        SqlException exception,
        out EmployeeErrorCode errorCode)
    {
        errorCode = exception.Number switch
        {
            52101 or 52102 or 52103 or 52104
                or 52105 or 52106 or 52107
                or 52108 or 52109 or 52110
                or 52111 or 52112 or 52113
                or 52114 or 52115 or 52116
                or 52131 or 52132
                or 52141 or 52142 or 52143
                or 52144 or 52145 or 52146
                or 52147 or 52148 or 52149
                or 52150 or 52151 or 52152
                or 52153 or 52154 or 52155
                or 52156 or 52157 or 52158
                or 52181 or 52182 or 52183
                or 52184 =>
                    EmployeeErrorCode.InvalidRequest,

            52117 or 52118 or 52119 or 52120
                or 52121 or 52122
                or 52159 or 52160 or 52161
                or 52162 or 52163 or 52164
                or 52185 or 52186 or 52187
                or 52188 or 52189 or 52190 =>
                    EmployeeErrorCode.AccessNotAvailable,

            52123 or 52167 =>
                EmployeeErrorCode.DepartmentNotFound,

            52124 or 52168 or 52193 =>
                EmployeeErrorCode.DepartmentInactive,

            52125 or 52169 =>
                EmployeeErrorCode.UserNotFound,

            52126 or 52170 =>
                EmployeeErrorCode.UserAlreadyAssigned,

            52127 or 52171 =>
                EmployeeErrorCode
                    .DuplicateIdentificationNumber,

            52165 or 52191 =>
                EmployeeErrorCode.EmployeeNotFound,

            52166 or 52172 or 52192 or 52194 =>
                EmployeeErrorCode.ConcurrencyConflict,

            _ =>
                EmployeeErrorCode.None
        };

        return errorCode != EmployeeErrorCode.None;
    }
}
