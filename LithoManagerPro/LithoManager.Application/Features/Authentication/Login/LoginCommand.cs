namespace LithoManager.Application.Features.Authentication.Login;

public sealed record LoginCommand(
    string EmailAddress,
    string Password,
    AuthenticationRequestContext RequestContext);