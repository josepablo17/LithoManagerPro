using System.Data;
using Dapper;
using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Payroll;
using LithoManager.Infrastructure.Persistence.Dapper;
using Microsoft.Data.SqlClient;

namespace LithoManager.Infrastructure.Persistence.Repositories.Payroll;

public sealed class PayrollRepository : IPayrollRepository
{
    private const string GetPayrollConceptsProcedure =
        "Payroll.GetPayrollConcepts";
    private const string GetSocialContributionTypesProcedure =
        "Payroll.GetSocialContributionTypes";
    private const string GetSocialContributionRatesProcedure =
        "Payroll.GetSocialContributionRates";
    private const string GetSocialContributionMinimumBasesProcedure =
        "Payroll.GetSocialContributionMinimumBases";
    private const string GetIncomeTaxBracketsProcedure =
        "Payroll.GetIncomeTaxBrackets";
    private const string GetIncomeTaxCreditsProcedure =
        "Payroll.GetIncomeTaxCredits";
    private const string GetWorkShiftTypesProcedure =
        "Payroll.GetWorkShiftTypes";
    private const string GetOvertimeRulesProcedure =
        "Payroll.GetOvertimeRules";
    private const string GetDisabilityTypesProcedure =
        "Payroll.GetDisabilityTypes";
    private const string GetAguinaldoRulesProcedure =
        "Payroll.GetAguinaldoRules";
    private const string GetAttendanceRecordsProcedure =
        "Payroll.GetAttendanceRecords";
    private const string GetAttendanceRecordByIdProcedure =
        "Payroll.GetAttendanceRecordById";
    private const string GetOvertimeRecordsProcedure =
        "Payroll.GetOvertimeRecords";
    private const string GetOvertimeRecordByIdProcedure =
        "Payroll.GetOvertimeRecordById";
    private const string GetEmployeeDisabilitiesProcedure =
        "Payroll.GetEmployeeDisabilities";
    private const string GetEmployeeDisabilityByIdProcedure =
        "Payroll.GetEmployeeDisabilityById";
    private const string SetEmployeeWorkScheduleProcedure =
        "Payroll.SetEmployeeWorkSchedule";
    private const string SaveAttendanceRecordProcedure =
        "Payroll.SaveAttendanceRecord";
    private const string CreateOvertimeRecordProcedure =
        "Payroll.CreateOvertimeRecord";
    private const string RespondOvertimeRecordProcedure =
        "Payroll.RespondOvertimeRecord";
    private const string CancelOvertimeRecordProcedure =
        "Payroll.CancelOvertimeRecord";
    private const string CreateEmployeeDisabilityProcedure =
        "Payroll.CreateEmployeeDisability";
    private const string ApproveEmployeeDisabilityProcedure =
        "Payroll.ApproveEmployeeDisability";
    private const string CancelEmployeeDisabilityProcedure =
        "Payroll.CancelEmployeeDisability";

    private readonly ISqlConnectionFactory _connectionFactory;

    public PayrollRepository(ISqlConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<PayrollConceptData>>
        GetPayrollConceptsAsync(
            bool? isActive,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetPayrollConceptsProcedure,
            new { IsActive = isActive },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QueryAsync<PayrollConceptData>(
            connection,
            command);
    }

    public async Task<IReadOnlyList<SocialContributionTypeData>>
        GetSocialContributionTypesAsync(
            bool? isActive,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetSocialContributionTypesProcedure,
            new { IsActive = isActive },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QueryAsync<SocialContributionTypeData>(
            connection,
            command);
    }

    public async Task<IReadOnlyList<SocialContributionRateData>>
        GetSocialContributionRatesAsync(
            DateTime? asOfDate,
            bool? isActive,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetSocialContributionRatesProcedure,
            new { AsOfDate = asOfDate?.Date, IsActive = isActive },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QueryAsync<SocialContributionRateData>(
            connection,
            command);
    }

    public async Task<IReadOnlyList<SocialContributionMinimumBaseData>>
        GetSocialContributionMinimumBasesAsync(
            DateTime? asOfDate,
            bool? isActive,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetSocialContributionMinimumBasesProcedure,
            new { AsOfDate = asOfDate?.Date, IsActive = isActive },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QueryAsync<SocialContributionMinimumBaseData>(
            connection,
            command);
    }

    public async Task<IReadOnlyList<IncomeTaxBracketData>>
        GetIncomeTaxBracketsAsync(
            int taxYear,
            string periodicity,
            DateTime? asOfDate,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetIncomeTaxBracketsProcedure,
            new
            {
                TaxYear = taxYear,
                Periodicity = periodicity,
                AsOfDate = asOfDate?.Date
            },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QueryAsync<IncomeTaxBracketData>(
            connection,
            command);
    }

    public async Task<IReadOnlyList<IncomeTaxCreditData>>
        GetIncomeTaxCreditsAsync(
            int taxYear,
            string periodicity,
            DateTime? asOfDate,
            bool? isActive,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetIncomeTaxCreditsProcedure,
            new
            {
                TaxYear = taxYear,
                Periodicity = periodicity,
                AsOfDate = asOfDate?.Date,
                IsActive = isActive
            },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QueryAsync<IncomeTaxCreditData>(
            connection,
            command);
    }

    public async Task<IReadOnlyList<WorkShiftTypeData>>
        GetWorkShiftTypesAsync(
            DateTime? asOfDate,
            bool? isActive,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetWorkShiftTypesProcedure,
            new { AsOfDate = asOfDate?.Date, IsActive = isActive },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QueryAsync<WorkShiftTypeData>(
            connection,
            command);
    }

    public async Task<IReadOnlyList<OvertimeRuleData>>
        GetOvertimeRulesAsync(
            DateTime? asOfDate,
            bool? isActive,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetOvertimeRulesProcedure,
            new { AsOfDate = asOfDate?.Date, IsActive = isActive },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QueryAsync<OvertimeRuleData>(
            connection,
            command);
    }

    public async Task<IReadOnlyList<DisabilityTypeData>>
        GetDisabilityTypesAsync(
            bool? isActive,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetDisabilityTypesProcedure,
            new { IsActive = isActive },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QueryAsync<DisabilityTypeData>(
            connection,
            command);
    }

    public async Task<IReadOnlyList<AguinaldoRuleData>>
        GetAguinaldoRulesAsync(
            DateTime? asOfDate,
            bool? isActive,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetAguinaldoRulesProcedure,
            new { AsOfDate = asOfDate?.Date, IsActive = isActive },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QueryAsync<AguinaldoRuleData>(
            connection,
            command);
    }

    public async Task<IReadOnlyList<AttendanceRecordData>>
        GetAttendanceRecordsAsync(
            int actorUserId,
            int? employeeId,
            int? departmentId,
            string? attendanceStatus,
            bool? isApproved,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? searchTerm,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetAttendanceRecordsProcedure,
            new
            {
                ActorUserId = actorUserId,
                EmployeeId = employeeId,
                DepartmentId = departmentId,
                AttendanceStatus = attendanceStatus,
                IsApproved = isApproved,
                DateFrom = dateFrom?.Date,
                DateTo = dateTo?.Date,
                SearchTerm = searchTerm
            },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QueryAsync<AttendanceRecordData>(
            connection,
            command);
    }

    public async Task<AttendanceRecordData?>
        GetAttendanceRecordByIdAsync(
            int attendanceRecordId,
            int actorUserId,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetAttendanceRecordByIdProcedure,
            new
            {
                AttendanceRecordId = attendanceRecordId,
                ActorUserId = actorUserId
            },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QuerySingleOrDefaultAsync<AttendanceRecordData>(
            connection,
            command);
    }

    public async Task<IReadOnlyList<OvertimeRecordData>>
        GetOvertimeRecordsAsync(
            int actorUserId,
            int? employeeId,
            int? departmentId,
            int? overtimeRuleId,
            string? approvalStatus,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? searchTerm,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetOvertimeRecordsProcedure,
            new
            {
                ActorUserId = actorUserId,
                EmployeeId = employeeId,
                DepartmentId = departmentId,
                OvertimeRuleId = overtimeRuleId,
                ApprovalStatus = approvalStatus,
                DateFrom = dateFrom?.Date,
                DateTo = dateTo?.Date,
                SearchTerm = searchTerm
            },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QueryAsync<OvertimeRecordData>(
            connection,
            command);
    }

    public async Task<OvertimeRecordData?>
        GetOvertimeRecordByIdAsync(
            int overtimeRecordId,
            int actorUserId,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetOvertimeRecordByIdProcedure,
            new
            {
                OvertimeRecordId = overtimeRecordId,
                ActorUserId = actorUserId
            },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QuerySingleOrDefaultAsync<OvertimeRecordData>(
            connection,
            command);
    }

    public async Task<IReadOnlyList<EmployeeDisabilityData>>
        GetEmployeeDisabilitiesAsync(
            int actorUserId,
            int? employeeId,
            int? departmentId,
            int? disabilityTypeId,
            string? disabilityStatus,
            string? issuerInstitution,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? searchTerm,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetEmployeeDisabilitiesProcedure,
            new
            {
                ActorUserId = actorUserId,
                EmployeeId = employeeId,
                DepartmentId = departmentId,
                DisabilityTypeId = disabilityTypeId,
                DisabilityStatus = disabilityStatus,
                IssuerInstitution = issuerInstitution,
                DateFrom = dateFrom?.Date,
                DateTo = dateTo?.Date,
                SearchTerm = searchTerm
            },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QueryAsync<EmployeeDisabilityData>(
            connection,
            command);
    }

    public async Task<EmployeeDisabilityData?>
        GetEmployeeDisabilityByIdAsync(
            int employeeDisabilityId,
            int actorUserId,
            CancellationToken cancellationToken)
    {
        CommandDefinition command = CreateCommand(
            GetEmployeeDisabilityByIdProcedure,
            new
            {
                EmployeeDisabilityId = employeeDisabilityId,
                ActorUserId = actorUserId
            },
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QuerySingleOrDefaultAsync<EmployeeDisabilityData>(
            connection,
            command);
    }

    public async Task<EmployeeWorkScheduleData>
        SetEmployeeWorkScheduleAsync(
            int employeeId,
            int workShiftTypeId,
            DateTime effectiveFromDate,
            int actorUserId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new();
        parameters.Add("EmployeeId", employeeId, DbType.Int32);
        parameters.Add(
            "WorkShiftTypeId",
            workShiftTypeId,
            DbType.Int32);
        parameters.Add(
            "EffectiveFromDate",
            effectiveFromDate.Date,
            DbType.Date);
        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            SetEmployeeWorkScheduleProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QuerySingleAsync<EmployeeWorkScheduleData>(
            connection,
            command);
    }

    public async Task<AttendanceRecordData>
        SaveAttendanceRecordAsync(
            int employeeId,
            DateTime attendanceDate,
            string attendanceStatus,
            decimal expectedHours,
            decimal workedHours,
            decimal paidHours,
            decimal unpaidHours,
            int? workShiftTypeId,
            bool isPaidHoliday,
            bool isApproved,
            string? notes,
            byte[]? expectedRowVersion,
            int actorUserId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new();
        parameters.Add("EmployeeId", employeeId, DbType.Int32);
        parameters.Add(
            "AttendanceDate",
            attendanceDate.Date,
            DbType.Date);
        parameters.Add(
            "AttendanceStatus",
            attendanceStatus,
            DbType.String,
            size: 4000);
        parameters.Add(
            "ExpectedHours",
            expectedHours,
            DbType.Decimal);
        parameters.Add("WorkedHours", workedHours, DbType.Decimal);
        parameters.Add("PaidHours", paidHours, DbType.Decimal);
        parameters.Add("UnpaidHours", unpaidHours, DbType.Decimal);
        parameters.Add(
            "WorkShiftTypeId",
            workShiftTypeId,
            DbType.Int32);
        parameters.Add(
            "IsPaidHoliday",
            isPaidHoliday,
            DbType.Boolean);
        parameters.Add("IsApproved", isApproved, DbType.Boolean);
        parameters.Add("Notes", notes, DbType.String, size: 4000);
        parameters.Add(
            "ExpectedRowVersion",
            expectedRowVersion,
            DbType.Binary,
            size: 8);
        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            SaveAttendanceRecordProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QuerySingleAsync<AttendanceRecordData>(
            connection,
            command);
    }

    public async Task<OvertimeRecordData> CreateOvertimeRecordAsync(
        int employeeId,
        int overtimeRuleId,
        DateTime overtimeDate,
        decimal hours,
        int? attendanceRecordId,
        string? notes,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new();
        parameters.Add("EmployeeId", employeeId, DbType.Int32);
        parameters.Add(
            "OvertimeRuleId",
            overtimeRuleId,
            DbType.Int32);
        parameters.Add(
            "OvertimeDate",
            overtimeDate.Date,
            DbType.Date);
        parameters.Add("Hours", hours, DbType.Decimal);
        parameters.Add(
            "AttendanceRecordId",
            attendanceRecordId,
            DbType.Int32);
        parameters.Add("Notes", notes, DbType.String, size: 4000);
        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            CreateOvertimeRecordProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QuerySingleAsync<OvertimeRecordData>(
            connection,
            command);
    }

    public async Task<OvertimeRecordData> RespondOvertimeRecordAsync(
        int overtimeRecordId,
        bool isApproved,
        string? rejectionReason,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new();
        parameters.Add(
            "OvertimeRecordId",
            overtimeRecordId,
            DbType.Int32);
        parameters.Add("IsApproved", isApproved, DbType.Boolean);
        parameters.Add(
            "RejectionReason",
            rejectionReason,
            DbType.String,
            size: 4000);
        AddRowVersionParameter(parameters, expectedRowVersion);
        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            RespondOvertimeRecordProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QuerySingleAsync<OvertimeRecordData>(
            connection,
            command);
    }

    public async Task<OvertimeRecordData> CancelOvertimeRecordAsync(
        int overtimeRecordId,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new();
        parameters.Add(
            "OvertimeRecordId",
            overtimeRecordId,
            DbType.Int32);
        AddRowVersionParameter(parameters, expectedRowVersion);
        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            CancelOvertimeRecordProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QuerySingleAsync<OvertimeRecordData>(
            connection,
            command);
    }

    public async Task<EmployeeDisabilityData>
        CreateEmployeeDisabilityAsync(
            int employeeId,
            int disabilityTypeId,
            string issuerInstitution,
            DateTime startDate,
            DateTime endDate,
            string? referenceNumber,
            decimal? employerPaidAmount,
            decimal? subsidyAmount,
            string? notes,
            int actorUserId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new();
        parameters.Add("EmployeeId", employeeId, DbType.Int32);
        parameters.Add(
            "DisabilityTypeId",
            disabilityTypeId,
            DbType.Int32);
        parameters.Add(
            "IssuerInstitution",
            issuerInstitution,
            DbType.String,
            size: 4000);
        parameters.Add("StartDate", startDate.Date, DbType.Date);
        parameters.Add("EndDate", endDate.Date, DbType.Date);
        parameters.Add(
            "ReferenceNumber",
            referenceNumber,
            DbType.String,
            size: 4000);
        parameters.Add(
            "EmployerPaidAmount",
            employerPaidAmount,
            DbType.Decimal);
        parameters.Add(
            "SubsidyAmount",
            subsidyAmount,
            DbType.Decimal);
        parameters.Add("Notes", notes, DbType.String, size: 4000);
        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            CreateEmployeeDisabilityProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QuerySingleAsync<EmployeeDisabilityData>(
            connection,
            command);
    }

    public async Task<EmployeeDisabilityData>
        ApproveEmployeeDisabilityAsync(
            int employeeDisabilityId,
            byte[] expectedRowVersion,
            int actorUserId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new();
        parameters.Add(
            "EmployeeDisabilityId",
            employeeDisabilityId,
            DbType.Int32);
        AddRowVersionParameter(parameters, expectedRowVersion);
        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            ApproveEmployeeDisabilityProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QuerySingleAsync<EmployeeDisabilityData>(
            connection,
            command);
    }

    public async Task<EmployeeDisabilityData>
        CancelEmployeeDisabilityAsync(
            int employeeDisabilityId,
            string cancellationReason,
            byte[] expectedRowVersion,
            int actorUserId,
            AuthenticationRequestContext requestContext,
            CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new();
        parameters.Add(
            "EmployeeDisabilityId",
            employeeDisabilityId,
            DbType.Int32);
        parameters.Add(
            "CancellationReason",
            cancellationReason,
            DbType.String,
            size: 4000);
        AddRowVersionParameter(parameters, expectedRowVersion);
        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        CommandDefinition command = CreateCommand(
            CancelEmployeeDisabilityProcedure,
            parameters,
            cancellationToken);

        await using var connection =
            _connectionFactory.CreateConnection();

        return await QuerySingleAsync<EmployeeDisabilityData>(
            connection,
            command);
    }

    private static void AddRowVersionParameter(
        DynamicParameters parameters,
        byte[] expectedRowVersion)
    {
        parameters.Add(
            "ExpectedRowVersion",
            expectedRowVersion,
            DbType.Binary,
            ParameterDirection.Input,
            size: 8);
    }

    private static void AddActorAndRequestContextParameters(
        DynamicParameters parameters,
        int actorUserId,
        AuthenticationRequestContext requestContext)
    {
        parameters.Add(
            "ActorUserId",
            actorUserId,
            DbType.Int32);

        parameters.Add(
            "CorrelationId",
            requestContext.CorrelationId,
            DbType.Guid);

        parameters.Add(
            "ClientIpAddress",
            requestContext.ClientIpAddress,
            DbType.String,
            size: 45);

        parameters.Add(
            "UserAgent",
            requestContext.UserAgent,
            DbType.String,
            size: 512);

        parameters.Add(
            "RequestPath",
            requestContext.RequestPath,
            DbType.String,
            size: 500);
    }

    private static CommandDefinition CreateCommand(
        string procedureName,
        object? parameters,
        CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            commandText: procedureName,
            parameters: parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
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
                out PayrollErrorCode errorCode))
        {
            throw new PayrollPersistenceException(
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
            return await connection.QuerySingleAsync<T>(command);
        }
        catch (SqlException exception)
            when (TryMapSqlException(
                exception,
                out PayrollErrorCode errorCode))
        {
            throw new PayrollPersistenceException(
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
                out PayrollErrorCode errorCode))
        {
            throw new PayrollPersistenceException(
                errorCode,
                exception.Message,
                exception);
        }
    }

    private static bool TryMapSqlException(
        SqlException exception,
        out PayrollErrorCode errorCode)
    {
        errorCode = exception.Number switch
        {
            56001 or 56002 or 56011 or 56012
                or 56101 or 56102 or 56103
                or 56104 or 56105 or 56106
                or 56201 or 56202 or 56203
                or 56204 or 56205 or 56206
                or 56301 or 56302 or 56303
                or 56304 or 56305 or 56306
                or 56316
                or 56401 or 56402 or 56403
                or 56404 or 56405 or 56406
                or 56407 or 56408
                or 56501 or 56502 or 56503
                or 56504 or 56505 or 56506
                or 56601 or 56602 or 56603
                or 56701 or 56702 or 56703
                or 56801 or 56802 or 56803
                or 56804 or 56805
                or 56901 or 56902 or 56903
                or 56904 or 56905 or 56906
                or 57001 or 57002
                or 57101 or 57102 or 57103
                or 57104 or 57105 or 57106
                or 57107
                or 57201 or 57202
                or 57301 or 57302 or 57303
                or 57304 or 57305 or 57306
                or 57307 or 57308
                or 57401 or 57402 =>
                    PayrollErrorCode.InvalidRequest,

            56107 or 56108 or 56109 or 56110
                or 56207 or 56208 or 56209 or 56210
                or 56307 or 56308 or 56309 or 56310
                or 56409 or 56410 or 56411 or 56412
                or 56507 or 56508 or 56509 or 56510
                or 56604 or 56605 or 56606 or 56607
                or 56611
                or 56704 or 56705 or 56706 or 56707
                or 56806 or 56807 or 56808 or 56809
                or 56907 or 56908 or 56909 or 56910
                or 57003 or 57004 or 57005 or 57006
                or 57108 or 57109 or 57110 or 57111
                or 57203 or 57204 or 57205 or 57206
                or 57309 or 57310 or 57311 or 57312
                or 57403 or 57404 or 57405 or 57406 =>
                    PayrollErrorCode.AccessNotAvailable,

            56111 or 56211 or 56311 or 56413 =>
                PayrollErrorCode.EmployeeNotFound,

            56112 or 56212 or 56312 or 56414 =>
                PayrollErrorCode.EmployeeInactive,

            56113 or 56114 or 56213 or 56214
                or 56313 or 56314 or 56415 or 56416 =>
                    PayrollErrorCode.ConfigurationNotFound,

            56315 =>
                PayrollErrorCode.AttendanceRecordNotFound,

            56511 or 56608 =>
                PayrollErrorCode.OvertimeRecordNotFound,

            56708 or 56810 =>
                PayrollErrorCode.EmployeeDisabilityNotFound,

            56215 or 56512 or 56609
                or 56709 or 56811 =>
                    PayrollErrorCode.ConcurrencyConflict,

            56417 =>
                PayrollErrorCode.DuplicateRecord,

            56115 or 56418 =>
                PayrollErrorCode.DateOverlap,

            56216 or 56513 or 56514
                or 56610 or 56612
                or 56710 or 56711
                or 56812 or 56813 =>
                    PayrollErrorCode.InvalidState,

            _ => PayrollErrorCode.None
        };

        return errorCode != PayrollErrorCode.None;
    }
}
