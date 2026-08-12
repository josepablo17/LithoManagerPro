using LithoManager.Application.Abstractions.Persistence;
using LithoManager.Application.Abstractions.Security;
using LithoManager.Application.Features.Authentication;
using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Authentication
    .RefreshTokens;

namespace LithoManager.Application.Features.Authentication
    .RefreshSession;

public sealed class RefreshSessionService
    : IRefreshSessionService
{
    private const int MaximumRefreshTokenLength = 512;
    private readonly IAuthenticationRepository
        _authenticationRepository;

    private readonly IRefreshTokenService
        _refreshTokenService;

    private readonly ITokenService _tokenService;
    private readonly AuthenticationSessionOptions
        _sessionOptions;
    private readonly TimeProvider _timeProvider;

    public RefreshSessionService(
        IAuthenticationRepository authenticationRepository,
        IRefreshTokenService refreshTokenService,
        ITokenService tokenService,
        AuthenticationSessionOptions sessionOptions,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            authenticationRepository);
        ArgumentNullException.ThrowIfNull(
            refreshTokenService);
        ArgumentNullException.ThrowIfNull(tokenService);
        ArgumentNullException.ThrowIfNull(sessionOptions);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _authenticationRepository =
            authenticationRepository;
        _refreshTokenService = refreshTokenService;
        _tokenService = tokenService;
        _sessionOptions = sessionOptions;
        _timeProvider = timeProvider;
    }

    public async Task<RefreshSessionResult> RefreshAsync(
        RefreshSessionCommand command,
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
            return RefreshSessionResult.Failure(
                RefreshSessionErrorCode.InvalidRequest);
        }

        byte[] currentTokenHash =
            _refreshTokenService.ComputeTokenHash(
                refreshToken);

        RefreshTokenContextData? context =
            await _authenticationRepository
                .GetRefreshTokenContextByTokenHashAsync(
                    currentTokenHash,
                    cancellationToken);

        if (context is null)
        {
            return RefreshSessionResult.Failure(
                RefreshSessionErrorCode
                    .InvalidRefreshToken);
        }

        GeneratedRefreshToken newRefreshToken =
            _refreshTokenService.GenerateToken();

        DateTime refreshTokenExpiresAtUtc =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime
                .AddDays(
                    _sessionOptions
                        .RefreshTokenExpirationDays);

        RotateRefreshTokenData rotation =
            await _authenticationRepository
                .RotateRefreshTokenAsync(
                    currentTokenHash:
                        currentTokenHash,
                    newTokenHash:
                        newRefreshToken.TokenHash,
                    expiresAtUtc:
                        refreshTokenExpiresAtUtc,
                    requestContext:
                        command.RequestContext,
                    cancellationToken:
                        cancellationToken);

        if (!rotation.WasRotated)
        {
            return RefreshSessionResult.Failure(
                RefreshSessionErrorCode
                    .InvalidRefreshToken);
        }

        AccessTokenResult accessToken =
            _tokenService.GenerateAccessToken(
                new AccessTokenUserData(
                    UserId: context.UserId,
                    EmailAddress:
                        context.EmailAddress,
                    TokenVersion:
                        context.TokenVersion,
                    RoleCode:
                        context.RoleCode,
                    EmployeeId:
                        context.EmployeeId));

        return RefreshSessionResult.Success(
            MapLoginUser(context),
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc,
            newRefreshToken.Token,
            refreshTokenExpiresAtUtc);
    }

    private static bool IsValidRefreshToken(
        string refreshToken)
    {
        return !string.IsNullOrWhiteSpace(refreshToken)
            && refreshToken.Length
                <= MaximumRefreshTokenLength;
    }

    private static LoginUserData MapLoginUser(
        RefreshTokenContextData context)
    {
        return new LoginUserData(
            UserId: context.UserId,
            EmailAddress: context.EmailAddress,
            RoleCode: context.RoleCode,
            RoleDisplayName:
                context.RoleDisplayName,
            EmployeeId:
                context.EmployeeId,
            FirstName:
                context.FirstName,
            LastName:
                context.LastName,
            JobTitle:
                context.JobTitle,
            ProfileImagePath:
                context.ProfileImagePath,
            DepartmentId:
                context.DepartmentId,
            DepartmentCode:
                context.DepartmentCode,
            DepartmentName:
                context.DepartmentName);
    }
}
