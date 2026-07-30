using LithoManager.Application.Abstractions.Security;
using Microsoft.AspNetCore.Identity;

namespace LithoManager.Infrastructure.Security;

internal sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _passwordHasher = new();

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return _passwordHasher.HashPassword(
            user: null!,
            password);
    }

    public bool VerifyPassword(
        string passwordHash,
        string providedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(providedPassword);

        PasswordVerificationResult result =
            _passwordHasher.VerifyHashedPassword(
                user: null!,
                hashedPassword: passwordHash,
                providedPassword);

        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}