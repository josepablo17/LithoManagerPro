namespace LithoManager.Application.Features.Authentication.Login;

public sealed class SuccessfulLoginRegistrationData
{
    public int UserId { get; init; }

    public DateTime LastLoginAtUtc { get; init; }

    public short FailedLoginAttempts { get; init; }

    public DateTime? LockoutEndAtUtc { get; init; }
}