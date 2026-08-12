namespace LithoManager.Application.Abstractions.Security;

public interface IRefreshTokenService
{
    GeneratedRefreshToken GenerateToken();

    byte[] ComputeTokenHash(
        string token);
}
