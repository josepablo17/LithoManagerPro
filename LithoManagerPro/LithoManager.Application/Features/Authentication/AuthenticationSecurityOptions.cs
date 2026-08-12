namespace LithoManager.Application.Features.Authentication;

public sealed class AuthenticationSecurityOptions
{
    public const string SectionName =
        "Authentication:Security";

    public int PasswordResetTokenExpirationMinutes
    {
        get;
        init;
    } = 15;

    public short MaximumFailedLoginAttempts
    {
        get;
        init;
    } = 5;

    public int LockoutDurationMinutes
    {
        get;
        init;
    } = 15;
}
