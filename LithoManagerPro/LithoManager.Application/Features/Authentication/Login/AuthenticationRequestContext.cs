namespace LithoManager.Application.Features.Authentication.Login;

public sealed record AuthenticationRequestContext(
    Guid CorrelationId,
    string? ClientIpAddress,
    string? UserAgent,
    string? RequestPath);