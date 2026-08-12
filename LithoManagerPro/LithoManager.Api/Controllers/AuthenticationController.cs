using LithoManager.Api.Authorization;
using LithoManager.Api.Contracts.Authentication;
using LithoManager.Api.Extensions;
using LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;
using LithoManager.Application.Features.Authentication.Logout;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Authentication
    .RefreshSession;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using LithoManager.Application.Features.Authentication
    .ChangePassword;
using LithoManager.Application.Features.Authentication
    .ForgotPassword;
using LithoManager.Application.Features.Authentication
    .ResetPassword;

namespace LithoManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthenticationController : ControllerBase
{
    private const string RefreshTokenCookieName =
        "__Host-LithoManager.RefreshToken";

    private readonly IAuthenticationService
        _authenticationService;

    private readonly IRefreshSessionService
        _refreshSessionService;

    private readonly ILogoutService _logoutService;

    private readonly IChangeTemporaryPasswordService
    _changeTemporaryPasswordService;

    private readonly IChangePasswordService
    _changePasswordService;

    private readonly IForgotPasswordService
    _forgotPasswordService;

    private readonly IResetPasswordService
    _resetPasswordService;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        IRefreshSessionService refreshSessionService,
        ILogoutService logoutService,
        IChangeTemporaryPasswordService
            changeTemporaryPasswordService,
        IChangePasswordService changePasswordService,
        IForgotPasswordService forgotPasswordService,
        IResetPasswordService resetPasswordService)
    {
        ArgumentNullException.ThrowIfNull(
            authenticationService);

        ArgumentNullException.ThrowIfNull(
            refreshSessionService);

        ArgumentNullException.ThrowIfNull(
            logoutService);

        ArgumentNullException.ThrowIfNull(
            changeTemporaryPasswordService);

        ArgumentNullException.ThrowIfNull(
            changePasswordService);

        ArgumentNullException.ThrowIfNull(
            forgotPasswordService);

        ArgumentNullException.ThrowIfNull(
            resetPasswordService);

        _authenticationService =
            authenticationService;

        _refreshSessionService =
            refreshSessionService;

        _logoutService = logoutService;

        _changeTemporaryPasswordService =
            changeTemporaryPasswordService;

        _changePasswordService =
            changePasswordService;

        _forgotPasswordService =
            forgotPasswordService;

        _resetPasswordService =
            resetPasswordService;
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
            this.PrepareNoStoreResponse();

        AuthenticationRequestContext requestContext =
            this.CreateAuthenticationRequestContext(
                correlationId);

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

        if (result.RequiresPasswordChange)
        {
            DeleteRefreshTokenCookie();
        }
        else
        {
            SetRefreshTokenCookie(
                result.RefreshToken,
                result.RefreshTokenExpiresAtUtc);
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

    [AllowAnonymous]
    [HttpPost("refresh")]
    [Produces("application/json")]
    [ProducesResponseType(
        typeof(LoginResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>>
    Refresh(
        CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!Request.Cookies.TryGetValue(
                RefreshTokenCookieName,
                out string? refreshToken))
        {
            DeleteRefreshTokenCookie();

            return Unauthorized(
                CreateRefreshFailure(
                    correlationId));
        }

        AuthenticationRequestContext requestContext =
            this.CreateAuthenticationRequestContext(
                correlationId);

        RefreshSessionResult result =
            await _refreshSessionService.RefreshAsync(
                new RefreshSessionCommand(
                    RefreshToken:
                        refreshToken,
                    RequestContext:
                        requestContext),
                cancellationToken);

        if (!result.IsSuccessful)
        {
            DeleteRefreshTokenCookie();

            return Unauthorized(
                CreateRefreshFailure(
                    correlationId));
        }

        if (result.User is null)
        {
            throw new InvalidOperationException(
                "A successful refresh must contain user data.");
        }

        SetRefreshTokenCookie(
            result.RefreshToken,
            result.RefreshTokenExpiresAtUtc);

        LoginResponse response =
            new(
                RequiresPasswordChange: false,
                TokenType: "Bearer",
                AccessToken:
                    result.AccessToken,
                AccessTokenExpiresAtUtc:
                    result.AccessTokenExpiresAtUtc,
                PasswordChangeToken: null,
                PasswordChangeTokenExpiresAtUtc: null,
                User:
                    MapUser(result.User));

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (Request.Cookies.TryGetValue(
                RefreshTokenCookieName,
                out string? refreshToken))
        {
            AuthenticationRequestContext requestContext =
                this.CreateAuthenticationRequestContext(
                    correlationId);

            await _logoutService.LogoutAsync(
                new LogoutCommand(
                    RefreshToken:
                        refreshToken,
                    RequestContext:
                        requestContext),
                cancellationToken);
        }

        DeleteRefreshTokenCookie();

        return NoContent();
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
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!this.TryResolveAuthenticatedUserId(
                out int userId))
        {
            return Unauthorized(
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
        }

        AuthenticationRequestContext requestContext =
            this.CreateAuthenticationRequestContext(
                correlationId);

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


    [Authorize]
    [HttpPost("change-password")]
    [Consumes("application/json")]
    [ProducesResponseType(
    StatusCodes.Status204NoContent)]
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
    StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword(
    [FromBody] ChangePasswordRequest request,
    CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        if (!this.TryResolveAuthenticatedUserId(
                out int userId))
        {
            ProblemDetails problemDetails =
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
                        correlationId);

            return Unauthorized(problemDetails);
        }

        AuthenticationRequestContext requestContext =
            this.CreateAuthenticationRequestContext(
                correlationId);

        ChangePasswordCommand command =
            new(
                UserId:
                    userId,
                CurrentPassword:
                    request.CurrentPassword,
                NewPassword:
                    request.NewPassword,
                ConfirmNewPassword:
                    request.ConfirmNewPassword,
                RequestContext:
                    requestContext);

        ChangePasswordResult result =
            await _changePasswordService.ChangeAsync(
                command,
                cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateVoluntaryPasswordChangeFailure(
                result,
                correlationId);
        }

        return NoContent();
    }


    private ObjectResult
    CreateVoluntaryPasswordChangeFailure(
        ChangePasswordResult result,
        Guid correlationId)
    {
        (
            int statusCode,
            string errorCode,
            string title,
            string detail
        ) error = result.ErrorCode switch
        {
            ChangePasswordErrorCode.InvalidRequest =>
            (
                StatusCodes.Status400BadRequest,
                "invalid_request",
                "Solicitud inválida",
                "Revise las contraseñas enviadas."
            ),

            ChangePasswordErrorCode
                .PasswordsDoNotMatch =>
            (
                StatusCodes.Status400BadRequest,
                "passwords_do_not_match",
                "Las contraseñas no coinciden",
                "La nueva contraseña y su " +
                "confirmación deben ser iguales."
            ),

            ChangePasswordErrorCode.WeakPassword =>
            (
                StatusCodes.Status400BadRequest,
                "weak_password",
                "La contraseña no cumple los requisitos",
                "Utilice al menos 12 caracteres, " +
                "incluyendo mayúsculas, minúsculas, " +
                "números y caracteres especiales."
            ),

            ChangePasswordErrorCode
                .PasswordReuseNotAllowed =>
            (
                StatusCodes.Status400BadRequest,
                "password_reuse_not_allowed",
                "Contraseña no permitida",
                "La nueva contraseña debe ser " +
                "diferente de la contraseña actual."
            ),

            ChangePasswordErrorCode
                .CurrentPasswordInvalid =>
            (
                StatusCodes.Status401Unauthorized,
                "current_password_invalid",
                "Contraseña actual incorrecta",
                "La contraseña actual ingresada " +
                "no es correcta."
            ),

            ChangePasswordErrorCode
                .AccessNotAvailable =>
            (
                StatusCodes.Status403Forbidden,
                "access_not_available",
                "Acceso no disponible",
                "La cuenta no está habilitada " +
                "para cambiar la contraseña."
            ),

            _ =>
            (
                StatusCodes.Status500InternalServerError,
                "password_change_error",
                "Error al cambiar la contraseña",
                "No fue posible completar el " +
                "cambio de contraseña."
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
                    error.errorCode,
                correlationId:
                    correlationId);

        return StatusCode(
            error.statusCode,
            problemDetails);
    }

    private ProblemDetails CreateRefreshFailure(
        Guid correlationId)
    {
        return this.CreateProblemDetails(
            statusCode:
                StatusCodes.Status401Unauthorized,
            title:
                "Sesión no válida",
            detail:
                "La sesión ya no está disponible. " +
                "Inicie sesión nuevamente.",
            errorCode:
                "invalid_refresh_token",
            correlationId:
                correlationId);
    }

    private void SetRefreshTokenCookie(
        string? refreshToken,
        DateTime? expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidOperationException(
                "A refresh token is required to set the cookie.");
        }

        if (expiresAtUtc is not DateTime expiration)
        {
            throw new InvalidOperationException(
                "A refresh token expiration is required to set the cookie.");
        }

        DateTime utcExpiration =
            DateTime.SpecifyKind(
                expiration,
                DateTimeKind.Utc);

        Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshToken,
            CreateRefreshTokenCookieOptions(
                utcExpiration));
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(
            RefreshTokenCookieName,
            CreateRefreshTokenCookieOptions(
                expiresAtUtc: null));
    }

    private static CookieOptions
        CreateRefreshTokenCookieOptions(
            DateTime? expiresAtUtc)
    {
        CookieOptions options =
            new()
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            };

        if (expiresAtUtc is DateTime expiration)
        {
            options.Expires =
                new DateTimeOffset(
                    DateTime.SpecifyKind(
                        expiration,
                        DateTimeKind.Utc));
        }

        return options;
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
            this.CreateProblemDetails(
                statusCode:
                    error.statusCode,
                title:
                    error.title,
                detail:
                    error.detail,
                errorCode:
                    error.errorCode,
                correlationId:
                    correlationId);

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
            this.CreateProblemDetails(
                statusCode:
                    error.statusCode,
                title:
                    error.title,
                detail:
                    error.detail,
                errorCode:
                    error.errorCode,
                correlationId:
                    correlationId);

        return StatusCode(
            error.statusCode,
            problemDetails);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(
    typeof(ForgotPasswordResponse),
    StatusCodes.Status202Accepted)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ForgotPasswordResponse>>
    ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        AuthenticationRequestContext requestContext =
            this.CreateAuthenticationRequestContext(
                correlationId);

        ForgotPasswordCommand command =
            new(
                EmailAddress:
                    request.EmailAddress,
                RequestContext:
                    requestContext);

        ForgotPasswordResult result =
            await _forgotPasswordService
                .RequestAsync(
                    command,
                    cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateForgotPasswordFailure(
                result,
                correlationId);
        }

        ForgotPasswordResponse response =
            new(
                Message:
                    "Si existe una cuenta asociada " +
                    "al correo indicado y está " +
                    "habilitada para recuperar la " +
                    "contraseña, se enviarán las " +
                    "instrucciones correspondientes.",
                CorrelationId:
                    correlationId);

        return Accepted(response);
    }

    private ObjectResult
    CreateForgotPasswordFailure(
        ForgotPasswordResult result,
        Guid correlationId)
    {
        (
            int statusCode,
            string errorCode,
            string title,
            string detail
        ) error = result.ErrorCode switch
        {
            ForgotPasswordErrorCode.InvalidRequest =>
            (
                StatusCodes.Status400BadRequest,
                "invalid_request",
                "Solicitud inválida",
                "Revise el correo electrónico enviado."
            ),

            _ =>
            (
                StatusCodes.Status500InternalServerError,
                "forgot_password_error",
                "Error al procesar la solicitud",
                "No fue posible procesar la " +
                "solicitud de recuperación."
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
                    error.errorCode,
                correlationId:
                    correlationId);

        return StatusCode(
            error.statusCode,
            problemDetails);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [Consumes("application/json")]
    [ProducesResponseType(
    StatusCodes.Status204NoContent)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult>
ResetPassword(
    [FromBody] ResetPasswordRequest request,
    CancellationToken cancellationToken)
    {
        Guid correlationId =
            this.PrepareNoStoreResponse();

        AuthenticationRequestContext requestContext =
            this.CreateAuthenticationRequestContext(
                correlationId);

        ResetPasswordCommand command =
            new(
                Token:
                    request.Token,
                NewPassword:
                    request.NewPassword,
                ConfirmNewPassword:
                    request.ConfirmNewPassword,
                RequestContext:
                    requestContext);

        ResetPasswordResult result =
            await _resetPasswordService
                .ResetAsync(
                    command,
                    cancellationToken);

        if (!result.IsSuccessful)
        {
            return CreateResetPasswordFailure(
                result,
                correlationId);
        }

        return NoContent();
    }

    private ObjectResult
CreateResetPasswordFailure(
    ResetPasswordResult result,
    Guid correlationId)
    {
        (
            int statusCode,
            string errorCode,
            string title,
            string detail
        ) error = result.ErrorCode switch
        {
            ResetPasswordErrorCode.InvalidRequest =>
            (
                StatusCodes.Status400BadRequest,
                "invalid_request",
                "Solicitud inválida",
                "Revise los datos enviados."
            ),

            ResetPasswordErrorCode
                .PasswordsDoNotMatch =>
            (
                StatusCodes.Status400BadRequest,
                "passwords_do_not_match",
                "Las contraseñas no coinciden",
                "La nueva contraseña y su " +
                "confirmación deben ser iguales."
            ),

            ResetPasswordErrorCode.WeakPassword =>
            (
                StatusCodes.Status400BadRequest,
                "weak_password",
                "La contraseña no cumple los requisitos",
                "Utilice al menos 12 caracteres, " +
                "incluyendo mayúsculas, minúsculas, " +
                "números y caracteres especiales."
            ),

            ResetPasswordErrorCode
                .PasswordReuseNotAllowed =>
            (
                StatusCodes.Status400BadRequest,
                "password_reuse_not_allowed",
                "Contraseña no permitida",
                "La nueva contraseña debe ser " +
                "diferente de la contraseña actual."
            ),

            ResetPasswordErrorCode
                .PasswordResetNotAvailable =>
            (
                StatusCodes.Status400BadRequest,
                "password_reset_not_available",
                "Recuperación no disponible",
                "El enlace de recuperación no es " +
                "válido o ya no está disponible."
            ),

            _ =>
            (
                StatusCodes.Status500InternalServerError,
                "reset_password_error",
                "Error al restablecer la contraseña",
                "No fue posible completar el " +
                "restablecimiento de contraseña."
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
                    error.errorCode,
                correlationId:
                    correlationId);

        return StatusCode(
            error.statusCode,
            problemDetails);
    }
}
