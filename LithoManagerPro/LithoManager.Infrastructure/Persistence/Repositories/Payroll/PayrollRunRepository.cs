using System.Data;
using System.Data.Common;
using Dapper;
using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Payroll;
using LithoManager.Application.Features.Payroll.Runs;
using LithoManager.Application.Features.Payroll.Runs.Calculation;
using LithoManager.Infrastructure.Persistence.Dapper;
using Microsoft.Data.SqlClient;

namespace LithoManager.Infrastructure.Persistence.Repositories.Payroll;

public sealed class PayrollRunRepository : IPayrollRunRepository
{
    private const string GetPayrollRunsProcedure =
        "Payroll.GetPayrollRuns";
    private const string GetPayrollRunByIdProcedure =
        "Payroll.GetPayrollRunById";
    private const string GetPayrollRunEmployeesProcedure =
        "Payroll.GetPayrollRunEmployees";
    private const string GetPayrollRunLinesProcedure =
        "Payroll.GetPayrollRunLines";
    private const string GetPayrollCalculationInputsProcedure =
        "Payroll.GetPayrollCalculationInputs";
    private const string GetAguinaldoCalculationInputsProcedure =
        "Payroll.GetAguinaldoCalculationInputs";
    private const string CreatePayrollRunProcedure =
        "Payroll.CreatePayrollRun";
    private const string SavePayrollRunResultsProcedure =
        "Payroll.SavePayrollRunResults";
    private const string ClosePayrollRunProcedure =
        "Payroll.ClosePayrollRun";

    private readonly ISqlConnectionFactory _connectionFactory;

    public PayrollRunRepository(ISqlConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<PayrollRunData>> GetPayrollRunsAsync(
        int? periodYear,
        string? runType,
        CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new();
        parameters.Add("PeriodYear", periodYear, DbType.Int16);
        parameters.Add("RunType", runType, DbType.String, size: 20);

        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        IEnumerable<PayrollRunData> runs =
            await connection.QueryAsync<PayrollRunData>(
                CreateCommand(
                    GetPayrollRunsProcedure,
                    parameters,
                    cancellationToken));

        return runs.ToList();
    }

    public async Task<PayrollRunData?> GetPayrollRunByIdAsync(
        int payrollRunId,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<PayrollRunData>(
            CreateCommand(
                GetPayrollRunByIdProcedure,
                new { PayrollRunId = payrollRunId },
                cancellationToken));
    }

    public async Task<IReadOnlyList<PayrollRunEmployeeData>> GetPayrollRunEmployeesAsync(
        int payrollRunId,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        IEnumerable<PayrollRunEmployeeData> employees =
            await connection.QueryAsync<PayrollRunEmployeeData>(
                CreateCommand(
                    GetPayrollRunEmployeesProcedure,
                    new { PayrollRunId = payrollRunId },
                    cancellationToken));

        return employees.ToList();
    }

    public async Task<IReadOnlyList<PayrollRunLineData>> GetPayrollRunLinesAsync(
        int payrollRunId,
        int? employeeId,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        IEnumerable<PayrollRunLineData> lines =
            await connection.QueryAsync<PayrollRunLineData>(
                CreateCommand(
                    GetPayrollRunLinesProcedure,
                    new
                    {
                        PayrollRunId = payrollRunId,
                        EmployeeId = employeeId,
                    },
                    cancellationToken));

        return lines.ToList();
    }

    public async Task<PayrollCalculationInputs> GetPayrollCalculationInputsAsync(
        PayrollRunData payrollRun,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payrollRun);

        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        await using SqlMapper.GridReader reader =
            await connection.QueryMultipleAsync(
                CreateCommand(
                    GetPayrollCalculationInputsProcedure,
                    new { payrollRun.PayrollRunId },
                    cancellationToken));

        // Result sets are read in the order the procedure returns them.
        return new PayrollCalculationInputs
        {
            PeriodStartDate = payrollRun.PeriodStartDate,
            PeriodEndDate = payrollRun.PeriodEndDate,
            Employees = (await reader.ReadAsync<CalculationEmployee>()).ToList(),
            SalarySegments = (await reader.ReadAsync<SalarySegment>()).ToList(),
            WorkSchedules = (await reader.ReadAsync<WorkSchedule>()).ToList(),
            Overtime = (await reader.ReadAsync<ApprovedOvertime>()).ToList(),
            Disabilities = (await reader.ReadAsync<ApprovedDisability>()).ToList(),
            Concepts = (await reader.ReadAsync<CalculationConcept>()).ToList(),
            ContributionRates = (await reader.ReadAsync<ContributionRate>()).ToList(),
            MinimumBases = (await reader.ReadAsync<ContributionMinimumBase>()).ToList(),
            IncomeTaxBrackets = (await reader.ReadAsync<IncomeTaxBracket>()).ToList(),
        };
    }

    public async Task<AguinaldoCalculationInputs> GetAguinaldoCalculationInputsAsync(
        PayrollRunData payrollRun,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payrollRun);

        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        await using SqlMapper.GridReader reader =
            await connection.QueryMultipleAsync(
                CreateCommand(
                    GetAguinaldoCalculationInputsProcedure,
                    new { payrollRun.PayrollRunId },
                    cancellationToken));

        return new AguinaldoCalculationInputs
        {
            WindowStartDate = payrollRun.PeriodStartDate,
            WindowEndDate = payrollRun.PeriodEndDate,
            Employees = (await reader.ReadAsync<AguinaldoEmployee>()).ToList(),
            SourceLines = (await reader.ReadAsync<AguinaldoSourceLine>()).ToList(),
            ClosedPeriods = (await reader.ReadAsync<ClosedPayrollPeriod>()).ToList(),
            Rule = (await reader.ReadAsync<AguinaldoRule>()).FirstOrDefault(),
            DisabilityTypes = (await reader.ReadAsync<DisabilityTypeFlag>()).ToList(),
            Concepts = (await reader.ReadAsync<CalculationConcept>()).ToList(),
        };
    }

    public async Task<PayrollRunData> CreatePayrollRunAsync(
        string runType,
        int periodYear,
        int? periodMonth,
        DateTime? paymentDate,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new();
        parameters.Add("RunType", runType, DbType.String, size: 20);
        parameters.Add("PeriodYear", periodYear, DbType.Int16);
        parameters.Add("PeriodMonth", periodMonth, DbType.Byte);
        parameters.Add("PaymentDate", paymentDate?.Date, DbType.Date);
        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        return await ExecuteMutationAsync(
            CreatePayrollRunProcedure,
            parameters,
            cancellationToken);
    }

    public async Task<PayrollRunData> SavePayrollRunResultsAsync(
        int payrollRunId,
        byte[] expectedRowVersion,
        string resultsJson,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new();
        parameters.Add("PayrollRunId", payrollRunId, DbType.Int32);
        AddRowVersionParameter(parameters, expectedRowVersion);
        parameters.Add(
            "ResultsJson",
            resultsJson,
            DbType.String,
            size: -1);
        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        return await ExecuteMutationAsync(
            SavePayrollRunResultsProcedure,
            parameters,
            cancellationToken);
    }

    public async Task<PayrollRunData> ClosePayrollRunAsync(
        int payrollRunId,
        byte[] expectedRowVersion,
        int actorUserId,
        AuthenticationRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        DynamicParameters parameters = new();
        parameters.Add("PayrollRunId", payrollRunId, DbType.Int32);
        AddRowVersionParameter(parameters, expectedRowVersion);
        AddActorAndRequestContextParameters(
            parameters,
            actorUserId,
            requestContext);

        return await ExecuteMutationAsync(
            ClosePayrollRunProcedure,
            parameters,
            cancellationToken);
    }

    private async Task<PayrollRunData> ExecuteMutationAsync(
        string procedureName,
        DynamicParameters parameters,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection =
            _connectionFactory.CreateConnection();

        try
        {
            return await connection.QuerySingleAsync<PayrollRunData>(
                CreateCommand(
                    procedureName,
                    parameters,
                    cancellationToken));
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
        parameters.Add("ActorUserId", actorUserId, DbType.Int32);
        parameters.Add("CorrelationId", requestContext.CorrelationId, DbType.Guid);
        parameters.Add("ClientIpAddress", requestContext.ClientIpAddress, DbType.String, size: 45);
        parameters.Add("UserAgent", requestContext.UserAgent, DbType.String, size: 512);
        parameters.Add("RequestPath", requestContext.RequestPath, DbType.String, size: 500);
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

    /// <summary>
    /// Error ranges: 581xx CreatePayrollRun, 582xx SavePayrollRunResults,
    /// 583xx ClosePayrollRun. Within each range +05..+09 come from
    /// Payroll.AuthorizePayrollRunActor.
    /// </summary>
    private static bool TryMapSqlException(
        SqlException exception,
        out PayrollErrorCode errorCode)
    {
        errorCode = exception.Number switch
        {
            58101 or 58102 or 58103 or 58104 or 58105
                or 58201 or 58202 or 58203 or 58205
                or 58301 or 58302 or 58305 =>
                    PayrollErrorCode.InvalidRequest,

            58106 or 58107 or 58108 or 58109
                or 58206 or 58207 or 58208 or 58209
                or 58306 or 58307 or 58308 or 58309 =>
                    PayrollErrorCode.AccessNotAvailable,

            58110 => PayrollErrorCode.DuplicateRecord,

            58111 => PayrollErrorCode.ConfigurationNotFound,

            58210 or 58310 => PayrollErrorCode.PayrollRunNotFound,

            58211 or 58311 => PayrollErrorCode.InvalidState,

            58212 or 58312 => PayrollErrorCode.ConcurrencyConflict,

            _ => PayrollErrorCode.None,
        };

        return errorCode != PayrollErrorCode.None;
    }
}
