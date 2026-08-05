using LithoManager.Application.Features.Authentication.Login;

namespace LithoManager.Application.Features.Authentication
    .ChangePassword;

public sealed record ChangePasswordCommand(
    int UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword,
    AuthenticationRequestContext RequestContext);