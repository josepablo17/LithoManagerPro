using LithoManager.Application.Features.Authentication.Login;

namespace LithoManager.Application.Features.Authentication
    .RefreshSession;

public sealed record RefreshSessionCommand(
    string? RefreshToken,
    AuthenticationRequestContext RequestContext);
