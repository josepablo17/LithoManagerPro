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

    /*
     * Se mantiene para no romper las pruebas existentes.
     * Se utiliza cuando no hay resultados en la cola.
     */
    public bool VerifyPasswordResult
    {
        get;
        set;
    }

    public Queue<bool>
        VerifyPasswordResultsToReturn
    {
        get;
    } = new();

    public List<(
        string PasswordHash,
        string ProvidedPassword)>
        VerificationCalls
    {
        get;
    } = new();

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

    public string?
        PasswordHashReceivedForVerification
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

        VerificationCalls.Add(
            (
                PasswordHash: passwordHash,
                ProvidedPassword:
                    providedPassword
            ));

        if (VerifyPasswordResultsToReturn.Count > 0)
        {
            return VerifyPasswordResultsToReturn
                .Dequeue();
        }

        return VerifyPasswordResult;
    }
}