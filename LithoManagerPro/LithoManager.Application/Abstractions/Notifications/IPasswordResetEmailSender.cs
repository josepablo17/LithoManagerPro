namespace LithoManager.Application.Abstractions
    .Notifications;

public interface IPasswordResetEmailSender
{
    Task<bool> TrySendAsync(
        string emailAddress,
        string token,
        DateTime expiresAtUtc,
        Guid correlationId,
        CancellationToken cancellationToken);
}