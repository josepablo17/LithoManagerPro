namespace LithoManager.Application.Features.Authentication
    .RefreshTokens;

public sealed class RotateRefreshTokenData
{
    public int? CurrentRefreshTokenId { get; set; }

    public int? NewRefreshTokenId { get; set; }

    public int? UserId { get; set; }

    public Guid? TokenFamilyId { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public DateTime? RotatedAtUtc { get; set; }

    public bool WasRotated { get; set; }

    public string? FailureReason { get; set; }
}
