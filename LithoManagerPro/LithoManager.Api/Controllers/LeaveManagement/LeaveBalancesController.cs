using LithoManager.Api.Authorization;
using LithoManager.Api.Contracts.LeaveManagement;
using LithoManager.Api.Extensions;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.LeaveManagement;
using LithoManager.Application.Features.LeaveManagement
    .AdjustEmployeeLeaveBalance;
using LithoManager.Application.Features.LeaveManagement
    .GetEmployeeLeaveBalance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithoManager.Api.Controllers.LeaveManagement;

[ApiController]
[Route("api/leave-management/balances")]
[Authorize]
public sealed class LeaveBalancesController : ControllerBase
{
    private readonly IGetEmployeeLeaveBalanceService
        _getEmployeeLeaveBalanceService;

    private readonly IAdjustEmployeeLeaveBalanceService
        _adjustEmployeeLeaveBalanceService;

    public LeaveBalancesController(
        IGetEmployeeLeaveBalanceService
            getEmployeeLeaveBalanceService,
        IAdjustEmployeeLeaveBalanceService
            adjustEmployeeLeaveBalanceService)
    {
        ArgumentNullException.ThrowIfNull(
            getEmployeeLeaveBalanceService);

        ArgumentNullException.ThrowIfNull(
            adjustEmployeeLeaveBalanceService);

        _getEmployeeLeaveBalanceService =
            getEmployeeLeaveBalanceService;

        _adjustEmployeeLeaveBalanceService =
            adjustEmployeeLeaveBalanceService;
    }

    [HttpGet("me")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(EmployeeLeaveBalanceResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeLeaveBalanceResponse>>
        GetMyLeaveBalance(
            [FromQuery] string? leaveTypeCode,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!this.TryPrepareLeaveManagementMutationContext(
                correlationId,
                out int actorUserId,
                out _,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeLeaveBalanceResult result =
            await _getEmployeeLeaveBalanceService.GetAsync(
                new GetEmployeeLeaveBalanceQuery(
                    EmployeeId:
                        null,
                    LeaveTypeCode:
                        leaveTypeCode,
                    ActorUserId:
                        actorUserId),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateLeaveManagementFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            LeaveManagementResponseMapper.Map(
                result.LeaveBalance!));
    }

    [HttpGet("employees/{employeeId:int}")]
    [Authorize(
        Policy =
            AuthorizationPolicyNames
                .LeaveManagementAdministration)]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(EmployeeLeaveBalanceResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeLeaveBalanceResponse>>
        GetEmployeeLeaveBalance(
            int employeeId,
            [FromQuery] string? leaveTypeCode,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!this.TryPrepareLeaveManagementMutationContext(
                correlationId,
                out int actorUserId,
                out _,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeLeaveBalanceResult result =
            await _getEmployeeLeaveBalanceService.GetAsync(
                new GetEmployeeLeaveBalanceQuery(
                    EmployeeId:
                        employeeId,
                    LeaveTypeCode:
                        leaveTypeCode,
                    ActorUserId:
                        actorUserId),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateLeaveManagementFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            LeaveManagementResponseMapper.Map(
                result.LeaveBalance!));
    }

    [HttpPatch("employees/{employeeId:int}/adjustments")]
    [Authorize(
        Policy =
            AuthorizationPolicyNames
                .LeaveManagementAdministrationMutation)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(EmployeeLeaveBalanceResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeLeaveBalanceResponse>>
        AdjustEmployeeLeaveBalance(
            int employeeId,
            [FromBody] AdjustEmployeeLeaveBalanceRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!this.TryPrepareLeaveManagementMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeLeaveBalanceResult result =
            await _adjustEmployeeLeaveBalanceService.AdjustAsync(
                new AdjustEmployeeLeaveBalanceCommand(
                    EmployeeId:
                        employeeId,
                    LeaveTypeCode:
                        request.LeaveTypeCode,
                    AdjustedDaysDelta:
                        request.AdjustedDaysDelta,
                    ActorUserId:
                        actorUserId,
                    RequestContext:
                        requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateLeaveManagementFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            LeaveManagementResponseMapper.Map(
                result.LeaveBalance!));
    }
}
