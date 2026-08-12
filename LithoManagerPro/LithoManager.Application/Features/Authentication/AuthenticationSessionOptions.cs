namespace LithoManager.Application.Features.Authentication;

public sealed class AuthenticationSessionOptions
{
    public const string SectionName =
        "Authentication:Session";

    public int RefreshTokenExpirationDays
    {
        get;
        init;
    } = 1;
}
