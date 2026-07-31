using LithoManager.Application.Features.Authentication.Login;

namespace LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;

public sealed record ChangeTemporaryPasswordCommand(
    int UserId,
    string NewPassword,
    string ConfirmNewPassword,
    AuthenticationRequestContext RequestContext);