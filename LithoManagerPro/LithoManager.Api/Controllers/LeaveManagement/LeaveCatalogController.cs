using LithoManager.Api.Contracts.LeaveManagement;
using LithoManager.Api.Extensions;
using LithoManager.Application.Features.LeaveManagement
    .GetLeaveRequestStatuses;
using LithoManager.Application.Features.LeaveManagement
    .GetLeaveTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithoManager.Api.Controllers.LeaveManagement;

[ApiController]
[Route("api/leave-management/catalog")]
[Authorize]
public sealed class LeaveCatalogController : ControllerBase
{
    private readonly IGetLeaveTypesService
        _getLeaveTypesService;

    private readonly IGetLeaveRequestStatusesService
        _getLeaveRequestStatusesService;

    public LeaveCatalogController(
        IGetLeaveTypesService getLeaveTypesService,
        IGetLeaveRequestStatusesService
            getLeaveRequestStatusesService)
    {
        ArgumentNullException.ThrowIfNull(
            getLeaveTypesService);

        ArgumentNullException.ThrowIfNull(
            getLeaveRequestStatusesService);

        _getLeaveTypesService =
            getLeaveTypesService;

        _getLeaveRequestStatusesService =
            getLeaveRequestStatusesService;
    }

    [HttpGet("leave-types")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(IReadOnlyList<LeaveTypeResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<
        LeaveTypeResponse>>> GetLeaveTypes(
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        var result =
            await _getLeaveTypesService.GetAsync(
                new GetLeaveTypesQuery(
                    IsActive:
                        isActive),
                cancellationToken);

        return Ok(
            result.LeaveTypes
                .Select(LeaveManagementResponseMapper.Map)
                .ToList());
    }

    [HttpGet("request-statuses")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(IReadOnlyList<LeaveRequestStatusResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<
        LeaveRequestStatusResponse>>> GetRequestStatuses(
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        var result =
            await _getLeaveRequestStatusesService.GetAsync(
                new GetLeaveRequestStatusesQuery(
                    IsActive:
                        isActive),
                cancellationToken);

        return Ok(
            result.LeaveRequestStatuses
                .Select(LeaveManagementResponseMapper.Map)
                .ToList());
    }
}
