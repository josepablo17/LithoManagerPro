namespace LithoManager.Application.Features.Authentication.Login;

public sealed class FailedLoginRegistrationData
{
    public int? UserId { get; init; }

    public short FailedLoginAttempts { get; init; }

    public DateTime? LockoutEndAtUtc { get; init; }

    public bool IsLockedOut { get; init; }
}