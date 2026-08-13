using LithoManager.Api.Authorization;
using LithoManager.Api.Contracts.LeaveManagement;
using LithoManager.Api.Extensions;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.LeaveManagement;
using LithoManager.Application.Features.LeaveManagement
    .CancelLeaveRequest;
using LithoManager.Application.Features.LeaveManagement
    .CreateLeaveRequest;
using LithoManager.Application.Features.LeaveManagement
    .GetLeaveRequestById;
using LithoManager.Application.Features.LeaveManagement
    .GetLeaveRequests;
using LithoManager.Application.Features.LeaveManagement
    .GetMyLeaveRequests;
using LithoManager.Application.Features.LeaveManagement
    .RespondLeaveRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithoManager.Api.Controllers.LeaveManagement;

[ApiController]
[Route("api/leave-management/requests")]
[Authorize]
public sealed class LeaveRequestsController : ControllerBase
{
    private readonly IGetMyLeaveRequestsService
        _getMyLeaveRequestsService;

    private readonly IGetLeaveRequestsService
        _getLeaveRequestsService;

    private readonly IGetLeaveRequestByIdService
        _getLeaveRequestByIdService;

    private readonly ICreateLeaveRequestService
        _createLeaveRequestService;

    private readonly ICancelLeaveRequestService
        _cancelLeaveRequestService;

    private readonly IRespondLeaveRequestService
        _respondLeaveRequestService;

    public LeaveRequestsController(
        IGetMyLeaveRequestsService getMyLeaveRequestsService,
        IGetLeaveRequestsService getLeaveRequestsService,
        IGetLeaveRequestByIdService getLeaveRequestByIdService,
        ICreateLeaveRequestService createLeaveRequestService,
        ICancelLeaveRequestService cancelLeaveRequestService,
        IRespondLeaveRequestService respondLeaveRequestService)
    {
        ArgumentNullException.ThrowIfNull(
            getMyLeaveRequestsService);

        ArgumentNullException.ThrowIfNull(
            getLeaveRequestsService);

        ArgumentNullException.ThrowIfNull(
            getLeaveRequestByIdService);

        ArgumentNullException.ThrowIfNull(
            createLeaveRequestService);

        ArgumentNullException.ThrowIfNull(
            cancelLeaveRequestService);

        ArgumentNullException.ThrowIfNull(
            respondLeaveRequestService);

        _getMyLeaveRequestsService =
            getMyLeaveRequestsService;

        _getLeaveRequestsService =
            getLeaveRequestsService;

        _getLeaveRequestByIdService =
            getLeaveRequestByIdService;

        _createLeaveRequestService =
            createLeaveRequestService;

        _cancelLeaveRequestService =
            cancelLeaveRequestService;

        _respondLeaveRequestService =
            respondLeaveRequestService;
    }

    [HttpGet("my")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(IReadOnlyList<LeaveRequestResponse>),
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
    public async Task<ActionResult<IReadOnlyList<
        LeaveRequestResponse>>> GetMyLeaveRequests(
            [FromQuery] string? statusCode,
            [FromQuery] DateTime? startDateFrom,
            [FromQuery] DateTime? startDateTo,
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

        LeaveRequestsResult result =
            await _getMyLeaveRequestsService.GetAsync(
                new GetMyLeaveRequestsQuery(
                    ActorUserId:
                        actorUserId,
                    LeaveRequestStatusCode:
                        statusCode,
                    StartDateFrom:
                        startDateFrom,
                    StartDateTo:
                        startDateTo),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateLeaveManagementFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            result.LeaveRequests
                .Select(LeaveManagementResponseMapper.Map)
                .ToList());
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(LeaveRequestResponse),
        StatusCodes.Status201Created)]
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
    public async Task<ActionResult<LeaveRequestResponse>>
        CreateLeaveRequest(
            [FromBody] CreateLeaveRequestRequest request,
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

        LeaveRequestResult result =
            await _createLeaveRequestService.CreateAsync(
                new CreateLeaveRequestCommand(
                    StartDate:
                        request.StartDate,
                    EndDate:
                        request.EndDate,
                    ActorUserId:
                        actorUserId,
                    LeaveTypeCode:
                        request.LeaveTypeCode,
                    RequestContext:
                        requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateLeaveManagementFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        LeaveRequestResponse response =
            LeaveManagementResponseMapper.Map(
                result.LeaveRequest!);

        return CreatedAtAction(
            nameof(GetLeaveRequestById),
            new
            {
                leaveRequestId = response.LeaveRequestId
            },
            response);
    }

    [HttpGet]
    [Authorize(
        Policy =
            AuthorizationPolicyNames
                .LeaveManagementAdministration)]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(IReadOnlyList<LeaveRequestResponse>),
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
    public async Task<ActionResult<IReadOnlyList<
        LeaveRequestResponse>>> GetLeaveRequests(
            [FromQuery] string? statusCode,
            [FromQuery] int? employeeId,
            [FromQuery] int? departmentId,
            [FromQuery] DateTime? startDateFrom,
            [FromQuery] DateTime? startDateTo,
            [FromQuery] string? searchTerm,
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

        LeaveRequestsResult result =
            await _getLeaveRequestsService.GetAsync(
                new GetLeaveRequestsQuery(
                    ActorUserId:
                        actorUserId,
                    LeaveRequestStatusCode:
                        statusCode,
                    EmployeeId:
                        employeeId,
                    DepartmentId:
                        departmentId,
                    StartDateFrom:
                        startDateFrom,
                    StartDateTo:
                        startDateTo,
                    SearchTerm:
                        searchTerm),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateLeaveManagementFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            result.LeaveRequests
                .Select(LeaveManagementResponseMapper.Map)
                .ToList());
    }

    [HttpGet("{leaveRequestId:int}")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(LeaveRequestResponse),
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
    public async Task<ActionResult<LeaveRequestResponse>>
        GetLeaveRequestById(
            int leaveRequestId,
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

        LeaveRequestResult result =
            await _getLeaveRequestByIdService.GetAsync(
                leaveRequestId,
                actorUserId,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateLeaveManagementFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            LeaveManagementResponseMapper.Map(
                result.LeaveRequest!));
    }

    [HttpPatch("{leaveRequestId:int}/cancel")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(LeaveRequestResponse),
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
    public async Task<ActionResult<LeaveRequestResponse>>
        CancelLeaveRequest(
            int leaveRequestId,
            [FromBody] CancelLeaveRequestRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!LeaveManagementControllerExtensions
                .TryParseRowVersion(
                    request.ExpectedRowVersion,
                    out byte[] expectedRowVersion))
        {
            return BadRequest(
                this.CreateInvalidRowVersionProblem(
                    correlationId));
        }

        if (!this.TryPrepareLeaveManagementMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        LeaveRequestResult result =
            await _cancelLeaveRequestService.CancelAsync(
                new CancelLeaveRequestCommand(
                    LeaveRequestId:
                        leaveRequestId,
                    ExpectedRowVersion:
                        expectedRowVersion,
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
                result.LeaveRequest!));
    }

    [HttpPatch("{leaveRequestId:int}/response")]
    [Authorize(
        Policy =
            AuthorizationPolicyNames
                .LeaveManagementAdministrationMutation)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(LeaveRequestResponse),
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
    public async Task<ActionResult<LeaveRequestResponse>>
        RespondLeaveRequest(
            int leaveRequestId,
            [FromBody] RespondLeaveRequestRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!LeaveManagementControllerExtensions
                .TryParseRowVersion(
                    request.ExpectedRowVersion,
                    out byte[] expectedRowVersion))
        {
            return BadRequest(
                this.CreateInvalidRowVersionProblem(
                    correlationId));
        }

        if (!this.TryPrepareLeaveManagementMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        LeaveRequestResult result =
            await _respondLeaveRequestService.RespondAsync(
                new RespondLeaveRequestCommand(
                    LeaveRequestId:
                        leaveRequestId,
                    IsApproved:
                        request.IsApproved,
                    ExpectedRowVersion:
                        expectedRowVersion,
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
                result.LeaveRequest!));
    }
}
