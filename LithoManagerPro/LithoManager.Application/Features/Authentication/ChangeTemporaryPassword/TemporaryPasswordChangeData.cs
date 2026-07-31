namespace LithoManager.Application.Features.Authentication
    .ChangeTemporaryPassword;

public sealed class TemporaryPasswordChangeData
{
    public int UserId { get; init; }

    public DateTime PasswordChangedAtUtc { get; init; }

    public bool RequiresPasswordChange { get; init; }
}