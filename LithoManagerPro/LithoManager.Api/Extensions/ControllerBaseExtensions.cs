using LithoManager.Application.Features.Authentication
    .Login;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;

namespace LithoManager.Api.Extensions;

public static class ControllerBaseExtensions
{
    private const string CorrelationIdHeaderName =
        "X-Correlation-ID";

    public static Guid PrepareNoStoreResponse(
        this ControllerBase controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        Guid correlationId =
            controller.ResolveCorrelationId();

        controller.Response.Headers[
            CorrelationIdHeaderName] =
                correlationId.ToString();

        controller.Response.Headers["Cache-Control"] =
            "no-store";

        controller.Response.Headers["Pragma"] =
            "no-cache";

        return correlationId;
    }

    public static AuthenticationRequestContext
        CreateAuthenticationRequestContext(
            this ControllerBase controller,
            Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(controller);

        return new AuthenticationRequestContext(
            CorrelationId:
                correlationId,
            ClientIpAddress:
                LimitLength(
                    controller.HttpContext
                        .Connection
                        .RemoteIpAddress?
                        .ToString(),
                    maximumLength: 45),
            UserAgent:
                LimitLength(
                    controller.Request
                        .Headers["User-Agent"]
                        .ToString(),
                    maximumLength: 512),
            RequestPath:
                LimitLength(
                    controller.Request.Path.Value,
                    maximumLength: 500));
    }

    public static bool TryResolveAuthenticatedUserId(
        this ControllerBase controller,
        out int userId)
    {
        ArgumentNullException.ThrowIfNull(controller);

        string? userIdValue =
            controller.User.FindFirst(
                JwtRegisteredClaimNames.Sub)?
                .Value;

        return int.TryParse(
                userIdValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out userId)
            && userId > 0;
    }

    public static ProblemDetails CreateProblemDetails(
        this ControllerBase controller,
        int statusCode,
        string title,
        string detail,
        string errorCode,
        Guid? correlationId = null)
    {
        ArgumentNullException.ThrowIfNull(controller);

        ProblemDetails problemDetails =
            new()
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = controller.Request.Path
            };

        problemDetails.Extensions["errorCode"] =
            errorCode;

        if (correlationId.HasValue)
        {
            problemDetails.Extensions[
                "correlationId"] =
                    correlationId.Value;
        }

        return problemDetails;
    }

    private static Guid ResolveCorrelationId(
        this ControllerBase controller)
    {
        string headerValue =
            controller.Request.Headers[
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
}
