namespace LithoManager.Application.Abstractions.Security;

public interface IPasswordResetTokenService
{
    GeneratedPasswordResetToken
        GenerateToken();

    byte[] ComputeTokenHash(
        string token);
}