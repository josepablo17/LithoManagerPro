namespace LithoManager.Infrastructure.Notifications
    .Email;

public sealed class EmailOptions
{
    public const string SectionName =
        "Notifications:Email";

    public bool IsEnabled
    {
        get;
        init;
    }

    public string Host
    {
        get;
        init;
    } = string.Empty;

    public int Port
    {
        get;
        init;
    } = 587;

    public SmtpSecurityMode SecurityMode
    {
        get;
        init;
    } = SmtpSecurityMode.StartTls;

    public string? UserName
    {
        get;
        init;
    }

    public string? Password
    {
        get;
        init;
    }

    public string FromAddress
    {
        get;
        init;
    } = string.Empty;

    public string FromName
    {
        get;
        init;
    } = "LithoManager";

    public string PasswordResetBaseUrl
    {
        get;
        init;
    } = string.Empty;

    public int TimeoutMilliseconds
    {
        get;
        init;
    } = 15000;
}