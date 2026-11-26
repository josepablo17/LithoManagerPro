using LithoManager.Api.Authorization;
using LithoManager.Api.Contracts.Payroll;
using LithoManager.Api.Extensions;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Payroll;
using LithoManager.Application.Features.Payroll.GetEmployeeDisabilities;
using LithoManager.Application.Features.Payroll.GetEmployeeWorkSchedules;
using LithoManager.Application.Features.Payroll.GetOvertimeRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithoManager.Api.Controllers;

/// <summary>
/// Read endpoints for the payroll operations (overtime, disabilities and
/// work schedules) that PayrollController only exposed for writing.
/// </summary>
[ApiController]
[Route("api/payroll")]
[Authorize(
    Policy =
        AuthorizationPolicyNames.PayrollAdministration)]
public sealed class PayrollOperationsQueryController : ControllerBase
{
    private readonly IGetOvertimeRecordsService _getOvertimeRecordsService;
    private readonly IGetEmployeeDisabilitiesService _getEmployeeDisabilitiesService;
    private readonly IGetEmployeeWorkSchedulesService _getEmployeeWorkSchedulesService;

    public PayrollOperationsQueryController(
        IGetOvertimeRecordsService getOvertimeRecordsService,
        IGetEmployeeDisabilitiesService getEmployeeDisabilitiesService,
        IGetEmployeeWorkSchedulesService getEmployeeWorkSchedulesService)
    {
        ArgumentNullException.ThrowIfNull(getOvertimeRecordsService);
        ArgumentNullException.ThrowIfNull(getEmployeeDisabilitiesService);
        ArgumentNullException.ThrowIfNull(getEmployeeWorkSchedulesService);

        _getOvertimeRecordsService = getOvertimeRecordsService;
        _getEmployeeDisabilitiesService = getEmployeeDisabilitiesService;
        _getEmployeeWorkSchedulesService = getEmployeeWorkSchedulesService;
    }

    [HttpGet("overtime-records")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<OvertimeRecordResponse>>>
        GetOvertimeRecords(
            [FromQuery] int? employeeId,
            [FromQuery] int? departmentId,
            [FromQuery] int? overtimeRuleId,
            [FromQuery] string? approvalStatus,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? searchTerm,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        if (!this.TryPreparePayrollMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? _,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        PayrollItemsResult<OvertimeRecordInfo> result =
            await _getOvertimeRecordsService.GetAsync(
                new GetOvertimeRecordsQuery(
                    actorUserId,
                    employeeId,
                    departmentId,
                    overtimeRuleId,
                    approvalStatus,
                    dateFrom,
                    dateTo,
                    searchTerm),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(result.Items.Select(PayrollResponseMapper.Map).ToList());
    }

    [HttpGet("employee-disabilities")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<EmployeeDisabilityResponse>>>
        GetEmployeeDisabilities(
            [FromQuery] int? employeeId,
            [FromQuery] int? departmentId,
            [FromQuery] int? disabilityTypeId,
            [FromQuery] string? disabilityStatus,
            [FromQuery] string? issuerInstitution,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] string? searchTerm,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        if (!this.TryPreparePayrollMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? _,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        PayrollItemsResult<EmployeeDisabilityInfo> result =
            await _getEmployeeDisabilitiesService.GetAsync(
                new GetEmployeeDisabilitiesQuery(
                    actorUserId,
                    employeeId,
                    departmentId,
                    disabilityTypeId,
                    disabilityStatus,
                    issuerInstitution,
                    dateFrom,
                    dateTo,
                    searchTerm),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(result.Items.Select(PayrollResponseMapper.Map).ToList());
    }

    [HttpGet("work-schedules")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<EmployeeWorkScheduleResponse>>>
        GetEmployeeWorkSchedules(
            [FromQuery] int? employeeId,
            [FromQuery] bool includeHistory,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        PayrollItemsResult<EmployeeWorkScheduleInfo> result =
            await _getEmployeeWorkSchedulesService.GetAsync(
                new GetEmployeeWorkSchedulesQuery(
                    employeeId,
                    includeHistory),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(result.Items.Select(PayrollResponseMapper.Map).ToList());
    }
}
