namespace LithoManager.Application.Features.Authentication.ForgotPassword;

public sealed class CreatePasswordResetTokenData
{
    public int? PasswordResetTokenId { get; set; }

    public int? UserId { get; set; }

    public string? EmailAddress { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public bool WasCreated { get; set; }
}