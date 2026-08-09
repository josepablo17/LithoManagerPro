namespace LithoManager.Application.Abstractions.Security;

public interface IPasswordPolicy
{
    bool IsStrongPassword(
        string password);
}
