namespace LithoManager.Application.Abstractions.Security;

public interface IPasswordService
{
    string HashPassword(string password);

    bool VerifyPassword(
        string passwordHash,
        string providedPassword);
}