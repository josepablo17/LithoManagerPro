using LithoManager.Api.Authorization;
using LithoManager.Api.Contracts.Authentication;
using LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;
using LithoManager.Application.Features.Authentication.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;

namespace LithoManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController : ControllerBase
{
    private const string CorrelationIdHeaderName =
        "X-Correlation-ID";

    private readonly IAuthenticationService
        _authenticationService;

    private readonly IChangeTemporaryPasswordService
    _changeTemporaryPasswordService;

    public AuthenticationController(
        IAuthenticationService authenticationService, IChangeTemporaryPasswordService
        changeTemporaryPasswordService)
    {
        ArgumentNullException.ThrowIfNull(
            authenticationService);

        ArgumentNullException.ThrowIfNull(
            changeTemporaryPasswordService);

        _authenticationService =
            authenticationService;

        _changeTemporaryPasswordService =
            changeTemporaryPasswordService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(LoginResponse),
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
        StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
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

        AuthenticationRequestContext requestContext =
            new(
                CorrelationId: correlationId,
                ClientIpAddress: LimitLength(
                    HttpContext.Connection
                        .RemoteIpAddress?
                        .ToString(),
                    maximumLength: 45),
                UserAgent: LimitLength(
                    Request.Headers["User-Agent"]
                        .ToString(),
                    maximumLength: 512),
                RequestPath: LimitLength(
                    Request.Path.Value,
                    maximumLength: 500));

        LoginCommand command =
            new(
                EmailAddress: request.EmailAddress,
                Password: request.Password,
                RequestContext: requestContext);

        LoginResult result =
            await _authenticationService.LoginAsync(
                command,
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
                "A successful login must contain user data.");
        }

        LoginResponse response =
            new(
                RequiresPasswordChange:
                    result.RequiresPasswordChange,
                TokenType: "Bearer",
                AccessToken:
                    result.AccessToken,
                AccessTokenExpiresAtUtc:
                    result.AccessTokenExpiresAtUtc,
                PasswordChangeToken:
                    result.PasswordChangeToken,
                PasswordChangeTokenExpiresAtUtc:
                    result.PasswordChangeTokenExpiresAtUtc,
                User:
                    MapUser(result.User));

        return Ok(response);
    }


    [Authorize(
    Policy =
        AuthorizationPolicyNames
            .PasswordChangeOnly)]
    [HttpPost("change-temporary-password")]
    [Consumes("application/json")]
    [ProducesResponseType(
    StatusCodes.Status204NoContent)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
    StatusCodes.Status403Forbidden)]
    public async Task<IActionResult>
    ChangeTemporaryPassword(
        [FromBody]
        ChangeTemporaryPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryResolveUserId(
                out int userId))
        {
            return Unauthorized(
                CreateProblemDetails(
                    statusCode:
                        StatusCodes
                            .Status401Unauthorized,
                    title:
                        "Token inválido",
                    detail:
                        "No fue posible identificar al usuario.",
                    errorCode:
                        "invalid_token"));
        }

        Guid correlationId =
            ResolveCorrelationId();

        Response.Headers[
            CorrelationIdHeaderName] =
                correlationId.ToString();

        Response.Headers["Cache-Control"] =
            "no-store";

        Response.Headers["Pragma"] =
            "no-cache";

        AuthenticationRequestContext requestContext =
            new(
                CorrelationId:
                    correlationId,
                ClientIpAddress:
                    LimitLength(
                        HttpContext.Connection
                            .RemoteIpAddress?
                            .ToString(),
                        maximumLength: 45),
                UserAgent:
                    LimitLength(
                        Request.Headers["User-Agent"]
                            .ToString(),
                        maximumLength: 512),
                RequestPath:
                    LimitLength(
                        Request.Path.Value,
                        maximumLength: 500));

        ChangeTemporaryPasswordCommand command =
            new(
                UserId: userId,
                NewPassword:
                    request.NewPassword,
                ConfirmNewPassword:
                    request.ConfirmNewPassword,
                RequestContext:
                    requestContext);

        ChangeTemporaryPasswordResult result =
            await _changeTemporaryPasswordService
                .ChangeAsync(
                    command,
                    cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreatePasswordChangeFailure(
                result,
                correlationId);
        }

        return NoContent();
    }

    private ObjectResult CreateFailureResponse(
        LoginResult result,
        Guid correlationId)
    {
        (
            int statusCode,
            string errorCode,
            string title,
            string detail
        ) error = result.ErrorCode switch
        {
            LoginErrorCode.InvalidRequest =>
            (
                StatusCodes.Status400BadRequest,
                "invalid_request",
                "Solicitud inválida",
                "Revise el correo y la contraseña enviados."
            ),

            LoginErrorCode.InvalidCredentials =>
            (
                StatusCodes.Status401Unauthorized,
                "invalid_credentials",
                "Credenciales inválidas",
                "El correo o la contraseña no son correctos."
            ),

            LoginErrorCode.AccountLocked =>
            (
                StatusCodes.Status429TooManyRequests,
                "account_locked",
                "Cuenta bloqueada temporalmente",
                "Se alcanzó el máximo de intentos permitidos."
            ),

            LoginErrorCode.EmailNotConfirmed =>
            (
                StatusCodes.Status403Forbidden,
                "email_not_confirmed",
                "Correo no confirmado",
                "Debe confirmar su correo antes de iniciar sesión."
            ),

            LoginErrorCode.TemporaryPasswordExpired =>
            (
                StatusCodes.Status403Forbidden,
                "temporary_password_expired",
                "Contraseña temporal vencida",
                "Solicite una nueva contraseña temporal."
            ),

            LoginErrorCode.AccountInactive
                or LoginErrorCode.RoleInactive
                or LoginErrorCode.EmployeeInactive =>
            (
                StatusCodes.Status403Forbidden,
                "access_not_available",
                "Acceso no disponible",
                "La cuenta no está habilitada para ingresar."
            ),

            _ =>
            (
                StatusCodes.Status500InternalServerError,
                "authentication_error",
                "Error de autenticación",
                "No fue posible completar la autenticación."
            )
        };

        ProblemDetails problemDetails =
            new()
            {
                Status = error.statusCode,
                Title = error.title,
                Detail = error.detail,
                Instance = Request.Path
            };

        problemDetails.Extensions["errorCode"] =
            error.errorCode;

        problemDetails.Extensions["correlationId"] =
            correlationId;

        if (result.LockoutEndAtUtc
            is DateTime lockoutEndAtUtc)
        {
            DateTime lockoutEndUtc =
                DateTime.SpecifyKind(
                    lockoutEndAtUtc,
                    DateTimeKind.Utc);

            problemDetails.Extensions[
                "lockoutEndAtUtc"] =
                lockoutEndUtc;

            Response.Headers["Retry-After"] =
                lockoutEndUtc.ToString(
                    "R",
                    CultureInfo.InvariantCulture);
        }

        return StatusCode(
            error.statusCode,
            problemDetails);
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

    private static string? LimitLength(
        string? value,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalizedValue =
            value.Trim();

        return normalizedValue.Length
            <= maximumLength
                ? normalizedValue
                : normalizedValue[
                    ..maximumLength];
    }

    private static LoginUserResponse MapUser(
        LoginUserData user)
    {
        return new LoginUserResponse(
            UserId: user.UserId,
            EmailAddress: user.EmailAddress,
            RoleCode: user.RoleCode,
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

    private ObjectResult
        CreatePasswordChangeFailure(
            ChangeTemporaryPasswordResult result,
            Guid correlationId)
    {
        (
            int statusCode,
            string errorCode,
            string title,
            string detail
        ) error = result.ErrorCode switch
        {
            ChangeTemporaryPasswordErrorCode
                .PasswordsDoNotMatch =>
            (
                StatusCodes.Status400BadRequest,
                "passwords_do_not_match",
                "Las contraseñas no coinciden",
                "La nueva contraseña y su confirmación deben ser iguales."
            ),

            ChangeTemporaryPasswordErrorCode
                .WeakPassword =>
            (
                StatusCodes.Status400BadRequest,
                "weak_password",
                "La contraseña no cumple los requisitos",
                "Utilice al menos 12 caracteres, incluyendo mayúsculas, minúsculas, números y caracteres especiales."
            ),

            _ =>
            (
                StatusCodes.Status400BadRequest,
                "invalid_request",
                "Solicitud inválida",
                "No fue posible procesar el cambio de contraseña."
            )
        };

        ProblemDetails problemDetails =
            CreateProblemDetails(
                error.statusCode,
                error.title,
                error.detail,
                error.errorCode);

        problemDetails.Extensions[
            "correlationId"] =
                correlationId;

        return StatusCode(
            error.statusCode,
            problemDetails);
    }

    private ProblemDetails CreateProblemDetails(
        int statusCode,
        string title,
        string detail,
        string errorCode)
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

        return problemDetails;
    }
}