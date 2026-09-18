using System.Net.Mail;
using LithoManager.Application.Abstractions.Security;

namespace LithoManager.Bootstrap;

internal sealed class ConsoleCredentialReader(
    IPasswordPolicy passwordPolicy)
{
    private const int MaximumEmailAddressLength = 254;

    private readonly IPasswordPolicy _passwordPolicy =
        passwordPolicy
        ?? throw new ArgumentNullException(
            nameof(passwordPolicy));

    public BootstrapCredentials Read()
    {
        if (Console.IsInputRedirected)
        {
            throw new InvalidOperationException(
                "Bootstrap requires an interactive terminal so the " +
                "temporary password can be entered securely.");
        }

        string emailAddress = ReadEmailAddress();
        string temporaryPassword = ReadTemporaryPassword();

        return new BootstrapCredentials(
            emailAddress,
            temporaryPassword);
    }

    private static string ReadEmailAddress()
    {
        Console.Write("Super administrator email: ");

        string? value = Console.ReadLine();
        string emailAddress = value?.Trim()
            ?? string.Empty;

        if (emailAddress.Length > MaximumEmailAddressLength
            || !MailAddress.TryCreate(
                emailAddress,
                out MailAddress? parsedAddress)
            || !string.Equals(
                parsedAddress.Address,
                emailAddress,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BootstrapInputException(
                "Enter a valid email address without a display name.");
        }

        return emailAddress;
    }

    private string ReadTemporaryPassword()
    {
        Console.Write("Temporary password: ");
        string password = ReadSecret();

        Console.Write("Confirm temporary password: ");
        string confirmation = ReadSecret();

        if (!string.Equals(
                password,
                confirmation,
                StringComparison.Ordinal))
        {
            throw new BootstrapInputException(
                "The temporary password and confirmation do not match.");
        }

        if (!_passwordPolicy.IsStrongPassword(password))
        {
            throw new BootstrapInputException(
                "The temporary password must contain 12 to 128 " +
                "characters, including uppercase, lowercase, a number " +
                "and a special character, without leading or trailing " +
                "whitespace.");
        }

        return password;
    }

    private static string ReadSecret()
    {
        List<char> characters = [];

        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(
                intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string([.. characters]);
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (characters.Count > 0)
                {
                    characters.RemoveAt(
                        characters.Count - 1);
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                characters.Add(key.KeyChar);
            }
        }
    }
}

internal sealed record BootstrapCredentials(
    string EmailAddress,
    string TemporaryPassword);

internal sealed class BootstrapInputException(
    string message)
    : Exception(message);
