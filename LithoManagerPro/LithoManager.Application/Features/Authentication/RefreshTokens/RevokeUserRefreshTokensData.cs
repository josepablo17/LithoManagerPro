namespace LithoManager.Application.Features.Authentication
    .RefreshTokens;

public sealed class RevokeUserRefreshTokensData
{
    public int UserId { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public int RevokedCount { get; set; }

    public bool WasRevoked { get; set; }
}
