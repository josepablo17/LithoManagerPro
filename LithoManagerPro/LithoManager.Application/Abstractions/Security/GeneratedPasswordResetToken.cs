namespace LithoManager.Application.Abstractions.Security;

public sealed class GeneratedPasswordResetToken
{
    private const int ExpectedHashLength = 32;

    private readonly byte[] _tokenHash;

    public GeneratedPasswordResetToken(
        string token,
        byte[] tokenHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            token);

        ArgumentNullException.ThrowIfNull(
            tokenHash);

        if (tokenHash.Length != ExpectedHashLength)
        {
            throw new ArgumentException(
                "The password reset token hash " +
                "must contain exactly 32 bytes.",
                nameof(tokenHash));
        }

        Token = token;

        _tokenHash =
            (byte[])tokenHash.Clone();
    }

    public string Token { get; }

    public byte[] TokenHash =>
        (byte[])_tokenHash.Clone();
}