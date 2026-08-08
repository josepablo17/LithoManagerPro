using LithoManager.Application.Features.Authentication
    .Login;

namespace LithoManager.Application.Features.Authentication
    .ResetPassword;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ConfirmNewPassword,
    AuthenticationRequestContext RequestContext);