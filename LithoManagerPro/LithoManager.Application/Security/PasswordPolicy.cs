using LithoManager.Application.Abstractions.Security;

namespace LithoManager.Application.Security;

public sealed class PasswordPolicy : IPasswordPolicy
{
    private const int MinimumPasswordLength = 12;
    private const int MaximumPasswordLength = 128;

    public bool IsStrongPassword(
        string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        if (password.Length
                < MinimumPasswordLength
            || password.Length
                > MaximumPasswordLength)
        {
            return false;
        }

        if (char.IsWhiteSpace(password[0])
            || char.IsWhiteSpace(password[^1]))
        {
            return false;
        }

        bool hasUppercase =
            password.Any(char.IsUpper);

        bool hasLowercase =
            password.Any(char.IsLower);

        bool hasDigit =
            password.Any(char.IsDigit);

        bool hasSpecialCharacter =
            password.Any(
                character =>
                    !char.IsLetterOrDigit(character)
                    && !char.IsWhiteSpace(character));

        return hasUppercase
            && hasLowercase
            && hasDigit
            && hasSpecialCharacter;
    }
}
