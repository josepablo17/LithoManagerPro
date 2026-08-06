using LithoManager.Application.Abstractions
    .Notifications;

namespace LithoManager.UnitTests.TestDoubles
    .Notifications;

public sealed class FakePasswordResetEmailSender
    : IPasswordResetEmailSender
{
    public bool ResultToReturn
    {
        get;
        set;
    } = true;

    public int CallCount
    {
        get;
        private set;
    }

    public string? LastEmailAddress
    {
        get;
        private set;
    }

    public string? LastToken
    {
        get;
        private set;
    }

    public DateTime? LastExpiresAtUtc
    {
        get;
        private set;
    }

    public Task<bool> TrySendAsync(
        string emailAddress,
        string token,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        ArgumentException.ThrowIfNullOrWhiteSpace(
            emailAddress);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            token);

        CallCount++;

        LastEmailAddress =
            emailAddress;

        LastToken =
            token;

        LastExpiresAtUtc =
            expiresAtUtc;

        return Task.FromResult(
            ResultToReturn);
    }
}