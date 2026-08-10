using LithoManager.Api.Authorization;
using LithoManager.Api.Contracts.HumanResources
    .Departments;
using LithoManager.Api.Extensions;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Departments;
using LithoManager.Application.Features
    .HumanResources.Departments.CreateDepartment;
using LithoManager.Application.Features
    .HumanResources.Departments.GetDepartmentById;
using LithoManager.Application.Features
    .HumanResources.Departments.GetDepartments;
using LithoManager.Application.Features
    .HumanResources.Departments.SetDepartmentStatus;
using LithoManager.Application.Features
    .HumanResources.Departments.UpdateDepartment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithoManager.Api.Controllers;

[ApiController]
[Route("api/human-resources/departments")]
[Authorize(
    Policy =
        AuthorizationPolicyNames
            .HumanResourcesDepartments)]
public sealed class DepartmentsController : ControllerBase
{
    private readonly ICreateDepartmentService
        _createDepartmentService;

    private readonly IGetDepartmentByIdService
        _getDepartmentByIdService;

    private readonly IGetDepartmentsService
        _getDepartmentsService;

    private readonly IUpdateDepartmentService
        _updateDepartmentService;

    private readonly ISetDepartmentStatusService
        _setDepartmentStatusService;

    public DepartmentsController(
        ICreateDepartmentService createDepartmentService,
        IGetDepartmentByIdService getDepartmentByIdService,
        IGetDepartmentsService getDepartmentsService,
        IUpdateDepartmentService updateDepartmentService,
        ISetDepartmentStatusService setDepartmentStatusService)
    {
        ArgumentNullException.ThrowIfNull(
            createDepartmentService);

        ArgumentNullException.ThrowIfNull(
            getDepartmentByIdService);

        ArgumentNullException.ThrowIfNull(
            getDepartmentsService);

        ArgumentNullException.ThrowIfNull(
            updateDepartmentService);

        ArgumentNullException.ThrowIfNull(
            setDepartmentStatusService);

        _createDepartmentService =
            createDepartmentService;

        _getDepartmentByIdService =
            getDepartmentByIdService;

        _getDepartmentsService =
            getDepartmentsService;

        _updateDepartmentService =
            updateDepartmentService;

        _setDepartmentStatusService =
            setDepartmentStatusService;
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(DepartmentResponse),
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
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DepartmentResponse>>
        CreateDepartment(
            [FromBody] CreateDepartmentRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!TryPrepareMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        CreateDepartmentCommand command = new(
            DepartmentCode:
                request.DepartmentCode,
            Name:
                request.Name,
            Description:
                request.Description,
            ActorUserId:
                actorUserId,
            RequestContext:
                requestContext!);

        DepartmentResult result =
            await _createDepartmentService.CreateAsync(
                command,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateFailureResponse(
                result,
                correlationId);
        }

        DepartmentResponse response =
            MapDepartment(result.Department!);

        return CreatedAtAction(
            nameof(GetDepartmentById),
            new
            {
                departmentId = response.DepartmentId
            },
            response);
    }

    [HttpGet("{departmentId:int}")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(DepartmentResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepartmentResponse>>
        GetDepartmentById(
            int departmentId,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        DepartmentResult result =
            await _getDepartmentByIdService.GetAsync(
                departmentId,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateFailureResponse(
                result,
                correlationId: null);
        }

        return Ok(
            MapDepartment(result.Department!));
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(IReadOnlyList<DepartmentResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<
        DepartmentResponse>>> GetDepartments(
            [FromQuery] string? searchTerm,
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        DepartmentsResult result =
            await _getDepartmentsService.GetAsync(
                new GetDepartmentsQuery(
                    SearchTerm:
                        searchTerm,
                    IsActive:
                        isActive),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateFailureResponse(
                result,
                correlationId: null);
        }

        return Ok(
            result.Departments
                .Select(MapDepartment)
                .ToList());
    }

    [HttpPut("{departmentId:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(DepartmentResponse),
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
    public async Task<ActionResult<DepartmentResponse>>
        UpdateDepartment(
            int departmentId,
            [FromBody] UpdateDepartmentRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!TryParseRowVersion(
                request.ExpectedRowVersion,
                out byte[] expectedRowVersion))
        {
            return BadRequest(
                CreateInvalidRowVersionProblem(
                    correlationId));
        }

        if (!TryPrepareMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        UpdateDepartmentCommand command = new(
            DepartmentId:
                departmentId,
            DepartmentCode:
                request.DepartmentCode,
            Name:
                request.Name,
            Description:
                request.Description,
            ExpectedRowVersion:
                expectedRowVersion,
            ActorUserId:
                actorUserId,
            RequestContext:
                requestContext!);

        DepartmentResult result =
            await _updateDepartmentService.UpdateAsync(
                command,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateFailureResponse(
                result,
                correlationId);
        }

        return Ok(
            MapDepartment(result.Department!));
    }

    [HttpPatch("{departmentId:int}/status")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(DepartmentResponse),
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
    public async Task<ActionResult<DepartmentResponse>>
        SetDepartmentStatus(
            int departmentId,
            [FromBody] SetDepartmentStatusRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!TryParseRowVersion(
                request.ExpectedRowVersion,
                out byte[] expectedRowVersion))
        {
            return BadRequest(
                CreateInvalidRowVersionProblem(
                    correlationId));
        }

        if (!TryPrepareMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        SetDepartmentStatusCommand command = new(
            DepartmentId:
                departmentId,
            IsActive:
                request.IsActive,
            ExpectedRowVersion:
                expectedRowVersion,
            ActorUserId:
                actorUserId,
            RequestContext:
                requestContext!);

        DepartmentResult result =
            await _setDepartmentStatusService.SetAsync(
                command,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateFailureResponse(
                result,
                correlationId);
        }

        return Ok(
            MapDepartment(result.Department!));
    }

    private bool TryPrepareMutationContext(
        Guid correlationId,
        out int actorUserId,
        out AuthenticationRequestContext? requestContext,
        out ActionResult? unauthorizedResult)
    {
        requestContext = null;
        unauthorizedResult = null;

        if (!this.TryResolveAuthenticatedUserId(
                out actorUserId))
        {
            unauthorizedResult =
                Unauthorized(
                    this.CreateProblemDetails(
                        statusCode:
                            StatusCodes
                                .Status401Unauthorized,
                        title:
                            "Token inválido",
                        detail:
                            "No fue posible identificar al usuario.",
                        errorCode:
                            "invalid_token",
                        correlationId:
                            correlationId));

            return false;
        }

        requestContext =
            this.CreateAuthenticationRequestContext(
                correlationId);

        return true;
    }

    private ObjectResult CreateFailureResponse(
        DepartmentResult result,
        Guid? correlationId)
    {
        return CreateFailureResponse(
            result.ErrorCode,
            correlationId);
    }

    private ObjectResult CreateFailureResponse(
        DepartmentsResult result,
        Guid? correlationId)
    {
        return CreateFailureResponse(
            result.ErrorCode,
            correlationId);
    }

    private ObjectResult CreateFailureResponse(
        DepartmentErrorCode errorCode,
        Guid? correlationId)
    {
        (
            int statusCode,
            string apiErrorCode,
            string title,
            string detail
        ) error = errorCode switch
        {
            DepartmentErrorCode.InvalidRequest =>
            (
                StatusCodes.Status400BadRequest,
                "invalid_request",
                "Solicitud inválida",
                "Revise los datos enviados."
            ),

            DepartmentErrorCode.DepartmentNotFound =>
            (
                StatusCodes.Status404NotFound,
                "department_not_found",
                "Departamento no encontrado",
                "El departamento solicitado no existe."
            ),

            DepartmentErrorCode.DuplicateDepartmentCode =>
            (
                StatusCodes.Status409Conflict,
                "duplicate_department_code",
                "Código de departamento duplicado",
                "Ya existe un departamento con el mismo código."
            ),

            DepartmentErrorCode.DuplicateDepartmentName =>
            (
                StatusCodes.Status409Conflict,
                "duplicate_department_name",
                "Nombre de departamento duplicado",
                "Ya existe un departamento con el mismo nombre."
            ),

            DepartmentErrorCode.ConcurrencyConflict =>
            (
                StatusCodes.Status409Conflict,
                "concurrency_conflict",
                "El departamento fue modificado",
                "Actualice la información e intente nuevamente."
            ),

            DepartmentErrorCode.AccessNotAvailable =>
            (
                StatusCodes.Status403Forbidden,
                "access_not_available",
                "Acceso no disponible",
                "La cuenta no está habilitada para administrar departamentos."
            ),

            DepartmentErrorCode.DepartmentHasActiveEmployees =>
            (
                StatusCodes.Status409Conflict,
                "department_has_active_employees",
                "Departamento con empleados activos",
                "No se puede desactivar un departamento con empleados activos."
            ),

            _ =>
            (
                StatusCodes.Status500InternalServerError,
                "department_error",
                "Error al procesar el departamento",
                "No fue posible completar la operación solicitada."
            )
        };

        ProblemDetails problemDetails =
            this.CreateProblemDetails(
                statusCode:
                    error.statusCode,
                title:
                    error.title,
                detail:
                    error.detail,
                errorCode:
                    error.apiErrorCode,
                correlationId:
                    correlationId);

        return StatusCode(
            error.statusCode,
            problemDetails);
    }

    private ProblemDetails CreateInvalidRowVersionProblem(
        Guid correlationId)
    {
        return this.CreateProblemDetails(
            statusCode:
                StatusCodes.Status400BadRequest,
            title:
                "RowVersion inválido",
            detail:
                "ExpectedRowVersion debe ser un valor Base64 de 8 bytes.",
            errorCode:
                "invalid_row_version",
            correlationId:
                correlationId);
    }

    private static bool TryParseRowVersion(
        string? rowVersion,
        out byte[] value)
    {
        value = [];

        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            return false;
        }

        try
        {
            value = Convert.FromBase64String(
                rowVersion);

            return value.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static DepartmentResponse MapDepartment(
        DepartmentInfo department)
    {
        return new DepartmentResponse(
            DepartmentId:
                department.DepartmentId,
            DepartmentCode:
                department.DepartmentCode,
            Name:
                department.Name,
            Description:
                department.Description,
            IsActive:
                department.IsActive,
            CreatedAtUtc:
                department.CreatedAtUtc,
            CreatedByUserId:
                department.CreatedByUserId,
            UpdatedAtUtc:
                department.UpdatedAtUtc,
            UpdatedByUserId:
                department.UpdatedByUserId,
            RowVersion:
                Convert.ToBase64String(
                    department.RowVersion));
    }
}
