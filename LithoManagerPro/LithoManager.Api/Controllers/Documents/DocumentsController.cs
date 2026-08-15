using LithoManager.Api.Authorization;
using LithoManager.Api.Contracts.Documents;
using LithoManager.Api.Extensions;
using LithoManager.Application.Abstractions.Storage;
using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.Documents;
using LithoManager.Application.Features.Documents
    .CreateEmployeeDocument;
using LithoManager.Application.Features.Documents
    .EnsureEmployeeRecord;
using LithoManager.Application.Features.Documents
    .GetDocumentTypes;
using LithoManager.Application.Features.Documents
    .GetEmployeeDocumentById;
using LithoManager.Application.Features.Documents
    .GetEmployeeDocumentDownloadContext;
using LithoManager.Application.Features.Documents
    .GetEmployeeDocuments;
using LithoManager.Application.Features.Documents
    .SetEmployeeDocumentStatus;
using LithoManager.Application.Features.Documents
    .UpdateEmployeeDocument;
using LithoManager.Application.Features
    .HumanResources.Employees;
using LithoManager.Application.Features
    .HumanResources.Employees.GetEmployees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LithoManager.Api.Controllers.Documents;

[ApiController]
[Route("api/documents")]
[Authorize]
public sealed class DocumentsController : ControllerBase
{
    private readonly IGetDocumentTypesService
        _getDocumentTypesService;

    private readonly IGetEmployeesService
        _getEmployeesService;

    private readonly IEnsureEmployeeRecordService
        _ensureEmployeeRecordService;

    private readonly IGetEmployeeDocumentsService
        _getEmployeeDocumentsService;

    private readonly IGetEmployeeDocumentByIdService
        _getEmployeeDocumentByIdService;

    private readonly IGetEmployeeDocumentDownloadContextService
        _getEmployeeDocumentDownloadContextService;

    private readonly ICreateEmployeeDocumentService
        _createEmployeeDocumentService;

    private readonly IUpdateEmployeeDocumentService
        _updateEmployeeDocumentService;

    private readonly ISetEmployeeDocumentStatusService
        _setEmployeeDocumentStatusService;

    private readonly IEmployeeDocumentStorage
        _employeeDocumentStorage;

    public DocumentsController(
        IGetDocumentTypesService getDocumentTypesService,
        IGetEmployeesService getEmployeesService,
        IEnsureEmployeeRecordService ensureEmployeeRecordService,
        IGetEmployeeDocumentsService getEmployeeDocumentsService,
        IGetEmployeeDocumentByIdService getEmployeeDocumentByIdService,
        IGetEmployeeDocumentDownloadContextService
            getEmployeeDocumentDownloadContextService,
        ICreateEmployeeDocumentService createEmployeeDocumentService,
        IUpdateEmployeeDocumentService updateEmployeeDocumentService,
        ISetEmployeeDocumentStatusService
            setEmployeeDocumentStatusService,
        IEmployeeDocumentStorage employeeDocumentStorage)
    {
        ArgumentNullException.ThrowIfNull(
            getDocumentTypesService);

        ArgumentNullException.ThrowIfNull(
            getEmployeesService);

        ArgumentNullException.ThrowIfNull(
            ensureEmployeeRecordService);

        ArgumentNullException.ThrowIfNull(
            getEmployeeDocumentsService);

        ArgumentNullException.ThrowIfNull(
            getEmployeeDocumentByIdService);

        ArgumentNullException.ThrowIfNull(
            getEmployeeDocumentDownloadContextService);

        ArgumentNullException.ThrowIfNull(
            createEmployeeDocumentService);

        ArgumentNullException.ThrowIfNull(
            updateEmployeeDocumentService);

        ArgumentNullException.ThrowIfNull(
            setEmployeeDocumentStatusService);

        ArgumentNullException.ThrowIfNull(
            employeeDocumentStorage);

        _getDocumentTypesService =
            getDocumentTypesService;

        _getEmployeesService =
            getEmployeesService;

        _ensureEmployeeRecordService =
            ensureEmployeeRecordService;

        _getEmployeeDocumentsService =
            getEmployeeDocumentsService;

        _getEmployeeDocumentByIdService =
            getEmployeeDocumentByIdService;

        _getEmployeeDocumentDownloadContextService =
            getEmployeeDocumentDownloadContextService;

        _createEmployeeDocumentService =
            createEmployeeDocumentService;

        _updateEmployeeDocumentService =
            updateEmployeeDocumentService;

        _setEmployeeDocumentStatusService =
            setEmployeeDocumentStatusService;

        _employeeDocumentStorage =
            employeeDocumentStorage;
    }

    [HttpGet("employees")]
    [Authorize(
        Policy =
            AuthorizationPolicyNames
                .DocumentAdministration)]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(IReadOnlyList<
            DocumentEmployeeOptionResponse>),
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
        DocumentEmployeeOptionResponse>>> GetEmployeeOptions(
            [FromQuery] string? searchTerm,
            CancellationToken cancellationToken)
    {
        this.PrepareNoStoreResponse();

        EmployeesResult result =
            await _getEmployeesService.GetAsync(
                new GetEmployeesQuery(
                    SearchTerm:
                        searchTerm,
                    DepartmentId:
                        null,
                    IsActive:
                        true),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return BadRequest(
                this.CreateProblemDetails(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Solicitud inválida",
                    detail:
                        "No fue posible consultar empleados.",
                    errorCode:
                        "invalid_request",
                    correlationId:
                        null));
        }

        return Ok(
            result.Employees
                .Select(MapEmployeeOption)
                .ToList());
    }

    private static DocumentEmployeeOptionResponse MapEmployeeOption(
        EmployeeInfo employee)
    {
        return new DocumentEmployeeOptionResponse(
            EmployeeId:
                employee.EmployeeId,
            IdentificationNumber:
                employee.IdentificationNumber,
            FirstName:
                employee.FirstName,
            LastName:
                employee.LastName,
            DepartmentId:
                employee.DepartmentId,
            DepartmentCode:
                employee.DepartmentCode,
            DepartmentName:
                employee.DepartmentName,
            JobTitle:
                employee.JobTitle);
    }

    [HttpGet("types")]
    [Authorize(
        Policy =
            AuthorizationPolicyNames
                .DocumentAdministration)]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(IReadOnlyList<DocumentTypeResponse>),
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
        DocumentTypeResponse>>> GetDocumentTypes(
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!this.TryPrepareDocumentMutationContext(
                correlationId,
                out int actorUserId,
                out _,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        DocumentTypesResult result =
            await _getDocumentTypesService.GetAsync(
                new GetDocumentTypesQuery(
                    ActorUserId:
                        actorUserId,
                    IsActive:
                        isActive),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateDocumentFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            result.DocumentTypes
                .Select(DocumentResponseMapper.Map)
                .ToList());
    }

    [HttpPost("employee-records/{employeeId:int}/ensure")]
    [Authorize(
        Policy =
            AuthorizationPolicyNames
                .DocumentAdministration)]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(EmployeeRecordResponse),
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
    public async Task<ActionResult<EmployeeRecordResponse>>
        EnsureEmployeeRecord(
            int employeeId,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!this.TryPrepareDocumentMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeRecordResult result =
            await _ensureEmployeeRecordService.EnsureAsync(
                new EnsureEmployeeRecordCommand(
                    EmployeeId:
                        employeeId,
                    ActorUserId:
                        actorUserId,
                    RequestContext:
                        requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateDocumentFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            DocumentResponseMapper.Map(
                result.EmployeeRecord!));
    }

    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(IReadOnlyList<EmployeeDocumentResponse>),
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
        EmployeeDocumentResponse>>> GetEmployeeDocuments(
            [FromQuery] int? employeeId,
            [FromQuery] int? documentTypeId,
            [FromQuery] bool? isActive,
            [FromQuery] bool? isVisibleToEmployee,
            [FromQuery] DateTime? createdFromUtc,
            [FromQuery] DateTime? createdToUtc,
            [FromQuery] string? searchTerm,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!this.TryPrepareDocumentMutationContext(
                correlationId,
                out int actorUserId,
                out _,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeDocumentsResult result =
            await _getEmployeeDocumentsService.GetAsync(
                new GetEmployeeDocumentsQuery(
                    ActorUserId:
                        actorUserId,
                    EmployeeId:
                        employeeId,
                    DocumentTypeId:
                        documentTypeId,
                    IsActive:
                        isActive,
                    IsVisibleToEmployee:
                        isVisibleToEmployee,
                    CreatedFromUtc:
                        createdFromUtc,
                    CreatedToUtc:
                        createdToUtc,
                    SearchTerm:
                        searchTerm),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateDocumentFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            result.EmployeeDocuments
                .Select(DocumentResponseMapper.Map)
                .ToList());
    }

    [HttpGet("{employeeDocumentId:int}")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(EmployeeDocumentResponse),
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
    public async Task<ActionResult<EmployeeDocumentResponse>>
        GetEmployeeDocumentById(
            int employeeDocumentId,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!this.TryPrepareDocumentMutationContext(
                correlationId,
                out int actorUserId,
                out _,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeDocumentResult result =
            await _getEmployeeDocumentByIdService.GetAsync(
                employeeDocumentId,
                actorUserId,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateDocumentFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            DocumentResponseMapper.Map(
                result.EmployeeDocument!));
    }

    [HttpGet("{employeeDocumentId:int}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
    public async Task<IActionResult> DownloadEmployeeDocument(
        int employeeDocumentId,
        CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!this.TryPrepareDocumentMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeDocumentDownloadContextResult result =
            await _getEmployeeDocumentDownloadContextService
                .GetAsync(
                    new GetEmployeeDocumentDownloadContextCommand(
                        EmployeeDocumentId:
                            employeeDocumentId,
                        ActorUserId:
                            actorUserId,
                        RequestContext:
                            requestContext!),
                    cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateDocumentFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        EmployeeDocumentDownloadContextInfo context =
            result.DownloadContext!;

        Stream? content =
            await _employeeDocumentStorage.OpenReadAsync(
                context.StorageProvider,
                context.StorageKey,
                cancellationToken);

        if (content is null)
        {
            return NotFound(
                this.CreateProblemDetails(
                    statusCode:
                        StatusCodes.Status404NotFound,
                    title:
                        "Archivo no encontrado",
                    detail:
                        "No fue posible localizar el archivo del documento.",
                    errorCode:
                        "document_file_not_found",
                    correlationId:
                        correlationId));
        }

        return File(
            content,
            context.ContentType,
            context.OriginalFileName);
    }

    [HttpPost]
    [Authorize(
        Policy =
            AuthorizationPolicyNames
                .DocumentAdministration)]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(EmployeeDocumentResponse),
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
    public async Task<ActionResult<EmployeeDocumentResponse>>
        CreateEmployeeDocument(
            [FromForm] CreateEmployeeDocumentRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (request.File is null
            || request.File.Length <= 0)
        {
            return BadRequest(
                this.CreateProblemDetails(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Archivo inválido",
                    detail:
                        "Debe enviar un archivo válido.",
                    errorCode:
                        "invalid_document_file",
                    correlationId:
                        correlationId));
        }

        if (!this.TryPrepareDocumentMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeDocumentStorageResult storageResult;

        try
        {
            await using Stream fileContent =
                request.File.OpenReadStream();

            storageResult =
                await _employeeDocumentStorage.SaveAsync(
                    fileContent,
                    request.File.FileName,
                    request.File.ContentType,
                    cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return BadRequest(
                this.CreateProblemDetails(
                    statusCode:
                        StatusCodes.Status400BadRequest,
                    title:
                        "Archivo inválido",
                    detail:
                        "El archivo está vacío o supera el tamaño permitido.",
                    errorCode:
                        "invalid_document_file",
                    correlationId:
                        correlationId));
        }

        EmployeeDocumentResult result =
            await _createEmployeeDocumentService.CreateAsync(
                new CreateEmployeeDocumentCommand(
                    EmployeeId:
                        request.EmployeeId ?? 0,
                    DocumentTypeId:
                        request.DocumentTypeId ?? 0,
                    Title:
                        request.Title,
                    Description:
                        request.Description,
                    OriginalFileName:
                        request.File.FileName,
                    StorageProvider:
                        storageResult.StorageProvider,
                    StorageKey:
                        storageResult.StorageKey,
                    ContentType:
                        string.IsNullOrWhiteSpace(
                            request.File.ContentType)
                            ? "application/octet-stream"
                            : request.File.ContentType,
                    FileSizeBytes:
                        storageResult.FileSizeBytes,
                    FileHash:
                        storageResult.FileHash,
                    IssuedDate:
                        request.IssuedDate,
                    ExpirationDate:
                        request.ExpirationDate,
                    IsVisibleToEmployee:
                        request.IsVisibleToEmployee,
                    ActorUserId:
                        actorUserId,
                    RequestContext:
                        requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            await _employeeDocumentStorage.DeleteAsync(
                storageResult.StorageProvider,
                storageResult.StorageKey,
                cancellationToken);

            return this.CreateDocumentFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        EmployeeDocumentResponse response =
            DocumentResponseMapper.Map(
                result.EmployeeDocument!);

        return CreatedAtAction(
            nameof(GetEmployeeDocumentById),
            new
            {
                employeeDocumentId =
                    response.EmployeeDocumentId
            },
            response);
    }

    [HttpPut("{employeeDocumentId:int}")]
    [Authorize(
        Policy =
            AuthorizationPolicyNames
                .DocumentAdministration)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(EmployeeDocumentResponse),
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
    public async Task<ActionResult<EmployeeDocumentResponse>>
        UpdateEmployeeDocument(
            int employeeDocumentId,
            [FromBody] UpdateEmployeeDocumentRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!DocumentControllerExtensions
                .TryParseDocumentRowVersion(
                    request.ExpectedRowVersion,
                    out byte[] expectedRowVersion))
        {
            return BadRequest(
                this.CreateInvalidDocumentRowVersionProblem(
                    correlationId));
        }

        if (!this.TryPrepareDocumentMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeDocumentResult result =
            await _updateEmployeeDocumentService.UpdateAsync(
                new UpdateEmployeeDocumentCommand(
                    EmployeeDocumentId:
                        employeeDocumentId,
                    DocumentTypeId:
                        request.DocumentTypeId ?? 0,
                    Title:
                        request.Title,
                    Description:
                        request.Description,
                    IssuedDate:
                        request.IssuedDate,
                    ExpirationDate:
                        request.ExpirationDate,
                    IsVisibleToEmployee:
                        request.IsVisibleToEmployee,
                    ExpectedRowVersion:
                        expectedRowVersion,
                    ActorUserId:
                        actorUserId,
                    RequestContext:
                        requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateDocumentFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            DocumentResponseMapper.Map(
                result.EmployeeDocument!));
    }

    [HttpPatch("{employeeDocumentId:int}/status")]
    [Authorize(
        Policy =
            AuthorizationPolicyNames
                .DocumentAdministrationMutation)]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(EmployeeDocumentResponse),
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
    public async Task<ActionResult<EmployeeDocumentResponse>>
        SetEmployeeDocumentStatus(
            int employeeDocumentId,
            [FromBody] SetEmployeeDocumentStatusRequest request,
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!DocumentControllerExtensions
                .TryParseDocumentRowVersion(
                    request.ExpectedRowVersion,
                    out byte[] expectedRowVersion))
        {
            return BadRequest(
                this.CreateInvalidDocumentRowVersionProblem(
                    correlationId));
        }

        if (!this.TryPrepareDocumentMutationContext(
                correlationId,
                out int actorUserId,
                out AuthenticationRequestContext? requestContext,
                out ActionResult? unauthorizedResult))
        {
            return unauthorizedResult!;
        }

        EmployeeDocumentResult result =
            await _setEmployeeDocumentStatusService.SetAsync(
                new SetEmployeeDocumentStatusCommand(
                    EmployeeDocumentId:
                        employeeDocumentId,
                    IsActive:
                        request.IsActive,
                    ExpectedRowVersion:
                        expectedRowVersion,
                    ActorUserId:
                        actorUserId,
                    RequestContext:
                        requestContext!),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return this.CreateDocumentFailureResponse(
                result.ErrorCode,
                correlationId);
        }

        return Ok(
            DocumentResponseMapper.Map(
                result.EmployeeDocument!));
    }
}
