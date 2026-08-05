namespace LithoManager.Application.Features.Authentication
    .ChangePassword;

public sealed class ChangePasswordData
{
    public int UserId { get; init; }

    public DateTime PasswordChangedAtUtc { get; init; }

    public bool RequiresPasswordChange { get; init; }
}