using LithoManager.Api.Authorization;
using LithoManager.Api.Contracts.Payroll;
using LithoManager.Api.Extensions;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Payroll;
using LithoManager.Application.Features.Payroll.Runs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithoManager.Api.Controllers;

/// <summary>
/// Payroll runs: Open -> Calculated (can be recalculated) -> Closed.
/// Reads are open to HR staff; creating, calculating and closing require
/// an HR administrator.
/// </summary>
[ApiController]
[Route("api/payroll/runs")]
[Authorize(
    Policy =
        AuthorizationPolicyNames.PayrollAdministration)]
public sealed class PayrollRunsController : ControllerBase
{
    private readonly IPayrollRunService _payrollRunService;

    public PayrollRunsController(IPayrollRunService payrollRunService)
    {
        ArgumentNullException.ThrowIfNull(payrollRunService);

        _payrollRunService = payrollRunService;
    }

    [HttpGet]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<PayrollRunResponse>>>
        GetPayrollRuns(
            [FromQuery] int? periodYear,
            [FromQuery] string? runType,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        PayrollItemsResult<PayrollRunData> result =
            await _payrollRunService.GetRunsAsync(
                periodYear,
                runType,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            result.Items
                .Select(PayrollRunResponseMapper.Map)
                .ToList());
    }

    [HttpGet("{payrollRunId:int}")]
    [Produces("application/json")]
    public async Task<ActionResult<PayrollRunDetailResponse>>
        GetPayrollRun(
            int payrollRunId,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        PayrollRunResult result =
            await _payrollRunService.GetRunAsync(
                payrollRunId,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(PayrollRunResponseMapper.MapDetail(result));
    }

    [HttpGet("{payrollRunId:int}/employees/{employeeId:int}/lines")]
    [Produces("application/json")]
    public async Task<ActionResult<IReadOnlyList<PayrollRunLineResponse>>>
        GetPayrollRunEmployeeLines(
            int payrollRunId,
            int employeeId,
            CancellationToken cancellationToken)
    {
        Guid correlationId = this.PrepareNoStoreResponse();

        PayrollItemsResult<PayrollRunLineData> result =
            await _payrollRunService.GetEmployeeLinesAsync(
                payrollRunId,
                employeeId,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            result.Items
                .Select(PayrollRunResponseMapper.Map)
                .ToList());
    }

    [HttpPost]
    [Authorize(
        Policy =
            AuthorizationPolicyNames.PayrollAdministrationMutation)]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<PayrollRunDetailResponse>>
        CreatePayrollRun(
            [FromBody] CreatePayrollRunRequest request,
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

        PayrollRunResult result =
            await _payrollRunService.CreateAsync(
                new CreatePayrollRunCommand(
                    request.RunType,
                    request.PeriodYear ?? 0,
                    request.PeriodMonth,
                    request.PaymentDate,
                    actorUserId,
                    requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreatePayrollFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return CreatedAtAction(
            nameof(GetPayrollRun),
            new { payrollRunId = result.PayrollRun!.PayrollRunId },
            PayrollRunResponseMapper.MapDetail(result));
    }

    [HttpPost("{payrollRunId:int}/calculate")]
    [Authorize(
        Policy =
            AuthorizationPolicyNames.PayrollAdministrationMutation)]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<PayrollRunDetailResponse>>
        CalculatePayrollRun(
            int payrollRunId,
            [FromBody] PayrollRunMutationRequest request,
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

        PayrollRunResult result =
            await _payrollRunService.CalculateAsync(
                new CalculatePayrollRunCommand(
                    payrollRunId,
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

        return Ok(PayrollRunResponseMapper.MapDetail(result));
    }

    [HttpPatch("{payrollRunId:int}/close")]
    [Authorize(
        Policy =
            AuthorizationPolicyNames.PayrollAdministrationMutation)]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<ActionResult<PayrollRunDetailResponse>>
        ClosePayrollRun(
            int payrollRunId,
            [FromBody] PayrollRunMutationRequest request,
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

        PayrollRunResult result =
            await _payrollRunService.CloseAsync(
                new ClosePayrollRunCommand(
                    payrollRunId,
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

        return Ok(PayrollRunResponseMapper.MapDetail(result));
    }
}
