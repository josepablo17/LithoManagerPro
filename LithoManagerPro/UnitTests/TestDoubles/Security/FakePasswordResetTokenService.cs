using LithoManager.Application.Abstractions.Security;

namespace LithoManager.UnitTests.TestDoubles.Security;

public sealed class FakePasswordResetTokenService
    : IPasswordResetTokenService
{
    public GeneratedPasswordResetToken
        GeneratedTokenToReturn
    {
        get;
        set;
    } = new(
        token:
            new string(
                'A',
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

    public GeneratedPasswordResetToken
        GenerateToken()
    {
        GenerateTokenCallCount++;

        return new GeneratedPasswordResetToken(
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