using LithoManager.Application.Features.Authentication.Login;

namespace LithoManager.Application.Features.Authentication.Logout;

public sealed record RevokeUserSessionsCommand(
    int UserId,
    string RevokedReason,
    int? ActorUserId,
    AuthenticationRequestContext RequestContext);
