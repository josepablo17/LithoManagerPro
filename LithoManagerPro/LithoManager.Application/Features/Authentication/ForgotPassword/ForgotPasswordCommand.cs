using LithoManager.Application.Features.Authentication.Login;

namespace LithoManager.Application.Features.Authentication
    .ForgotPassword;

public sealed record ForgotPasswordCommand(
    string EmailAddress,
    AuthenticationRequestContext RequestContext);