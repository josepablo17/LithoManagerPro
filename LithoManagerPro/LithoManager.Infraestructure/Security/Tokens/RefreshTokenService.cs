using System.Security.Cryptography;
using System.Text;
using LithoManager.Application.Abstractions.Security;

namespace LithoManager.Infrastructure.Security.Tokens;

public sealed class RefreshTokenService
    : IRefreshTokenService
{
    private const int TokenLengthInBytes = 32;

    public GeneratedRefreshToken GenerateToken()
    {
        byte[] randomTokenBytes =
            new byte[TokenLengthInBytes];

        try
        {
            RandomNumberGenerator.Fill(
                randomTokenBytes);

            string token =
                EncodeBase64Url(
                    randomTokenBytes);

            byte[] tokenHash =
                ComputeTokenHash(
                    token);

            return new GeneratedRefreshToken(
                token,
                tokenHash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                randomTokenBytes);
        }
    }

    public byte[] ComputeTokenHash(
        string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            token);

        byte[] tokenBytes =
            Encoding.UTF8.GetBytes(
                token);

        try
        {
            return SHA256.HashData(
                tokenBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                tokenBytes);
        }
    }

    private static string EncodeBase64Url(
        byte[] value)
    {
        return Convert
            .ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
