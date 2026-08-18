using LithoManager.Api.Authorization;
using LithoManager.Api.Contracts.Payroll;
using LithoManager.Api.Extensions;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Payroll;
using LithoManager.Application.Features.Payroll.ApproveEmployeeDisability;
using LithoManager.Application.Features.Payroll.CancelEmployeeDisability;
using LithoManager.Application.Features.Payroll.CancelOvertimeRecord;
using LithoManager.Application.Features.Payroll.CreateEmployeeDisability;
using LithoManager.Application.Features.Payroll.CreateOvertimeRecord;
using LithoManager.Application.Features.Payroll.GetPayrollConfiguration;
using LithoManager.Application.Features.Payroll.RespondOvertimeRecord;
using LithoManager.Application.Features.Payroll.SaveAttendanceRecord;
using LithoManager.Application.Features.Payroll.SetEmployeeWorkSchedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithoManager.Api.Controllers;

[ApiController]
[Route("api/payroll")]
[Authorize(
    Policy =
        AuthorizationPolicyNames.PayrollAdministration)]
public sealed class PayrollController : ControllerBase
{
    private readonly IPayrollConfigurationService
        _payrollConfigurationService;
    private readonly ISetEmployeeWorkScheduleService
        _setEmployeeWorkScheduleService;
    private readonly ISaveAttendanceRecordService
        _saveAttendanceRecordService;
    private readonly ICreateOvertimeRecordService
        _createOvertimeRecordService;
    private readonly IRespondOvertimeRecordService
        _respondOvertimeRecordService;
    private readonly ICancelOvertimeRecordService
        _cancelOvertimeRecordService;
    private readonly ICreateEmployeeDisabilityService
        _createEmployeeDisabilityService;
    private readonly IApproveEmployeeDisabilityService
        _approveEmployeeDisabilityService;
    private readonly ICancelEmployeeDisabilityService
        _cancelEmployeeDisabilityService;

    public PayrollController(
        IPayrollConfigurationService payrollConfigurationService,
        ISetEmployeeWorkScheduleService
            setEmployeeWorkScheduleService,
        ISaveAttendanceRecordService saveAttendanceRecordService,
        ICreateOvertimeRecordService createOvertimeRecordService,
        IRespondOvertimeRecordService respondOvertimeRecordService,
        ICancelOvertimeRecordService cancelOvertimeRecordService,
        ICreateEmployeeDisabilityService
            createEmployeeDisabilityService,
        IApproveEmployeeDisabilityService
            approveEmployeeDisabilityService,
        ICancelEmployeeDisabilityService
            cancelEmployeeDisabilityService)
    {
        ArgumentNullException.ThrowIfNull(
            payrollConfigurationService);
        ArgumentNullException.ThrowIfNull(
            setEmployeeWorkScheduleService);
        ArgumentNullException.ThrowIfNull(
            saveAttendanceRecordService);
        ArgumentNullException.ThrowIfNull(
            createOvertimeRecordService);
        ArgumentNullException.ThrowIfNull(
            respondOvertimeRecordService);
        ArgumentNullException.ThrowIfNull(
            cancelOvertimeRecordService);
        ArgumentNullException.ThrowIfNull(
            createEmployeeDisabilityService);
        ArgumentNullException.ThrowIfNull(
            approveEmployeeDisabilityService);
        ArgumentNullException.ThrowIfNull(
            cancelEmployeeDisabilityService);

        _payrollConfigurationService =
            payrollConfigurationService;
        _setEmployeeWorkScheduleService =
            setEmployeeWorkScheduleService;
        _saveAttendanceRecordService =
            saveAttendanceRecordService;
        _createOvertimeRecordService =
            createOvertimeRecordService;
        _respondOvertimeRecordService =
            respondOvertimeRecordService;
        _cancelOvertimeRecordService =
            cancelOvertimeRecordService;
        _createEmployeeDisabilityService =
            createEmployeeDisabilityService;
        _approveEmployeeDisabilityService =
            approveEmployeeDisabilityService;
        _cancelEmployeeDisabilityService =
            cancelEmployeeDisabilityService;
    }

    [HttpGet("configuration/concepts")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<
        PayrollConceptResponse>>> GetPayrollConcepts(
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        PayrollItemsResult<PayrollConceptInfo> result =
            await _payrollConfigurationService
                .GetPayrollConceptsAsync(
                    new ActivePayrollConfigurationQuery(
                        IsActive: isActive),
                    cancellationToken);

        return ToActionResult(
            result,
            PayrollResponseMapper.Map);
    }

    [HttpGet("configuration/social-contribution-types")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<
        SocialContributionTypeResponse>>>
        GetSocialContributionTypes(
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        PayrollItemsResult<SocialContributionTypeInfo> result =
            await _payrollConfigurationService
                .GetSocialContributionTypesAsync(
                    new ActivePayrollConfigurationQuery(
                        IsActive: isActive),
                    cancellationToken);

        return ToActionResult(
            result,
            PayrollResponseMapper.Map);
    }

    [HttpGet("configuration/social-contribution-rates")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<
        SocialContributionRateResponse>>>
        GetSocialContributionRates(
            [FromQuery] DateTime? asOfDate,
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        PayrollItemsResult<SocialContributionRateInfo> result =
            await _payrollConfigurationService
                .GetSocialContributionRatesAsync(
                    new EffectivePayrollConfigurationQuery(
                        AsOfDate: asOfDate,
                        IsActive: isActive),
                    cancellationToken);

        return ToActionResult(
            result,
            PayrollResponseMapper.Map);
    }

    [HttpGet("configuration/social-contribution-minimum-bases")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<
        SocialContributionMinimumBaseResponse>>>
        GetSocialContributionMinimumBases(
            [FromQuery] DateTime? asOfDate,
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        PayrollItemsResult<SocialContributionMinimumBaseInfo>
            result =
                await _payrollConfigurationService
                    .GetSocialContributionMinimumBasesAsync(
                        new EffectivePayrollConfigurationQuery(
                            AsOfDate: asOfDate,
                            IsActive: isActive),
                        cancellationToken);

        return ToActionResult(
            result,
            PayrollResponseMapper.Map);
    }

    [HttpGet("configuration/income-tax-brackets")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<
        IncomeTaxBracketResponse>>> GetIncomeTaxBrackets(
            [FromQuery] int taxYear,
            [FromQuery] string? periodicity,
            [FromQuery] DateTime? asOfDate,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        PayrollItemsResult<IncomeTaxBracketInfo> result =
            await _payrollConfigurationService
                .GetIncomeTaxBracketsAsync(
                    new IncomeTaxConfigurationQuery(
                        TaxYear: taxYear,
                        Periodicity: periodicity,
                        AsOfDate: asOfDate,
                        IsActive: null),
                    cancellationToken);

        return ToActionResult(
            result,
            PayrollResponseMapper.Map);
    }

    [HttpGet("configuration/income-tax-credits")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<
        IncomeTaxCreditResponse>>> GetIncomeTaxCredits(
            [FromQuery] int taxYear,
            [FromQuery] string? periodicity,
            [FromQuery] DateTime? asOfDate,
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        PayrollItemsResult<IncomeTaxCreditInfo> result =
            await _payrollConfigurationService
                .GetIncomeTaxCreditsAsync(
                    new IncomeTaxConfigurationQuery(
                        TaxYear: taxYear,
                        Periodicity: periodicity,
                        AsOfDate: asOfDate,
                        IsActive: isActive),
                    cancellationToken);

        return ToActionResult(
            result,
            PayrollResponseMapper.Map);
    }

    [HttpGet("configuration/work-shift-types")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<
        WorkShiftTypeResponse>>> GetWorkShiftTypes(
            [FromQuery] DateTime? asOfDate,
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        PayrollItemsResult<WorkShiftTypeInfo> result =
            await _payrollConfigurationService
                .GetWorkShiftTypesAsync(
                    new EffectivePayrollConfigurationQuery(
                        AsOfDate: asOfDate,
                        IsActive: isActive),
                    cancellationToken);

        return ToActionResult(
            result,
            PayrollResponseMapper.Map);
    }

    [HttpGet("configuration/overtime-rules")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<
        OvertimeRuleResponse>>> GetOvertimeRules(
            [FromQuery] DateTime? asOfDate,
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        PayrollItemsResult<OvertimeRuleInfo> result =
            await _payrollConfigurationService
                .GetOvertimeRulesAsync(
                    new EffectivePayrollConfigurationQuery(
                        AsOfDate: asOfDate,
                        IsActive: isActive),
                    cancellationToken);

        return ToActionResult(
            result,
            PayrollResponseMapper.Map);
    }

    [HttpGet("configuration/disability-types")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<
        DisabilityTypeResponse>>> GetDisabilityTypes(
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        PayrollItemsResult<DisabilityTypeInfo> result =
            await _payrollConfigurationService
                .GetDisabilityTypesAsync(
                    new ActivePayrollConfigurationQuery(
                        IsActive: isActive),
                    cancellationToken);

        return ToActionResult(
            result,
            PayrollResponseMapper.Map);
    }

    [HttpGet("configuration/aguinaldo-rules")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<
        AguinaldoRuleResponse>>> GetAguinaldoRules(
            [FromQuery] DateTime? asOfDate,
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        PayrollItemsResult<AguinaldoRuleInfo> result =
            await _payrollConfigurationService
                .GetAguinaldoRulesAsync(
                    new EffectivePayrollConfigurationQuery(
                        AsOfDate: asOfDate,
                        IsActive: isActive),
                    cancellationToken);

        return ToActionResult(
            result,
            PayrollResponseMapper.Map);
    }

    [HttpPost("work-schedules")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<EmployeeWorkScheduleResponse>>
        SetEmployeeWorkSchedule(
            [FromBody] SetEmployeeWorkScheduleRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        if (!this.TryPreparePayrollMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeWorkScheduleResult result =
            await _setEmployeeWorkScheduleService.SetAsync(
                new SetEmployeeWorkScheduleCommand(
                    request.EmployeeId ?? 0,
                    request.WorkShiftTypeId ?? 0,
                    request.EffectiveFromDate,
                    actorUserId,
                    requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            PayrollResponseMapper.Map(
                result.EmployeeWorkSchedule!));
    }

    [HttpPost("attendance-records")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<AttendanceRecordResponse>>
        SaveAttendanceRecord(
            [FromBody] SaveAttendanceRecordRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        byte[]? expectedRowVersion = null;

        if (!string.IsNullOrWhiteSpace(
                request.ExpectedRowVersion))
        {
            if (!PayrollControllerExtensions
                    .TryParsePayrollRowVersion(
                        request.ExpectedRowVersion,
                        out byte[] parsedRowVersion))
            {
                return BadRequest(
                    this.CreateInvalidPayrollRowVersionProblem(
                        correlationId));
            }

            expectedRowVersion = parsedRowVersion;
        }

        if (!this.TryPreparePayrollMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        AttendanceRecordResult result =
            await _saveAttendanceRecordService.SaveAsync(
                new SaveAttendanceRecordCommand(
                    request.EmployeeId ?? 0,
                    request.AttendanceDate,
                    request.AttendanceStatus,
                    request.ExpectedHours,
                    request.WorkedHours,
                    request.PaidHours,
                    request.UnpaidHours,
                    request.WorkShiftTypeId,
                    request.IsPaidHoliday,
                    request.IsApproved,
                    request.Notes,
                    expectedRowVersion,
                    actorUserId,
                    requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            PayrollResponseMapper.Map(
                result.AttendanceRecord!));
    }

    [HttpPost("overtime-records")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<OvertimeRecordResponse>>
        CreateOvertimeRecord(
            [FromBody] CreateOvertimeRecordRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        if (!this.TryPreparePayrollMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        OvertimeRecordResult result =
            await _createOvertimeRecordService.CreateAsync(
                new CreateOvertimeRecordCommand(
                    request.EmployeeId ?? 0,
                    request.OvertimeRuleId ?? 0,
                    request.OvertimeDate,
                    request.Hours,
                    request.AttendanceRecordId,
                    request.Notes,
                    actorUserId,
                    requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            PayrollResponseMapper.Map(
                result.OvertimeRecord!));
    }

    [HttpPatch("overtime-records/{overtimeRecordId:int}/response")]
    [Authorize(
        Policy =
            AuthorizationPolicyNames
                .PayrollAdministrationMutation)]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<OvertimeRecordResponse>>
        RespondOvertimeRecord(
            int overtimeRecordId,
            [FromBody] RespondOvertimeRecordRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        if (!PayrollControllerExtensions.TryParsePayrollRowVersion(
                request.ExpectedRowVersion,
                out byte[] expectedRowVersion))
        {
            return BadRequest(
                this.CreateInvalidPayrollRowVersionProblem(
                    correlationId));
        }

        if (!this.TryPreparePayrollMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        OvertimeRecordResult result =
            await _respondOvertimeRecordService.RespondAsync(
                new RespondOvertimeRecordCommand(
                    overtimeRecordId,
                    request.IsApproved,
                    request.RejectionReason,
                    expectedRowVersion,
                    actorUserId,
                    requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            PayrollResponseMapper.Map(
                result.OvertimeRecord!));
    }

    [HttpPatch("overtime-records/{overtimeRecordId:int}/cancel")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<OvertimeRecordResponse>>
        CancelOvertimeRecord(
            int overtimeRecordId,
            [FromBody] CancelOvertimeRecordRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        if (!PayrollControllerExtensions.TryParsePayrollRowVersion(
                request.ExpectedRowVersion,
                out byte[] expectedRowVersion))
        {
            return BadRequest(
                this.CreateInvalidPayrollRowVersionProblem(
                    correlationId));
        }

        if (!this.TryPreparePayrollMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        OvertimeRecordResult result =
            await _cancelOvertimeRecordService.CancelAsync(
                new CancelOvertimeRecordCommand(
                    overtimeRecordId,
                    expectedRowVersion,
                    actorUserId,
                    requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            PayrollResponseMapper.Map(
                result.OvertimeRecord!));
    }

    [HttpPost("employee-disabilities")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<EmployeeDisabilityResponse>>
        CreateEmployeeDisability(
            [FromBody] CreateEmployeeDisabilityRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        if (!this.TryPreparePayrollMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeDisabilityResult result =
            await _createEmployeeDisabilityService.CreateAsync(
                new CreateEmployeeDisabilityCommand(
                    request.EmployeeId ?? 0,
                    request.DisabilityTypeId ?? 0,
                    request.IssuerInstitution,
                    request.StartDate,
                    request.EndDate,
                    request.ReferenceNumber,
                    request.EmployerPaidAmount,
                    request.SubsidyAmount,
                    request.Notes,
                    actorUserId,
                    requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            PayrollResponseMapper.Map(
                result.EmployeeDisability!));
    }

    [HttpPatch(
        "employee-disabilities/{employeeDisabilityId:int}/approval")]
    [Authorize(
        Policy =
            AuthorizationPolicyNames
                .PayrollAdministrationMutation)]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<EmployeeDisabilityResponse>>
        ApproveEmployeeDisability(
            int employeeDisabilityId,
            [FromBody] ApproveEmployeeDisabilityRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        if (!PayrollControllerExtensions.TryParsePayrollRowVersion(
                request.ExpectedRowVersion,
                out byte[] expectedRowVersion))
        {
            return BadRequest(
                this.CreateInvalidPayrollRowVersionProblem(
                    correlationId));
        }

        if (!this.TryPreparePayrollMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeDisabilityResult result =
            await _approveEmployeeDisabilityService.ApproveAsync(
                new ApproveEmployeeDisabilityCommand(
                    employeeDisabilityId,
                    expectedRowVersion,
                    actorUserId,
                    requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            PayrollResponseMapper.Map(
                result.EmployeeDisability!));
    }

    [HttpPatch(
        "employee-disabilities/{employeeDisabilityId:int}/cancel")]
    [Authorize(
        Policy =
            AuthorizationPolicyNames
                .PayrollAdministrationMutation)]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<EmployeeDisabilityResponse>>
        CancelEmployeeDisability(
            int employeeDisabilityId,
            [FromBody] CancelEmployeeDisabilityRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        if (!PayrollControllerExtensions.TryParsePayrollRowVersion(
                request.ExpectedRowVersion,
                out byte[] expectedRowVersion))
        {
            return BadRequest(
                this.CreateInvalidPayrollRowVersionProblem(
                    correlationId));
        }

        if (!this.TryPreparePayrollMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeDisabilityResult result =
            await _cancelEmployeeDisabilityService.CancelAsync(
                new CancelEmployeeDisabilityCommand(
                    employeeDisabilityId,
                    request.CancellationReason,
                    expectedRowVersion,
                    actorUserId,
                    requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            PayrollResponseMapper.Map(
                result.EmployeeDisability!));
    }

    private ActionResult<IReadOnlyList<TResponse>> ToActionResult<
        TInfo,
        TResponse>(
        PayrollItemsResult<TInfo> result,
        Func<TInfo, TResponse> mapper)
    {
        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId: null);
        }

        return Ok(
            result.Items
                .Select(mapper)
                .ToList());
    }
}
