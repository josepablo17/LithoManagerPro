namespace LithoManager.Application.Features.Authentication
    .RefreshTokens;

public sealed class CreateRefreshTokenData
{
    public int RefreshTokenId { get; set; }

    public int UserId { get; set; }

    public Guid TokenFamilyId { get; set; }

    public int TokenVersion { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
