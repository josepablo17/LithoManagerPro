namespace LithoManager.Api.Contracts.Authentication;

public sealed record LoginResponse(
    bool RequiresPasswordChange,
    string TokenType,
    string? AccessToken,
    DateTimeOffset? AccessTokenExpiresAtUtc,
    string? PasswordChangeToken,
    DateTimeOffset? PasswordChangeTokenExpiresAtUtc,
    LoginUserResponse User);