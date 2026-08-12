namespace LithoManager.Application.Features.Authentication
    .RefreshTokens;

public sealed class RevokeRefreshTokenData
{
    public int? RefreshTokenId { get; set; }

    public int? UserId { get; set; }

    public Guid? TokenFamilyId { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public bool WasRevoked { get; set; }

    public bool WasAlreadyInactive { get; set; }
}
