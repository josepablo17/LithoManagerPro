using LithoManager.Application.Features.Authentication.Login;

namespace LithoManager.Application.Features.Authentication.Logout;

public sealed record LogoutCommand(
    string? RefreshToken,
    AuthenticationRequestContext RequestContext);
