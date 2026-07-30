namespace LithoManager.Application.Abstractions.Security;

public sealed record AccessTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);