namespace LithoManager.Application.Features.Authentication
    .ResetPassword;

public sealed class PasswordResetContextData
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

    public string PasswordHash
    {
        get;
        set;
    } = string.Empty;

    public DateTime ExpiresAtUtc
    {
        get;
        set;
    }
}