namespace LithoManager.Application.Features.Authentication
    .ForgotPassword;

public sealed class RevokePasswordResetTokenData
{
    public int PasswordResetTokenId
    {
        get;
        set;
    }

    public int UserId
    {
        get;
        set;
    }

    public DateTime? RevokedAtUtc
    {
        get;
        set;
    }

    public bool WasRevoked
    {
        get;
        set;
    }

    public bool IsInactive
    {
        get;
        set;
    }
}