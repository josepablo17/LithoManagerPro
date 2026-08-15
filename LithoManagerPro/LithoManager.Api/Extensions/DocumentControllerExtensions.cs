using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.Documents;
using Microsoft.AspNetCore.Mvc;

namespace LithoManager.Api.Extensions;

public static class DocumentControllerExtensions
{
    public static bool TryPrepareDocumentMutationContext(
        this ControllerBase controller,
        Guid correlationId,
        out int actorUserId,
        out AuthenticationRequestContext? requestContext,
        out ActionResult? unauthorizedResult)
    {
        ArgumentNullException.ThrowIfNull(controller);

        requestContext = null;
        unauthorizedResult = null;

        if (!controller.TryResolveAuthenticatedUserId(
                out actorUserId))
        {
            unauthorizedResult =
                controller.Unauthorized(
                    controller.CreateProblemDetails(
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
            controller.CreateAuthenticationRequestContext(
                correlationId);

        return true;
    }

    public static ObjectResult CreateDocumentFailureResponse(
        this ControllerBase controller,
        DocumentErrorCode errorCode,
        Guid? correlationId)
    {
        ArgumentNullException.ThrowIfNull(controller);

        (
            int statusCode,
            string apiErrorCode,
            string title,
            string detail
        ) error = errorCode switch
        {
            DocumentErrorCode.InvalidRequest =>
            (
                StatusCodes.Status400BadRequest,
                "invalid_request",
                "Solicitud inválida",
                "Revise los datos enviados."
            ),

            DocumentErrorCode.AccessNotAvailable =>
            (
                StatusCodes.Status403Forbidden,
                "access_not_available",
                "Acceso no disponible",
                "La cuenta no está habilitada para esta operación."
            ),

            DocumentErrorCode.EmployeeNotFound =>
            (
                StatusCodes.Status404NotFound,
                "employee_not_found",
                "Empleado no encontrado",
                "El empleado solicitado no existe."
            ),

            DocumentErrorCode.EmployeeRecordNotFound =>
            (
                StatusCodes.Status404NotFound,
                "employee_record_not_found",
                "Expediente no encontrado",
                "El expediente del empleado no existe."
            ),

            DocumentErrorCode.DocumentTypeNotFound =>
            (
                StatusCodes.Status404NotFound,
                "document_type_not_found",
                "Tipo de documento no encontrado",
                "El tipo de documento indicado no existe o no está activo."
            ),

            DocumentErrorCode.EmployeeDocumentNotFound =>
            (
                StatusCodes.Status404NotFound,
                "employee_document_not_found",
                "Documento no encontrado",
                "El documento solicitado no existe."
            ),

            DocumentErrorCode.DuplicateStorageKey =>
            (
                StatusCodes.Status409Conflict,
                "duplicate_document_storage",
                "Documento duplicado",
                "No fue posible registrar el archivo generado."
            ),

            DocumentErrorCode.ConcurrencyConflict =>
            (
                StatusCodes.Status409Conflict,
                "concurrency_conflict",
                "El documento fue modificado",
                "Actualice la información e intente nuevamente."
            ),

            _ =>
            (
                StatusCodes.Status500InternalServerError,
                "document_error",
                "Error al procesar documentos",
                "No fue posible completar la operación solicitada."
            )
        };

        ProblemDetails problemDetails =
            controller.CreateProblemDetails(
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

        return controller.StatusCode(
            error.statusCode,
            problemDetails);
    }

    public static ProblemDetails CreateInvalidDocumentRowVersionProblem(
        this ControllerBase controller,
        Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(controller);

        return controller.CreateProblemDetails(
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

    public static bool TryParseDocumentRowVersion(
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
            value = Convert.FromBase64String(rowVersion);

            return value.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
