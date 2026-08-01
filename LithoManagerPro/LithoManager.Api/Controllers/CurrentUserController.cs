using LithoManager.Api.Contracts.Authentication;
using LithoManager.Application.Features.Authentication
    .GetCurrentUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;

namespace LithoManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public sealed class CurrentUserController : ControllerBase
{
    private const string CorrelationIdHeaderName =
        "X-Correlation-ID";

    private readonly IGetCurrentUserService
        _getCurrentUserService;

    public CurrentUserController(
        IGetCurrentUserService getCurrentUserService)
    {
        ArgumentNullException.ThrowIfNull(
            getCurrentUserService);

        _getCurrentUserService =
            getCurrentUserService;
    }

    [HttpGet("me")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(CurrentUserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CurrentUserResponse>>
        GetCurrentUser(
            CancellationToken cancellationToken)
    {
        Guid correlationId =
            ResolveCorrelationId();

        Response.Headers[CorrelationIdHeaderName] =
            correlationId.ToString();

        Response.Headers["Cache-Control"] =
            "no-store";

        Response.Headers["Pragma"] =
            "no-cache";

        if (!TryResolveUserId(out int userId))
        {
            ProblemDetails problemDetails =
                CreateProblemDetails(
                    statusCode:
                        StatusCodes.Status401Unauthorized,
                    title:
                        "Token inválido",
                    detail:
                        "No fue posible identificar al usuario.",
                    errorCode:
                        "invalid_token",
                    correlationId:
                        correlationId);

            return Unauthorized(problemDetails);
        }

        CurrentUserResult result =
            await _getCurrentUserService.GetAsync(
                userId,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateFailureResponse(
                result,
                correlationId);
        }

        if (result.User is null)
        {
            throw new InvalidOperationException(
                "A successful current-user result " +
                "must contain user data.");
        }

        CurrentUserResponse response =
            MapCurrentUser(result.User);

        return Ok(response);
    }

    private bool TryResolveUserId(
        out int userId)
    {
        string? userIdValue =
            User.FindFirst(
                JwtRegisteredClaimNames.Sub)?
                .Value;

        return int.TryParse(
                userIdValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out userId)
            && userId > 0;
    }

    private ObjectResult CreateFailureResponse(
        CurrentUserResult result,
        Guid correlationId)
    {
        (
            int statusCode,
            string errorCode,
            string title,
            string detail
        ) error = result.ErrorCode switch
        {
            CurrentUserErrorCode.InvalidRequest
                or CurrentUserErrorCode.UserNotFound =>
            (
                StatusCodes.Status401Unauthorized,
                "invalid_session",
                "Sesión inválida",
                "La sesión ya no es válida. " +
                "Inicie sesión nuevamente."
            ),

            CurrentUserErrorCode.PasswordChangeRequired =>
            (
                StatusCodes.Status403Forbidden,
                "session_not_available",
                "Sesión no disponible",
                "La sesión ya no puede utilizarse. " +
                "Inicie sesión nuevamente."
            ),

            CurrentUserErrorCode.AccountInactive
                or CurrentUserErrorCode.EmailNotConfirmed
                or CurrentUserErrorCode.RoleInactive
                or CurrentUserErrorCode.EmployeeInactive
                or CurrentUserErrorCode.DepartmentInactive =>
            (
                StatusCodes.Status403Forbidden,
                "access_not_available",
                "Acceso no disponible",
                "La cuenta no está habilitada " +
                "para acceder al sistema."
            ),

            _ =>
            (
                StatusCodes.Status500InternalServerError,
                "current_user_error",
                "Error al recuperar la sesión",
                "No fue posible recuperar la " +
                "información del usuario."
            )
        };

        ProblemDetails problemDetails =
            CreateProblemDetails(
                statusCode: error.statusCode,
                title: error.title,
                detail: error.detail,
                errorCode: error.errorCode,
                correlationId: correlationId);

        return StatusCode(
            error.statusCode,
            problemDetails);
    }

    private ProblemDetails CreateProblemDetails(
        int statusCode,
        string title,
        string detail,
        string errorCode,
        Guid correlationId)
    {
        ProblemDetails problemDetails =
            new()
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = Request.Path
            };

        problemDetails.Extensions["errorCode"] =
            errorCode;

        problemDetails.Extensions["correlationId"] =
            correlationId;

        return problemDetails;
    }

    private Guid ResolveCorrelationId()
    {
        string headerValue =
            Request.Headers[
                CorrelationIdHeaderName]
                .ToString();

        if (Guid.TryParse(
                headerValue,
                out Guid correlationId)
            && correlationId != Guid.Empty)
        {
            return correlationId;
        }

        return Guid.NewGuid();
    }

    private static CurrentUserResponse MapCurrentUser(
        CurrentUserInfo user)
    {
        return new CurrentUserResponse(
            UserId:
                user.UserId,
            EmailAddress:
                user.EmailAddress,
            RoleCode:
                user.RoleCode,
            RoleDisplayName:
                user.RoleDisplayName,
            EmployeeId:
                user.EmployeeId,
            FirstName:
                user.FirstName,
            LastName:
                user.LastName,
            JobTitle:
                user.JobTitle,
            ProfileImagePath:
                user.ProfileImagePath,
            DepartmentId:
                user.DepartmentId,
            DepartmentCode:
                user.DepartmentCode,
            DepartmentName:
                user.DepartmentName);
    }
}