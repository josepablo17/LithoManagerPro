using System.Data;
using Dapper;
using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.LeaveManagement;
using LithoManager.Infrastructure.Persistence.Dapper;
using Microsoft.Data.SqlClient;

namespace LithoManager.Infrastructure.Persistence
    .Repositories.LeaveManagement;

public sealed class LeaveManagementRepository
    : ILeaveManagementRepository
{
    private const string GetLeaveTypesProcedure =
        "LeaveManagement.GetLeaveTypes";

    private const string GetLeaveRequestStatusesProcedure =
        "LeaveManagement.GetLeaveRequestStatuses";

    private const string GetEmployeeLeaveBalanceProcedure =
        "LeaveManagement.GetEmployeeLeaveBalance";

    private const string AdjustEmployeeLeaveBalanceProcedure =
        "LeaveManagement.AdjustEmployeeLeaveBalance";

    private const string GetMyLeaveRequestsProcedure =
        "LeaveManagement.GetMyLeaveRequests";

    private const string GetLeaveRequestsProcedure =
        "LeaveManagement.GetLeaveRequests";

    private const string GetLeaveRequestByIdProcedure =
        "LeaveManagement.GetLeaveRequestById";

    private const string CreateLeaveRequestProcedure =
        "LeaveManagement.CreateLeaveRequest";

    private const string CancelLeaveRequestProcedure =
        "LeaveManagement.CancelLeaveRequest";

    private const string RespondLeaveRequestProcedure =
        "LeaveManagement.RespondLeaveRequest";

    private readonly ISqlConnectionFactory _connectionFactory;

    public LeaveManagementRepository(
        ISqlConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(
            connectionFactory);

        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<LeaveTypeData>>
        GetLeaveTypesAsync(
            bool? isActive,
            CancellationToken cancellationToken)
    {
        var parameters = new
        {
            IsActive = isActive
        };

        CommandDefinition command = CreateCommand(
            GetLeaveTypesProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        IEnumerable<LeaveTypeData> leaveTypes =
            await connection.QueryAsync<LeaveTypeData>(
                command);

        return leaveTypes
            .Select(NormalizeDates)
            .ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestStatusData>>
        GetLeaveRequestStatusesAsync(
            bool? isActive,
            CancellationToken cancellationToken)
    {
        var parameters = new
        {
            IsActive = isActive
        };

        CommandDefinition command = CreateCommand(
            GetLeaveRequestStatusesProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        IEnumerable<LeaveRequestStatusData> statuses =
            await connection.QueryAsync<LeaveRequestStatusData>(
                command);

        return statuses
            .Select(NormalizeDates)
            .ToList();
    }

    public async Task<EmployeeLeaveBalanceData?>
        GetEmployeeLeaveBalanceAsync(
            int? employeeId,
            string leaveTypeCode,
            int actorUserId,
            CancellationToken cancellationToken)
    {
        if (employeeId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(employeeId),
                "EmployeeId must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            leaveTypeCode);

        ValidateActorUserId(actorUserId);

        var parameters = new
        {
            EmployeeId = employeeId,
            LeaveTypeCode = leaveTypeCode.Trim(),
            ActorUserId = actorUserId
        };

        CommandDefinition command = CreateCommand(
            GetEmployeeLeaveBalanceProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        EmployeeLeaveBalanceData? result =
            await QuerySingleOrDefaultAsync<
                EmployeeLeaveBalanceData>(
                    connection,
                    command);

        return result is null
            ? null
            : NormalizeDates(result);
    }

    public async Task<EmployeeLeaveBalanceData>
        AdjustEmployeeLeaveBalanceAsync(
            int employeeId,
            string leaveTypeCode,
            decimal adjustedDaysDelta,
            int actorUserId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        ValidateEmployeeId(employeeId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            leaveTypeCode);

        if (adjustedDaysDelta == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(adjustedDaysDelta),
                "AdjustedDaysDelta must be different from zero.");
        }

        ValidateActorUserId(actorUserId);
        ValidateRequestContext(requestContext);

        DynamicParameters parameters = new();

        parameters.Add(
            "EmployeeId",
            employeeId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "LeaveTypeCode",
            leaveTypeCode.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        parameters.Add(
            "AdjustedDaysDelta",
            adjustedDaysDelta,
            DbType.Decimal,
            ParameterDirection.Input);

        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            AdjustEmployeeLeaveBalanceProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        EmployeeLeaveBalanceData result =
            await QuerySingleAsync<EmployeeLeaveBalanceData>(
                connection,
                command);

        return NormalizeDates(result);
    }

    public async Task<IReadOnlyList<LeaveRequestData>>
        GetMyLeaveRequestsAsync(
            int actorUserId,
            string? leaveRequestStatusCode,
            DateTime? startDateFrom,
            DateTime? startDateTo,
            CancellationToken cancellationToken)
    {
        ValidateActorUserId(actorUserId);
        ValidateDateRange(startDateFrom, startDateTo);

        var parameters = new
        {
            ActorUserId = actorUserId,
            LeaveRequestStatusCode =
                NormalizeOptionalString(
                    leaveRequestStatusCode),
            StartDateFrom = startDateFrom?.Date,
            StartDateTo = startDateTo?.Date
        };

        CommandDefinition command = CreateCommand(
            GetMyLeaveRequestsProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        IEnumerable<LeaveRequestData> requests =
            await QueryAsync<LeaveRequestData>(
                connection,
                command);

        return requests
            .Select(NormalizeDates)
            .ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestData>>
        GetLeaveRequestsAsync(
            int actorUserId,
            string? leaveRequestStatusCode,
            int? employeeId,
            int? departmentId,
            DateTime? startDateFrom,
            DateTime? startDateTo,
            string? searchTerm,
            CancellationToken cancellationToken)
    {
        ValidateActorUserId(actorUserId);
        ValidateDateRange(startDateFrom, startDateTo);

        if (employeeId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(employeeId),
                "EmployeeId must be greater than zero.");
        }

        if (departmentId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(departmentId),
                "DepartmentId must be greater than zero.");
        }

        var parameters = new
        {
            ActorUserId = actorUserId,
            LeaveRequestStatusCode =
                NormalizeOptionalString(
                    leaveRequestStatusCode),
            EmployeeId = employeeId,
            DepartmentId = departmentId,
            StartDateFrom = startDateFrom?.Date,
            StartDateTo = startDateTo?.Date,
            SearchTerm = NormalizeOptionalString(searchTerm)
        };

        CommandDefinition command = CreateCommand(
            GetLeaveRequestsProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        IEnumerable<LeaveRequestData> requests =
            await QueryAsync<LeaveRequestData>(
                connection,
                command);

        return requests
            .Select(NormalizeDates)
            .ToList();
    }

    public async Task<LeaveRequestData?>
        GetLeaveRequestByIdAsync(
            int leaveRequestId,
            int actorUserId,
            CancellationToken cancellationToken)
    {
        ValidateLeaveRequestId(leaveRequestId);
        ValidateActorUserId(actorUserId);

        var parameters = new
        {
            LeaveRequestId = leaveRequestId,
            ActorUserId = actorUserId
        };

        CommandDefinition command = CreateCommand(
            GetLeaveRequestByIdProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        LeaveRequestData? result =
            await QuerySingleOrDefaultAsync<
                LeaveRequestData>(
                    connection,
                    command);

        return result is null
            ? null
            : NormalizeDates(result);
    }

    public async Task<LeaveRequestData> CreateLeaveRequestAsync(
        DateTime startDate,
        DateTime endDate,
        int actorUserId,
        string leaveTypeCode,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ValidateLeaveRequestDates(startDate, endDate);
        ValidateActorUserId(actorUserId);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            leaveTypeCode);

        ValidateRequestContext(requestContext);

        DynamicParameters parameters = new();

        parameters.Add(
            "StartDate",
            startDate.Date,
            DbType.Date,
            ParameterDirection.Input);

        parameters.Add(
            "EndDate",
            endDate.Date,
            DbType.Date,
            ParameterDirection.Input);

        parameters.Add(
            "ActorUserId",
            actorUserId,
            DbType.Int32,
            ParameterDirection.Input);

        parameters.Add(
            "LeaveTypeCode",
            leaveTypeCode.Trim(),
            DbType.String,
            ParameterDirection.Input,
            size: 4000);

        AddRequestContextParameters(
            parameters,
            requestContext);

        CommandDefinition command = CreateCommand(
            CreateLeaveRequestProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        LeaveRequestData result =
            await QuerySingleAsync<LeaveRequestData>(
                connection,
                command);

        return NormalizeDates(result);
    }

    public async Task<LeaveRequestData> CancelLeaveRequestAsync(
        int leaveRequestId,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ValidateLeaveRequestId(leaveRequestId);
        ValidateRowVersion(expectedRowVersion);
        ValidateActorUserId(actorUserId);
        ValidateRequestContext(requestContext);

        DynamicParameters parameters =
            CreateLeaveRequestCommandParameters(
                leaveRequestId,
                expectedRowVersion,
                actorUserId,
                requestContext);

        CommandDefinition command = CreateCommand(
            CancelLeaveRequestProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        LeaveRequestData result =
            await QuerySingleAsync<LeaveRequestData>(
                connection,
                command);

        return NormalizeDates(result);
    }

    public async Task<LeaveRequestData> RespondLeaveRequestAsync(
        int leaveRequestId,
        bool isApproved,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ValidateLeaveRequestId(leaveRequestId);
        ValidateRowVersion(expectedRowVersion);
        ValidateActorUserId(actorUserId);
        ValidateRequestContext(requestContext);

        DynamicParameters parameters =
            CreateLeaveRequestCommandParameters(
                leaveRequestId,
                expectedRowVersion,
                actorUserId,
                requestContext);

        parameters.Add(
            "IsApproved",
            isApproved,
            DbType.Boolean,
            ParameterDirection.Input);

        CommandDefinition command = CreateCommand(
            RespondLeaveRequestProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        LeaveRequestData result =
            await QuerySingleAsync<LeaveRequestData>(
                connection,
                command);

        return NormalizeDates(result);
    }

    private static DynamicParameters
        CreateLeaveRequestCommandParameters(
            int leaveRequestId,
            byte[] expectedRowVersion,
            int actorUserId,
            AuthenticationRequestContext requestContext)
    {
        DynamicParameters parameters = new();

        parameters.Add(
            "LeaveRequestId",
            leaveRequestId,
            DbType.Int32,
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

        AddRequestContextParameters(
            parameters,
            requestContext);
    }

    private static void AddRequestContextParameters(
        DynamicParameters parameters,
        AuthenticationRequestContext requestContext)
    {
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

    private static CommandDefinition CreateCommand(
        string procedureName,
        object? parameters,
        CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            commandText:
                procedureName,
            parameters:
                parameters,
            commandType:
                CommandType.StoredProcedure,
            cancellationToken:
                cancellationToken);
    }

    private static string? NormalizeOptionalString(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
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

    private static void ValidateLeaveRequestId(
        int leaveRequestId)
    {
        if (leaveRequestId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaveRequestId),
                "LeaveRequestId must be greater than zero.");
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

    private static void ValidateDateRange(
        DateTime? startDateFrom,
        DateTime? startDateTo)
    {
        if (startDateFrom.HasValue
            && startDateTo.HasValue
            && startDateTo.Value.Date
                < startDateFrom.Value.Date)
        {
            throw new ArgumentException(
                "StartDateTo cannot be earlier than StartDateFrom.",
                nameof(startDateTo));
        }
    }

    private static void ValidateLeaveRequestDates(
        DateTime startDate,
        DateTime endDate)
    {
        if (endDate.Date < startDate.Date)
        {
            throw new ArgumentException(
                "EndDate cannot be earlier than StartDate.",
                nameof(endDate));
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

    private static LeaveTypeData NormalizeDates(
        LeaveTypeData leaveType)
    {
        return new LeaveTypeData
        {
            LeaveTypeId = leaveType.LeaveTypeId,
            LeaveTypeCode = leaveType.LeaveTypeCode,
            Name = leaveType.Name,
            AffectsVacationBalance =
                leaveType.AffectsVacationBalance,
            IsActive = leaveType.IsActive,
            CreatedAtUtc = SpecifyUtc(
                leaveType.CreatedAtUtc),
            CreatedByUserId = leaveType.CreatedByUserId,
            UpdatedAtUtc = SpecifyNullableUtc(
                leaveType.UpdatedAtUtc),
            UpdatedByUserId = leaveType.UpdatedByUserId,
            RowVersion = leaveType.RowVersion
        };
    }

    private static LeaveRequestStatusData NormalizeDates(
        LeaveRequestStatusData status)
    {
        return new LeaveRequestStatusData
        {
            LeaveRequestStatusCode =
                status.LeaveRequestStatusCode,
            Name = status.Name,
            SortOrder = status.SortOrder,
            IsTerminal = status.IsTerminal,
            IsActive = status.IsActive,
            CreatedAtUtc = SpecifyUtc(
                status.CreatedAtUtc),
            UpdatedAtUtc = SpecifyNullableUtc(
                status.UpdatedAtUtc),
            RowVersion = status.RowVersion
        };
    }

    private static EmployeeLeaveBalanceData NormalizeDates(
        EmployeeLeaveBalanceData balance)
    {
        return new EmployeeLeaveBalanceData
        {
            EmployeeLeaveBalanceId =
                balance.EmployeeLeaveBalanceId,
            EmployeeId = balance.EmployeeId,
            IdentificationNumber =
                balance.IdentificationNumber,
            FirstName = balance.FirstName,
            LastName = balance.LastName,
            EmployeeName = balance.EmployeeName,
            DepartmentId = balance.DepartmentId,
            DepartmentCode = balance.DepartmentCode,
            DepartmentName = balance.DepartmentName,
            LeaveTypeId = balance.LeaveTypeId,
            LeaveTypeCode = balance.LeaveTypeCode,
            LeaveTypeName = balance.LeaveTypeName,
            AffectsVacationBalance =
                balance.AffectsVacationBalance,
            LeavePolicyId = balance.LeavePolicyId,
            LeavePolicyCode = balance.LeavePolicyCode,
            LeavePolicyName = balance.LeavePolicyName,
            EntitlementDays = balance.EntitlementDays,
            EntitlementWeeks = balance.EntitlementWeeks,
            UsesBusinessDays = balance.UsesBusinessDays,
            AccruedDays = balance.AccruedDays,
            AdjustedDays = balance.AdjustedDays,
            PendingDays = balance.PendingDays,
            UsedDays = balance.UsedDays,
            AvailableDays = balance.AvailableDays,
            CreatedAtUtc = SpecifyUtc(
                balance.CreatedAtUtc),
            CreatedByUserId = balance.CreatedByUserId,
            UpdatedAtUtc = SpecifyNullableUtc(
                balance.UpdatedAtUtc),
            UpdatedByUserId = balance.UpdatedByUserId,
            RowVersion = balance.RowVersion
        };
    }

    private static LeaveRequestData NormalizeDates(
        LeaveRequestData leaveRequest)
    {
        return new LeaveRequestData
        {
            LeaveRequestId = leaveRequest.LeaveRequestId,
            EmployeeId = leaveRequest.EmployeeId,
            IdentificationNumber =
                leaveRequest.IdentificationNumber,
            FirstName = leaveRequest.FirstName,
            LastName = leaveRequest.LastName,
            DepartmentId = leaveRequest.DepartmentId,
            DepartmentCode = leaveRequest.DepartmentCode,
            DepartmentName = leaveRequest.DepartmentName,
            LeaveTypeId = leaveRequest.LeaveTypeId,
            LeaveTypeCode = leaveRequest.LeaveTypeCode,
            LeaveTypeName = leaveRequest.LeaveTypeName,
            LeaveRequestStatusCode =
                leaveRequest.LeaveRequestStatusCode,
            LeaveRequestStatusName =
                leaveRequest.LeaveRequestStatusName,
            StartDate = leaveRequest.StartDate.Date,
            EndDate = leaveRequest.EndDate.Date,
            RequestedDays = leaveRequest.RequestedDays,
            RespondedAtUtc = SpecifyNullableUtc(
                leaveRequest.RespondedAtUtc),
            RespondedByUserId =
                leaveRequest.RespondedByUserId,
            RespondedByEmailAddress =
                leaveRequest.RespondedByEmailAddress,
            CancelledAtUtc = SpecifyNullableUtc(
                leaveRequest.CancelledAtUtc),
            CancelledByUserId =
                leaveRequest.CancelledByUserId,
            CancelledByEmailAddress =
                leaveRequest.CancelledByEmailAddress,
            CreatedAtUtc = SpecifyUtc(
                leaveRequest.CreatedAtUtc),
            CreatedByUserId = leaveRequest.CreatedByUserId,
            CreatedByEmailAddress =
                leaveRequest.CreatedByEmailAddress,
            UpdatedAtUtc = SpecifyNullableUtc(
                leaveRequest.UpdatedAtUtc),
            UpdatedByUserId = leaveRequest.UpdatedByUserId,
            UpdatedByEmailAddress =
                leaveRequest.UpdatedByEmailAddress,
            RowVersion = leaveRequest.RowVersion
        };
    }

    private static DateTime SpecifyUtc(
        DateTime value)
    {
        return DateTime.SpecifyKind(
            value,
            DateTimeKind.Utc);
    }

    private static DateTime? SpecifyNullableUtc(
        DateTime? value)
    {
        return value is DateTime dateTime
            ? DateTime.SpecifyKind(
                dateTime,
                DateTimeKind.Utc)
            : null;
    }

    private static async Task<IReadOnlyList<T>> QueryAsync<T>(
        System.Data.Common.DbConnection connection,
        CommandDefinition command)
    {
        try
        {
            IEnumerable<T> result =
                await connection.QueryAsync<T>(command);

            return result.ToList();
        }
        catch (SqlException exception)
            when (TryMapSqlException(
                exception,
                out LeaveManagementErrorCode errorCode))
        {
            throw new LeaveManagementPersistenceException(
                errorCode,
                exception.Message,
                exception);
        }
    }

    private static async Task<T> QuerySingleAsync<T>(
        System.Data.Common.DbConnection connection,
        CommandDefinition command)
    {
        try
        {
            return await connection.QuerySingleAsync<T>(
                command);
        }
        catch (SqlException exception)
            when (TryMapSqlException(
                exception,
                out LeaveManagementErrorCode errorCode))
        {
            throw new LeaveManagementPersistenceException(
                errorCode,
                exception.Message,
                exception);
        }
    }

    private static async Task<T?> QuerySingleOrDefaultAsync<T>(
        System.Data.Common.DbConnection connection,
        CommandDefinition command)
    {
        try
        {
            return await connection.QuerySingleOrDefaultAsync<T>(
                command);
        }
        catch (SqlException exception)
            when (TryMapSqlException(
                exception,
                out LeaveManagementErrorCode errorCode))
        {
            throw new LeaveManagementPersistenceException(
                errorCode,
                exception.Message,
                exception);
        }
    }

    private static bool TryMapSqlException(
        SqlException exception,
        out LeaveManagementErrorCode errorCode)
    {
        errorCode = exception.Number switch
        {
            53001 or 53002 or 53003 or 53004
                or 53101 or 53102 or 53103
                or 53201 or 53202 or 53203
                or 53204 or 53205 or 53206
                or 53301 or 53302 or 53303
                or 53304 or 53305
                or 53401 or 53402 or 53403
                or 53404 or 53405 or 53406
                or 53407
                or 53501 or 53502 or 53503
                or 53601 or 53602 or 53603
                or 53604 or 53701 =>
                    LeaveManagementErrorCode.InvalidRequest,

            53005 or 53006 or 53007
                or 53008 or 53009 or 53010
                or 53011
                or 53104 or 53105 or 53106
                or 53107 or 53108 or 53109
                or 53207 or 53208 or 53209
                or 53210 or 53211 or 53212
                or 53306 or 53307 or 53308
                or 53309 or 53310 or 53311
                or 53408 or 53409 or 53410
                or 53411 or 53412 or 53413
                or 53504 or 53505 or 53506
                or 53507 or 53508 or 53512
                or 53605 or 53606 or 53607
                or 53608 or 53609 or 53610
                or 53702 or 53703 or 53704
                or 53705 or 53706 =>
                    LeaveManagementErrorCode.AccessNotAvailable,

            53312 =>
                LeaveManagementErrorCode.EmployeeNotFound,

            53313 =>
                LeaveManagementErrorCode.EmployeeInactive,

            53314 =>
                LeaveManagementErrorCode.DepartmentInactive,

            53315 or 53414 =>
                LeaveManagementErrorCode.LeaveTypeNotFound,

            53316 =>
                LeaveManagementErrorCode.LeavePolicyNotFound,

            53416 or 53513 or 53614 =>
                LeaveManagementErrorCode.LeaveBalanceNotFound,

            53417 =>
                LeaveManagementErrorCode.InsufficientLeaveBalance,

            53418 =>
                LeaveManagementErrorCode.PendingLeaveRequestExists,

            53419 =>
                LeaveManagementErrorCode.LeaveRequestDateOverlap,

            53509 or 53611 =>
                LeaveManagementErrorCode.LeaveRequestNotFound,

            53510 or 53612 =>
                LeaveManagementErrorCode.ConcurrencyConflict,

            53511 or 53613 =>
                LeaveManagementErrorCode.LeaveRequestAlreadyResolved,

            _ =>
                LeaveManagementErrorCode.None
        };

        return errorCode != LeaveManagementErrorCode.None;
    }
}
