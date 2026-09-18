namespace LithoManager.Infrastructure.Security.Tokens;

public sealed class JwtOptions
{
    public const string SectionName =
        "Authentication:Jwt";

    public string Issuer { get; init; } =
        string.Empty;

    public string Audience { get; init; } =
        string.Empty;

    public string SigningKeyBase64 { get; init; } =
        string.Empty;

    public int AccessTokenExpirationMinutes
    {
        get;
        init;
    } = 30;

    public int PasswordChangeTokenExpirationMinutes
    {
        get;
        init;
    } = 10;
}