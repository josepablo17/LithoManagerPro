using LithoManager.Api.Authorization;
using LithoManager.Api.Contracts.HumanResources
    .Employees;
using LithoManager.Api.Extensions;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features
    .HumanResources.Employees;
using LithoManager.Application.Features
    .HumanResources.Employees.CreateEmployee;
using LithoManager.Application.Features
    .HumanResources.Employees.GetAssignableEmployeeUsers;
using LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeById;
using LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeIdentificationTypes;
using LithoManager.Application.Features
    .HumanResources.Employees.GetEmployeeSalaryHistory;
using LithoManager.Application.Features
    .HumanResources.Employees.GetEmployees;
using LithoManager.Application.Features
    .HumanResources.Employees.SetEmployeeStatus;
using LithoManager.Application.Features
    .HumanResources.Employees.UpdateEmployee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithoManager.Api.Controllers;

[ApiController]
[Route("api/human-resources/employees")]
[Authorize(
    Policy =
        AuthorizationPolicyNames
            .HumanResourcesEmployees)]
public sealed class EmployeesController : ControllerBase
{
    private readonly ICreateEmployeeService
        _createEmployeeService;

    private readonly IGetAssignableEmployeeUsersService
        _getAssignableEmployeeUsersService;

    private readonly IGetEmployeeByIdService
        _getEmployeeByIdService;

    private readonly IGetEmployeeIdentificationTypesService
        _getEmployeeIdentificationTypesService;

    private readonly IGetEmployeesService
        _getEmployeesService;

    private readonly IGetEmployeeSalaryHistoryService
        _getEmployeeSalaryHistoryService;

    private readonly IUpdateEmployeeService
        _updateEmployeeService;

    private readonly ISetEmployeeStatusService
        _setEmployeeStatusService;

    public EmployeesController(
        ICreateEmployeeService createEmployeeService,
        IGetAssignableEmployeeUsersService
            getAssignableEmployeeUsersService,
        IGetEmployeeByIdService getEmployeeByIdService,
        IGetEmployeeIdentificationTypesService
            getEmployeeIdentificationTypesService,
        IGetEmployeesService getEmployeesService,
        IGetEmployeeSalaryHistoryService
            getEmployeeSalaryHistoryService,
        IUpdateEmployeeService updateEmployeeService,
        ISetEmployeeStatusService setEmployeeStatusService)
    {
        ArgumentNullException.ThrowIfNull(
            createEmployeeService);

        ArgumentNullException.ThrowIfNull(
            getAssignableEmployeeUsersService);

        ArgumentNullException.ThrowIfNull(
            getEmployeeByIdService);

        ArgumentNullException.ThrowIfNull(
            getEmployeeIdentificationTypesService);

        ArgumentNullException.ThrowIfNull(
            getEmployeesService);

        ArgumentNullException.ThrowIfNull(
            getEmployeeSalaryHistoryService);

        ArgumentNullException.ThrowIfNull(
            updateEmployeeService);

        ArgumentNullException.ThrowIfNull(
            setEmployeeStatusService);

        _createEmployeeService =
            createEmployeeService;

        _getAssignableEmployeeUsersService =
            getAssignableEmployeeUsersService;

        _getEmployeeByIdService =
            getEmployeeByIdService;

        _getEmployeeIdentificationTypesService =
            getEmployeeIdentificationTypesService;

        _getEmployeesService =
            getEmployeesService;

        _getEmployeeSalaryHistoryService =
            getEmployeeSalaryHistoryService;

        _updateEmployeeService =
            updateEmployeeService;

        _setEmployeeStatusService =
            setEmployeeStatusService;
    }

    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(EmployeeResponse),
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
    public async Task<ActionResult<EmployeeResponse>>
        CreateEmployee(
            [FromBody] CreateEmployeeRequest request,
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

        CreateEmployeeCommand command = new(
            UserId:
                request.UserId,
            DepartmentId:
                request.DepartmentId ?? 0,
            IdentificationType:
                request.IdentificationType,
            IdentificationNumber:
                request.IdentificationNumber,
            FirstName:
                request.FirstName,
            LastName:
                request.LastName,
            PhoneNumber:
                request.PhoneNumber,
            BirthDate:
                request.BirthDate,
            HireDate:
                request.HireDate,
            TerminationDate:
                request.TerminationDate,
            JobTitle:
                request.JobTitle,
            BaseSalary:
                request.BaseSalary,
            ProfileImagePath:
                request.ProfileImagePath,
            ActorUserId:
                actorUserId,
            RequestContext:
                requestContext!);

        EmployeeResult result =
            await _createEmployeeService.CreateAsync(
                command,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateFailureResponse(
                result,
                correlationId);
        }

        EmployeeResponse response =
            MapEmployee(result.Employee!);

        return CreatedAtAction(
            nameof(GetEmployeeById),
            new
            {
                employeeId = response.EmployeeId
            },
            response);
    }

    [HttpGet("{employeeId:int}")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(EmployeeResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>>
        GetEmployeeById(
            int employeeId,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        EmployeeResult result =
            await _getEmployeeByIdService.GetAsync(
                employeeId,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateFailureResponse(
                result,
                correlationId: null);
        }

        return Ok(
            MapEmployee(result.Employee!));
    }

    [HttpGet("assignable-users")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(IReadOnlyList<
            AssignableEmployeeUserResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<
        AssignableEmployeeUserResponse>>> GetAssignableUsers(
            [FromQuery] int? employeeId,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        AssignableEmployeeUsersResult result =
            await _getAssignableEmployeeUsersService.GetAsync(
                new GetAssignableEmployeeUsersQuery(
                    EmployeeId:
                        employeeId),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateFailureResponse(
                result,
                correlationId: null);
        }

        return Ok(
            result.Users
                .Select(MapAssignableUser)
                .ToList());
    }

    [HttpGet("identification-types")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(IReadOnlyList<
            EmployeeIdentificationTypeResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<
        EmployeeIdentificationTypeResponse>>>
        GetEmployeeIdentificationTypes(
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        EmployeeIdentificationTypesResult result =
            await _getEmployeeIdentificationTypesService
                .GetAsync(
                    new GetEmployeeIdentificationTypesQuery(),
                    cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateFailureResponse(
                result,
                correlationId: null);
        }

        return Ok(
            result.IdentificationTypes
                .Select(MapIdentificationType)
                .ToList());
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(IReadOnlyList<EmployeeResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<
        EmployeeResponse>>> GetEmployees(
            [FromQuery] string? searchTerm,
            [FromQuery] int? departmentId,
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        EmployeesResult result =
            await _getEmployeesService.GetAsync(
                new GetEmployeesQuery(
                    SearchTerm:
                        searchTerm,
                    DepartmentId:
                        departmentId,
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
            result.Employees
                .Select(MapEmployee)
                .ToList());
    }

    [HttpGet("{employeeId:int}/salary-history")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(IReadOnlyList<
            EmployeeSalaryHistoryResponse>),
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
    public async Task<ActionResult<IReadOnlyList<
        EmployeeSalaryHistoryResponse>>>
        GetEmployeeSalaryHistory(
            int employeeId,
            [FromQuery] DateTime? effectiveFromDate,
            [FromQuery] DateTime? effectiveToDate,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!this.TryResolveAuthenticatedUserId(
                out int actorUserId))
        {
            return Unauthorized(
                this.CreateProblemDetails(
                    statusCode:
                        StatusCodes.Status401Unauthorized,
                    title:
                        "Token inválido",
                    detail:
                        "No fue posible identificar al usuario.",
                    errorCode:
                        "invalid_token",
                    correlationId:
                        correlationId));
        }

        EmployeeSalaryHistoryResult result =
            await _getEmployeeSalaryHistoryService.GetAsync(
                new GetEmployeeSalaryHistoryQuery(
                    ActorUserId:
                        actorUserId,
                    EmployeeId:
                        employeeId,
                    EffectiveFromDate:
                        effectiveFromDate,
                    EffectiveToDate:
                        effectiveToDate),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateFailureResponse(
                result,
                correlationId);
        }

        return Ok(
            result.SalaryHistory
                .Select(MapSalaryHistory)
                .ToList());
    }

    [HttpPut("{employeeId:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(EmployeeResponse),
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
    public async Task<ActionResult<EmployeeResponse>>
        UpdateEmployee(
            int employeeId,
            [FromBody] UpdateEmployeeRequest request,
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

        UpdateEmployeeCommand command = new(
            EmployeeId:
                employeeId,
            UserId:
                request.UserId,
            DepartmentId:
                request.DepartmentId ?? 0,
            IdentificationType:
                request.IdentificationType,
            IdentificationNumber:
                request.IdentificationNumber,
            FirstName:
                request.FirstName,
            LastName:
                request.LastName,
            PhoneNumber:
                request.PhoneNumber,
            BirthDate:
                request.BirthDate,
            HireDate:
                request.HireDate,
            TerminationDate:
                request.TerminationDate,
            JobTitle:
                request.JobTitle,
            BaseSalary:
                request.BaseSalary,
            ProfileImagePath:
                request.ProfileImagePath,
            ExpectedRowVersion:
                expectedRowVersion,
            ActorUserId:
                actorUserId,
            RequestContext:
                requestContext!);

        EmployeeResult result =
            await _updateEmployeeService.UpdateAsync(
                command,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateFailureResponse(
                result,
                correlationId);
        }

        return Ok(
            MapEmployee(result.Employee!));
    }

    [HttpPatch("{employeeId:int}/status")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(EmployeeResponse),
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
    public async Task<ActionResult<EmployeeResponse>>
        SetEmployeeStatus(
            int employeeId,
            [FromBody] SetEmployeeStatusRequest request,
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

        SetEmployeeStatusCommand command = new(
            EmployeeId:
                employeeId,
            IsActive:
                request.IsActive,
            ExpectedRowVersion:
                expectedRowVersion,
            ActorUserId:
                actorUserId,
            RequestContext:
                requestContext!);

        EmployeeResult result =
            await _setEmployeeStatusService.SetAsync(
                command,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateFailureResponse(
                result,
                correlationId);
        }

        return Ok(
            MapEmployee(result.Employee!));
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
        EmployeeResult result,
        Guid? correlationId)
    {
        return CreateFailureResponse(
            result.ErrorCode,
            correlationId);
    }

    private ObjectResult CreateFailureResponse(
        AssignableEmployeeUsersResult result,
        Guid? correlationId)
    {
        return CreateFailureResponse(
            result.ErrorCode,
            correlationId);
    }

    private ObjectResult CreateFailureResponse(
        EmployeesResult result,
        Guid? correlationId)
    {
        return CreateFailureResponse(
            result.ErrorCode,
            correlationId);
    }

    private ObjectResult CreateFailureResponse(
        EmployeeIdentificationTypesResult result,
        Guid? correlationId)
    {
        return CreateFailureResponse(
            result.ErrorCode,
            correlationId);
    }

    private ObjectResult CreateFailureResponse(
        EmployeeSalaryHistoryResult result,
        Guid? correlationId)
    {
        return CreateFailureResponse(
            result.ErrorCode,
            correlationId);
    }

    private ObjectResult CreateFailureResponse(
        EmployeeErrorCode errorCode,
        Guid? correlationId)
    {
        (
            int statusCode,
            string apiErrorCode,
            string title,
            string detail
        ) error = errorCode switch
        {
            EmployeeErrorCode.InvalidRequest =>
            (
                StatusCodes.Status400BadRequest,
                "invalid_request",
                "Solicitud inválida",
                "Revise los datos enviados."
            ),

            EmployeeErrorCode.EmployeeNotFound =>
            (
                StatusCodes.Status404NotFound,
                "employee_not_found",
                "Empleado no encontrado",
                "El empleado solicitado no existe."
            ),

            EmployeeErrorCode.DuplicateIdentificationNumber =>
            (
                StatusCodes.Status409Conflict,
                "duplicate_identification_number",
                "Identificación duplicada",
                "Ya existe un empleado con el mismo tipo y número de identificación."
            ),

            EmployeeErrorCode.UserNotFound =>
            (
                StatusCodes.Status404NotFound,
                "user_not_found",
                "Usuario no encontrado",
                "El usuario indicado no existe."
            ),

            EmployeeErrorCode.UserAlreadyAssigned =>
            (
                StatusCodes.Status409Conflict,
                "user_already_assigned",
                "Usuario ya asignado",
                "El usuario indicado ya está vinculado a otro empleado."
            ),

            EmployeeErrorCode.DepartmentNotFound =>
            (
                StatusCodes.Status404NotFound,
                "department_not_found",
                "Departamento no encontrado",
                "El departamento indicado no existe."
            ),

            EmployeeErrorCode.DepartmentInactive =>
            (
                StatusCodes.Status409Conflict,
                "department_inactive",
                "Departamento inactivo",
                "El departamento indicado no está activo."
            ),

            EmployeeErrorCode.ConcurrencyConflict =>
            (
                StatusCodes.Status409Conflict,
                "concurrency_conflict",
                "El empleado fue modificado",
                "Actualice la información e intente nuevamente."
            ),

            EmployeeErrorCode.AccessNotAvailable =>
            (
                StatusCodes.Status403Forbidden,
                "access_not_available",
                "Acceso no disponible",
                "La cuenta no está habilitada para administrar empleados."
            ),

            _ =>
            (
                StatusCodes.Status500InternalServerError,
                "employee_error",
                "Error al procesar el empleado",
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

    private static EmployeeResponse MapEmployee(
        EmployeeInfo employee)
    {
        return new EmployeeResponse(
            EmployeeId:
                employee.EmployeeId,
            UserId:
                employee.UserId,
            EmailAddress:
                employee.EmailAddress,
            DepartmentId:
                employee.DepartmentId,
            DepartmentCode:
                employee.DepartmentCode,
            DepartmentName:
                employee.DepartmentName,
            IsDepartmentActive:
                employee.IsDepartmentActive,
            IdentificationType:
                employee.IdentificationType,
            IdentificationNumber:
                employee.IdentificationNumber,
            FirstName:
                employee.FirstName,
            LastName:
                employee.LastName,
            PhoneNumber:
                employee.PhoneNumber,
            BirthDate:
                employee.BirthDate,
            HireDate:
                employee.HireDate,
            TerminationDate:
                employee.TerminationDate,
            JobTitle:
                employee.JobTitle,
            BaseSalary:
                employee.BaseSalary,
            ProfileImagePath:
                employee.ProfileImagePath,
            IsActive:
                employee.IsActive,
            CreatedAtUtc:
                employee.CreatedAtUtc,
            CreatedByUserId:
                employee.CreatedByUserId,
            UpdatedAtUtc:
                employee.UpdatedAtUtc,
            UpdatedByUserId:
                employee.UpdatedByUserId,
            RowVersion:
                Convert.ToBase64String(
                    employee.RowVersion));
    }

    private static EmployeeSalaryHistoryResponse
        MapSalaryHistory(
            EmployeeSalaryHistoryInfo salaryHistory)
    {
        return new EmployeeSalaryHistoryResponse(
            EmployeeSalaryHistoryId:
                salaryHistory.EmployeeSalaryHistoryId,
            EmployeeId:
                salaryHistory.EmployeeId,
            IdentificationType:
                salaryHistory.IdentificationType,
            IdentificationNumber:
                salaryHistory.IdentificationNumber,
            FirstName:
                salaryHistory.FirstName,
            LastName:
                salaryHistory.LastName,
            DepartmentId:
                salaryHistory.DepartmentId,
            DepartmentCode:
                salaryHistory.DepartmentCode,
            DepartmentName:
                salaryHistory.DepartmentName,
            BaseSalary:
                salaryHistory.BaseSalary,
            EffectiveFromDate:
                salaryHistory.EffectiveFromDate,
            EffectiveToDate:
                salaryHistory.EffectiveToDate,
            IsCurrent:
                salaryHistory.IsCurrent,
            CreatedAtUtc:
                salaryHistory.CreatedAtUtc,
            CreatedByUserId:
                salaryHistory.CreatedByUserId,
            UpdatedAtUtc:
                salaryHistory.UpdatedAtUtc,
            UpdatedByUserId:
                salaryHistory.UpdatedByUserId,
            RowVersion:
                Convert.ToBase64String(
                    salaryHistory.RowVersion));
    }

    private static AssignableEmployeeUserResponse
        MapAssignableUser(
            AssignableEmployeeUserInfo user)
    {
        return new AssignableEmployeeUserResponse(
            UserId:
                user.UserId,
            EmailAddress:
                user.EmailAddress,
            RoleId:
                user.RoleId,
            RoleCode:
                user.RoleCode,
            RoleName:
                user.RoleName,
            AssignedEmployeeId:
                user.AssignedEmployeeId,
            AssignedEmployeeFirstName:
                user.AssignedEmployeeFirstName,
            AssignedEmployeeLastName:
                user.AssignedEmployeeLastName);
    }

    private static EmployeeIdentificationTypeResponse
        MapIdentificationType(
            EmployeeIdentificationTypeInfo
                identificationType)
    {
        return new EmployeeIdentificationTypeResponse(
            IdentificationType:
                identificationType.IdentificationType,
            Name:
                identificationType.Name,
            MinLength:
                identificationType.MinLength,
            MaxLength:
                identificationType.MaxLength,
            IsNumericOnly:
                identificationType.IsNumericOnly,
            AllowsLeadingZero:
                identificationType.AllowsLeadingZero,
            SortOrder:
                identificationType.SortOrder);
    }
}
