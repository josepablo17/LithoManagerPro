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

    private const string GetAssignableEmployeeUsersProcedure =
        "HumanResources.GetAssignableEmployeeUsers";

    private const string GetEmployeeIdentificationTypesProcedure =
        "HumanResources.GetEmployeeIdentificationTypes";

    private const string GetEmployeeByIdProcedure =
        "HumanResources.GetEmployeeById";

    private const string GetEmployeesProcedure =
        "HumanResources.GetEmployees";

    private const string GetEmployeeSalaryHistoryProcedure =
        "HumanResources.GetEmployeeSalaryHistory";

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

    public async Task<IReadOnlyList<AssignableEmployeeUserData>>
        GetAssignableEmployeeUsersAsync(
            int? employeeId,
            CancellationToken cancellationToken)
    {
        if (employeeId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(employeeId),
                "EmployeeId must be greater than zero.");
        }

        DynamicParameters parameters = new();
        parameters.Add(
            "EmployeeId",
            employeeId,
            DbType.Int32,
            ParameterDirection.Input);

        CommandDefinition command = new(
            commandText:
                GetAssignableEmployeeUsersProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        try
        {
            IEnumerable<AssignableEmployeeUserData> result =
                await connection.QueryAsync<
                    AssignableEmployeeUserData>(command);

            return result.ToList();
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

    public async Task<IReadOnlyList<EmployeeIdentificationTypeData>>
        GetEmployeeIdentificationTypesAsync(
            CancellationToken cancellationToken)
    {
        CommandDefinition command = new(
            commandText:
                GetEmployeeIdentificationTypesProcedure,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        IEnumerable<EmployeeIdentificationTypeData> result =
            await connection.QueryAsync<
                EmployeeIdentificationTypeData>(command);

        return result.ToList();
    }

    public async Task<EmployeeData> CreateEmployeeAsync(
        int? userId,
        int departmentId,
        string identificationType,
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
            identificationType,
            identificationNumber,
            firstName,
            lastName,
            phoneNumber,
            jobTitle,
            baseSalary,
            actorUserId,
            requestContext);

        DynamicParameters parameters =
            CreateEmployeeParameters(
                userId,
                departmentId,
                identificationType,
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

    public async Task<IReadOnlyList<EmployeeSalaryHistoryData>>
        GetEmployeeSalaryHistoryAsync(
            int actorUserId,
            int employeeId,
            DateTime? effectiveFromDate,
            DateTime? effectiveToDate,
            CancellationToken cancellationToken)
    {
        ValidateActorUserId(actorUserId);
        ValidateEmployeeId(employeeId);
        ValidateEffectiveDateRange(
            effectiveFromDate,
            effectiveToDate);

        var parameters = new
        {
            ActorUserId = actorUserId,
            EmployeeId = employeeId,
            EffectiveFromDate = effectiveFromDate?.Date,
            EffectiveToDate = effectiveToDate?.Date
        };

        CommandDefinition command = new(
            commandText:
                GetEmployeeSalaryHistoryProcedure,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        IReadOnlyList<EmployeeSalaryHistoryData> salaryHistory =
            await QueryEmployeeSalaryHistoryAsync(
                connection,
                command);

        return salaryHistory
            .Select(NormalizeDates)
            .ToList();
    }

    public async Task<EmployeeData> UpdateEmployeeAsync(
        int employeeId,
        int? userId,
        int departmentId,
        string identificationType,
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
            identificationType,
            identificationNumber,
            firstName,
            lastName,
            phoneNumber,
            jobTitle,
            baseSalary,
            actorUserId,
            requestContext);

        ValidateRowVersion(expectedRowVersion);

        DynamicParameters parameters =
            CreateEmployeeParameters(
                userId,
                departmentId,
                identificationType,
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
        string identificationType,
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
            "IdentificationType",
            identificationType.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

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
            size: 8);

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
        string identificationType,
        string identificationNumber,
        string firstName,
        string lastName,
        string? phoneNumber,
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
            identificationType);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            identificationNumber);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            firstName);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            lastName);

        if (!IsValidPhoneNumber(phoneNumber))
        {
            throw new ArgumentException(
                "PhoneNumber must contain exactly 8 digits.",
                nameof(phoneNumber));
        }

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

    private static bool IsValidPhoneNumber(
        string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return true;
        }

        string normalizedPhoneNumber =
            phoneNumber.Trim();

        return normalizedPhoneNumber.Length == 8
            && normalizedPhoneNumber.All(char.IsDigit);
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

    private static void ValidateEffectiveDateRange(
        DateTime? effectiveFromDate,
        DateTime? effectiveToDate)
    {
        if (effectiveFromDate.HasValue
            && effectiveToDate.HasValue
            && effectiveToDate.Value.Date
                < effectiveFromDate.Value.Date)
        {
            throw new ArgumentException(
                "EffectiveToDate cannot be earlier than EffectiveFromDate.",
                nameof(effectiveToDate));
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

            IdentificationType =
                employee.IdentificationType,

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

    private static EmployeeSalaryHistoryData NormalizeDates(
        EmployeeSalaryHistoryData salaryHistory)
    {
        return new EmployeeSalaryHistoryData
        {
            EmployeeSalaryHistoryId =
                salaryHistory.EmployeeSalaryHistoryId,

            EmployeeId =
                salaryHistory.EmployeeId,

            IdentificationType =
                salaryHistory.IdentificationType,

            IdentificationNumber =
                salaryHistory.IdentificationNumber,

            FirstName =
                salaryHistory.FirstName,

            LastName =
                salaryHistory.LastName,

            DepartmentId =
                salaryHistory.DepartmentId,

            DepartmentCode =
                salaryHistory.DepartmentCode,

            DepartmentName =
                salaryHistory.DepartmentName,

            BaseSalary =
                salaryHistory.BaseSalary,

            EffectiveFromDate =
                salaryHistory.EffectiveFromDate.Date,

            EffectiveToDate =
                salaryHistory.EffectiveToDate?.Date,

            IsCurrent =
                salaryHistory.IsCurrent,

            CreatedAtUtc =
                DateTime.SpecifyKind(
                    salaryHistory.CreatedAtUtc,
                    DateTimeKind.Utc),

            CreatedByUserId =
                salaryHistory.CreatedByUserId,

            UpdatedAtUtc =
                salaryHistory.UpdatedAtUtc is DateTime
                    updatedAtUtc
                        ? DateTime.SpecifyKind(
                            updatedAtUtc,
                            DateTimeKind.Utc)
                        : null,

            UpdatedByUserId =
                salaryHistory.UpdatedByUserId,

            RowVersion =
                salaryHistory.RowVersion
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

    private static async Task<IReadOnlyList<
        EmployeeSalaryHistoryData>>
        QueryEmployeeSalaryHistoryAsync(
            System.Data.Common.DbConnection connection,
            CommandDefinition command)
    {
        try
        {
            IEnumerable<EmployeeSalaryHistoryData> result =
                await connection.QueryAsync<
                    EmployeeSalaryHistoryData>(command);

            return result.ToList();
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
                or 52129 or 52130
                or 52131 or 52132 or 52133
                or 52141 or 52142 or 52143
                or 52144 or 52145 or 52146
                or 52147 or 52148 or 52149
                or 52150 or 52151 or 52152
                or 52153 or 52154 or 52155
                or 52156 or 52157 or 52158
                or 52173 or 52174 or 52175
                or 52181 or 52182 or 52183
                or 52184
                or 52201 or 52202 or 52203 =>
                    EmployeeErrorCode.InvalidRequest,

            52117 or 52118 or 52119 or 52120
                or 52121 or 52122
                or 52159 or 52160 or 52161
                or 52162 or 52163 or 52164
                or 52185 or 52186 or 52187
                or 52188 or 52189 or 52190
                or 52204 or 52205 or 52206
                or 52207 or 52208 or 52209 =>
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

            52165 or 52191 or 52210 =>
                EmployeeErrorCode.EmployeeNotFound,

            52166 or 52172 or 52192 or 52194 =>
                EmployeeErrorCode.ConcurrencyConflict,

            _ =>
                EmployeeErrorCode.None
        };

        return errorCode != EmployeeErrorCode.None;
    }
}
