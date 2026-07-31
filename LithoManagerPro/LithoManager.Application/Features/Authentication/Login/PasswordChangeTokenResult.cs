namespace LithoManager.Application.Abstractions.Security;

public sealed record PasswordChangeTokenResult(
    string Token,
    DateTimeOffset ExpiresAtUtc);