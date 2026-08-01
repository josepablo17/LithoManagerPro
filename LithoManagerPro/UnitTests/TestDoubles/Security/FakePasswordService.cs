using LithoManager.Application.Abstractions.Security;

namespace LithoManager.UnitTests.TestDoubles.Security;

public sealed class FakePasswordService
    : IPasswordService
{
    public string HashToReturn
    {
        get;
        set;
    } = "fake-password-hash";

    public bool VerifyPasswordResult
    {
        get;
        set;
    }

    public int HashPasswordCallCount
    {
        get;
        private set;
    }

    public string? PasswordReceivedForHash
    {
        get;
        private set;
    }

    public int VerifyPasswordCallCount
    {
        get;
        private set;
    }

    public string? PasswordHashReceivedForVerification
    {
        get;
        private set;
    }

    public string? ProvidedPasswordReceived
    {
        get;
        private set;
    }

    public string HashPassword(
        string password)
    {
        HashPasswordCallCount++;

        PasswordReceivedForHash =
            password;

        return HashToReturn;
    }

    public bool VerifyPassword(
        string passwordHash,
        string providedPassword)
    {
        VerifyPasswordCallCount++;

        PasswordHashReceivedForVerification =
            passwordHash;

        ProvidedPasswordReceived =
            providedPassword;

        return VerifyPasswordResult;
    }
}