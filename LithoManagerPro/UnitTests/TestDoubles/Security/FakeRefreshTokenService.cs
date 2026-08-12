using LithoManager.Application.Abstractions.Security;

namespace LithoManager.UnitTests.TestDoubles.Security;

public sealed class FakeRefreshTokenService
    : IRefreshTokenService
{
    public GeneratedRefreshToken GeneratedTokenToReturn
    {
        get;
        set;
    } = new(
        token:
            new string(
                'R',
                43),
        tokenHash:
            new byte[32]);

    public byte[] ComputedHashToReturn
    {
        get;
        set;
    } = new byte[32];

    public int GenerateTokenCallCount
    {
        get;
        private set;
    }

    public int ComputeTokenHashCallCount
    {
        get;
        private set;
    }

    public string? LastTokenToHash
    {
        get;
        private set;
    }

    public GeneratedRefreshToken GenerateToken()
    {
        GenerateTokenCallCount++;

        return new GeneratedRefreshToken(
            GeneratedTokenToReturn.Token,
            GeneratedTokenToReturn.TokenHash);
    }

    public byte[] ComputeTokenHash(
        string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            token);

        ComputeTokenHashCallCount++;
        LastTokenToHash = token;

        return (byte[])
            ComputedHashToReturn.Clone();
    }
}
