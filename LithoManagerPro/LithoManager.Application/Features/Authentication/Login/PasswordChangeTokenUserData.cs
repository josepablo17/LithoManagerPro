namespace LithoManager.Application.Abstractions.Security;

public sealed record PasswordChangeTokenUserData(
    int UserId,
    string EmailAddress,
    int TokenVersion);
