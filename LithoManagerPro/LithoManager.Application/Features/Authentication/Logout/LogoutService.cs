using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Abstractions.Security;
using LithoManager.Application.Features.Authentication
    .RefreshTokens;

namespace LithoManager.Application.Features.Authentication.Logout;

public sealed class LogoutService : ILogoutService
{
    private const int MaximumRefreshTokenLength = 512;
    private const string LogoutRevokedReason = "Logout";

    private readonly IAuthenticationRepository
        _authenticationRepository;

    private readonly IRefreshTokenService
        _refreshTokenService;

    public LogoutService(
        IAuthenticationRepository authenticationRepository,
        IRefreshTokenService refreshTokenService)
    {
        ArgumentNullException.ThrowIfNull(
            authenticationRepository);
        ArgumentNullException.ThrowIfNull(
            refreshTokenService);

        _authenticationRepository =
            authenticationRepository;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<LogoutResult> LogoutAsync(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (command.RequestContext.CorrelationId
            == Guid.Empty)
        {
            throw new ArgumentException(
                "CorrelationId cannot be empty.",
                nameof(command));
        }

        string refreshToken =
            command.RefreshToken?.Trim()
            ?? string.Empty;

        if (!IsValidRefreshToken(refreshToken))
        {
            return LogoutResult.Failure(
                LogoutErrorCode.InvalidRequest);
        }

        byte[] tokenHash =
            _refreshTokenService.ComputeTokenHash(
                refreshToken);

        RevokeRefreshTokenData revocation =
            await _authenticationRepository
                .RevokeRefreshTokenAsync(
                    tokenHash:
                        tokenHash,
                    revokedReason:
                        LogoutRevokedReason,
                    requestContext:
                        command.RequestContext,
                    cancellationToken:
                        cancellationToken);

        return LogoutResult.Success(
            userId:
                revocation.UserId,
            revokedAtUtc:
                revocation.RevokedAtUtc,
            revokedCount:
                revocation.WasRevoked ? 1 : 0,
            wasRevoked:
                revocation.WasRevoked,
            wasAlreadyInactive:
                revocation.WasAlreadyInactive);
    }

    public async Task<LogoutResult>
    RevokeUserSessionsAsync(
        RevokeUserSessionsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(
            command.RequestContext);

        if (command.RequestContext.CorrelationId
            == Guid.Empty)
        {
            throw new ArgumentException(
                "CorrelationId cannot be empty.",
                nameof(command));
        }

        if (command.UserId <= 0
            || string.IsNullOrWhiteSpace(
                command.RevokedReason))
        {
            return LogoutResult.Failure(
                LogoutErrorCode.InvalidRequest);
        }

        if (command.ActorUserId is <= 0)
        {
            return LogoutResult.Failure(
                LogoutErrorCode.InvalidRequest);
        }

        RevokeUserRefreshTokensData revocation =
            await _authenticationRepository
                .RevokeUserRefreshTokensAsync(
                    userId:
                        command.UserId,
                    revokedReason:
                        command.RevokedReason,
                    actorUserId:
                        command.ActorUserId,
                    requestContext:
                        command.RequestContext,
                    cancellationToken:
                        cancellationToken);

        return LogoutResult.Success(
            userId:
                revocation.UserId,
            revokedAtUtc:
                revocation.RevokedAtUtc,
            revokedCount:
                revocation.RevokedCount,
            wasRevoked:
                revocation.WasRevoked,
            wasAlreadyInactive:
                false);
    }

    private static bool IsValidRefreshToken(
        string refreshToken)
    {
        return !string.IsNullOrWhiteSpace(refreshToken)
            && refreshToken.Length
                <= MaximumRefreshTokenLength;
    }
}
