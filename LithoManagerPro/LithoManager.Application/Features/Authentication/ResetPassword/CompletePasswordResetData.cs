namespace LithoManager.Application.Features.Authentication
    .ResetPassword;

public sealed class CompletePasswordResetData
{
    public int? PasswordResetTokenId
    {
        get;
        set;
    }

    public int? UserId
    {
        get;
        set;
    }

    public DateTime? PasswordChangedAtUtc
    {
        get;
        set;
    }

    public bool? RequiresPasswordChange
    {
        get;
        set;
    }

    public bool WasCompleted
    {
        get;
        set;
    }
}