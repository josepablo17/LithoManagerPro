using LithoManager.Application.Features.Authentication
    .Login;
using LithoManager.Application.Features.LeaveManagement;
using Microsoft.AspNetCore.Mvc;

namespace LithoManager.Api.Extensions;

public static class LeaveManagementControllerExtensions
{
    public static bool TryPrepareLeaveManagementMutationContext(
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

    public static ObjectResult CreateLeaveManagementFailureResponse(
        this ControllerBase controller,
        LeaveManagementErrorCode errorCode,
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
            LeaveManagementErrorCode.InvalidRequest =>
            (
                StatusCodes.Status400BadRequest,
                "invalid_request",
                "Solicitud inválida",
                "Revise los datos enviados."
            ),

            LeaveManagementErrorCode.AccessNotAvailable =>
            (
                StatusCodes.Status403Forbidden,
                "access_not_available",
                "Acceso no disponible",
                "La cuenta no está habilitada para esta operación."
            ),

            LeaveManagementErrorCode.EmployeeNotFound =>
            (
                StatusCodes.Status404NotFound,
                "employee_not_found",
                "Empleado no encontrado",
                "El empleado solicitado no existe."
            ),

            LeaveManagementErrorCode.EmployeeInactive =>
            (
                StatusCodes.Status409Conflict,
                "employee_inactive",
                "Empleado inactivo",
                "El empleado indicado no está activo."
            ),

            LeaveManagementErrorCode.DepartmentInactive =>
            (
                StatusCodes.Status409Conflict,
                "department_inactive",
                "Departamento inactivo",
                "El departamento del empleado no está activo."
            ),

            LeaveManagementErrorCode.LeaveTypeNotFound =>
            (
                StatusCodes.Status404NotFound,
                "leave_type_not_found",
                "Tipo de licencia no encontrado",
                "El tipo de licencia indicado no existe o no está activo."
            ),

            LeaveManagementErrorCode.LeavePolicyNotFound =>
            (
                StatusCodes.Status404NotFound,
                "leave_policy_not_found",
                "Política no encontrada",
                "No existe una política activa para el tipo de licencia."
            ),

            LeaveManagementErrorCode.LeaveBalanceNotFound =>
            (
                StatusCodes.Status404NotFound,
                "leave_balance_not_found",
                "Saldo no encontrado",
                "El empleado no tiene saldo registrado para vacaciones."
            ),

            LeaveManagementErrorCode.InsufficientLeaveBalance =>
            (
                StatusCodes.Status409Conflict,
                "insufficient_leave_balance",
                "Saldo insuficiente",
                "El empleado no tiene suficientes días disponibles."
            ),

            LeaveManagementErrorCode.PendingLeaveRequestExists =>
            (
                StatusCodes.Status409Conflict,
                "pending_leave_request_exists",
                "Solicitud pendiente existente",
                "El empleado ya tiene una solicitud pendiente."
            ),

            LeaveManagementErrorCode.LeaveRequestDateOverlap =>
            (
                StatusCodes.Status409Conflict,
                "leave_request_date_overlap",
                "Fechas no disponibles",
                "El empleado ya tiene una solicitud en ese rango de fechas."
            ),

            LeaveManagementErrorCode.LeaveRequestNotFound =>
            (
                StatusCodes.Status404NotFound,
                "leave_request_not_found",
                "Solicitud no encontrada",
                "La solicitud de vacaciones no existe."
            ),

            LeaveManagementErrorCode.ConcurrencyConflict =>
            (
                StatusCodes.Status409Conflict,
                "concurrency_conflict",
                "La solicitud fue modificada",
                "Actualice la información e intente nuevamente."
            ),

            LeaveManagementErrorCode.LeaveRequestAlreadyResolved =>
            (
                StatusCodes.Status409Conflict,
                "leave_request_already_resolved",
                "Solicitud ya resuelta",
                "Solo se pueden modificar solicitudes pendientes."
            ),

            _ =>
            (
                StatusCodes.Status500InternalServerError,
                "leave_management_error",
                "Error al procesar vacaciones",
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

    public static ProblemDetails CreateInvalidRowVersionProblem(
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

    public static bool TryParseRowVersion(
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
}
