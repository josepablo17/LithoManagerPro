using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Payroll;
using Microsoft.AspNetCore.Mvc;

namespace LithoManager.Api.Extensions;

public static class PayrollControllerExtensions
{
    public static bool TryPreparePayrollMutationContext(
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
                        StatusCodes.Status401Unauthorized,
                        "Token inválido",
                        "No fue posible identificar al usuario.",
                        "invalid_token",
                        correlationId));

            return false;
        }

        requestContext =
            controller.CreateAuthenticationRequestContext(
                correlationId);

        return true;
    }

    public static ObjectResult CreatePayrollFailureResponse(
        this ControllerBase controller,
        PayrollErrorCode errorCode,
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
            PayrollErrorCode.InvalidRequest =>
            (
                StatusCodes.Status400BadRequest,
                "invalid_request",
                "Solicitud inválida",
                "Revise los datos enviados."
            ),

            PayrollErrorCode.AccessNotAvailable =>
            (
                StatusCodes.Status403Forbidden,
                "access_not_available",
                "Acceso no disponible",
                "La cuenta no está habilitada para esta operación."
            ),

            PayrollErrorCode.EmployeeNotFound =>
            (
                StatusCodes.Status404NotFound,
                "employee_not_found",
                "Empleado no encontrado",
                "El empleado solicitado no existe."
            ),

            PayrollErrorCode.EmployeeInactive =>
            (
                StatusCodes.Status409Conflict,
                "employee_inactive",
                "Empleado inactivo",
                "El empleado indicado no está activo."
            ),

            PayrollErrorCode.DepartmentInactive =>
            (
                StatusCodes.Status409Conflict,
                "department_inactive",
                "Departamento inactivo",
                "El departamento del empleado no está activo."
            ),

            PayrollErrorCode.ConfigurationNotFound =>
            (
                StatusCodes.Status404NotFound,
                "configuration_not_found",
                "Configuración no encontrada",
                "La configuración indicada no existe o no está activa."
            ),

            PayrollErrorCode.AttendanceRecordNotFound =>
            (
                StatusCodes.Status404NotFound,
                "attendance_record_not_found",
                "Registro de asistencia no encontrado",
                "El registro de asistencia indicado no existe."
            ),

            PayrollErrorCode.OvertimeRecordNotFound =>
            (
                StatusCodes.Status404NotFound,
                "overtime_record_not_found",
                "Registro de horas extra no encontrado",
                "El registro de horas extra indicado no existe."
            ),

            PayrollErrorCode.EmployeeDisabilityNotFound =>
            (
                StatusCodes.Status404NotFound,
                "employee_disability_not_found",
                "Incapacidad no encontrada",
                "El registro de incapacidad indicado no existe."
            ),

            PayrollErrorCode.DuplicateRecord =>
            (
                StatusCodes.Status409Conflict,
                "duplicate_record",
                "Registro duplicado",
                "Ya existe un registro con los mismos datos clave."
            ),

            PayrollErrorCode.DateOverlap =>
            (
                StatusCodes.Status409Conflict,
                "date_overlap",
                "Fechas no disponibles",
                "Ya existe un registro activo en ese rango de fechas."
            ),

            PayrollErrorCode.InvalidState =>
            (
                StatusCodes.Status409Conflict,
                "invalid_state",
                "Estado no válido",
                "El registro ya no se encuentra en un estado modificable."
            ),

            PayrollErrorCode.ConcurrencyConflict =>
            (
                StatusCodes.Status409Conflict,
                "concurrency_conflict",
                "El registro fue modificado",
                "Actualice la información e intente nuevamente."
            ),

            _ =>
            (
                StatusCodes.Status500InternalServerError,
                "payroll_error",
                "Error al procesar planilla",
                "No fue posible completar la operación solicitada."
            )
        };

        ProblemDetails problemDetails =
            controller.CreateProblemDetails(
                error.statusCode,
                error.title,
                error.detail,
                error.apiErrorCode,
                correlationId);

        return controller.StatusCode(
            error.statusCode,
            problemDetails);
    }

    public static ProblemDetails CreateInvalidPayrollRowVersionProblem(
        this ControllerBase controller,
        Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(controller);

        return controller.CreateProblemDetails(
            StatusCodes.Status400BadRequest,
            "RowVersion inválido",
            "ExpectedRowVersion debe ser un valor Base64 de 8 bytes.",
            "invalid_row_version",
            correlationId);
    }

    public static bool TryParsePayrollRowVersion(
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
