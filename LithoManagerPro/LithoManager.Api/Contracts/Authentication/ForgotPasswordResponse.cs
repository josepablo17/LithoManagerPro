namespace LithoManager.Api.Contracts.Authentication;

public sealed record ForgotPasswordResponse(
    string Message,
    Guid CorrelationId);