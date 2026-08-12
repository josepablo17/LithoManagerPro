using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Authentication.Logout;
using LithoManager.Application.Features.Authentication
    .RefreshTokens;
using LithoManager.UnitTests.TestDoubles.Persistence;
using LithoManager.UnitTests.TestDoubles.Security;
using Xunit;

namespace LithoManager.UnitTests.Features.Authentication.Logout;

public sealed class LogoutServiceTests
{
    [Fact]
    public async Task LogoutAsync_WhenTokenIsValid_RevokesRefreshToken()
    {
        byte[] tokenHash =
            Enumerable
                .Repeat((byte)5, 32)
                .ToArray();

        DateTime revokedAtUtc =
            new(
                year: 2026,
                month: 8,
                day: 1,
                hour: 6,
                minute: 0,
                second: 0,
                kind: DateTimeKind.Utc);

        FakeAuthenticationRepository repository =
            new()
            {
                RevokeRefreshTokenToReturn =
                    new RevokeRefreshTokenData
                    {
                        RefreshTokenId = 1,
                        UserId = 7,
                        TokenFamilyId =
                            Guid.NewGuid(),
                        RevokedAtUtc =
                            revokedAtUtc,
                        WasRevoked = true,
                        WasAlreadyInactive = false
                    }
            };

        FakeRefreshTokenService refreshTokenService =
            new()
            {
                ComputedHashToReturn = tokenHash
            };

        LogoutService service =
            new(
                repository,
                refreshTokenService);

        AuthenticationRequestContext requestContext =
            CreateRequestContext();

        LogoutResult result =
            await service.LogoutAsync(
                new LogoutCommand(
                    RefreshToken:
                        "refresh-token",
                    RequestContext:
                        requestContext),
                CancellationToken.None);

        Assert.True(result.IsSuccessful);
        Assert.True(result.WasRevoked);
        Assert.Equal(1, result.RevokedCount);
        Assert.Equal(7, result.UserId);
        Assert.Equal(
            revokedAtUtc,
            result.RevokedAtUtc);

        Assert.Equal(
            1,
            refreshTokenService.ComputeTokenHashCallCount);
        Assert.Equal(
            "refresh-token",
            refreshTokenService.LastTokenToHash);
        Assert.Equal(
            1,
            repository.RevokeRefreshTokenCallCount);
        Assert.Equal(
            tokenHash,
            repository.LastRevokedRefreshTokenHash);
        Assert.Equal(
            "Logout",
            repository.LastRefreshTokenRevokedReason);
        Assert.Same(
            requestContext,
            repository.LastRevokeRefreshTokenRequestContext);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LogoutAsync_WhenTokenIsInvalid_ReturnsInvalidRequest(
        string? refreshToken)
    {
        FakeAuthenticationRepository repository =
            new();

        FakeRefreshTokenService refreshTokenService =
            new();

        LogoutService service =
            new(
                repository,
                refreshTokenService);

        LogoutResult result =
            await service.LogoutAsync(
                new LogoutCommand(
                    RefreshToken:
                        refreshToken,
                    RequestContext:
                        CreateRequestContext()),
                CancellationToken.None);

        Assert.False(result.IsSuccessful);
        Assert.Equal(
            LogoutErrorCode.InvalidRequest,
            result.ErrorCode);
        Assert.Equal(
            0,
            refreshTokenService.ComputeTokenHashCallCount);
        Assert.Equal(
            0,
            repository.RevokeRefreshTokenCallCount);
    }

    [Fact]
    public async Task RevokeUserSessionsAsync_WhenCommandIsValid_RevokesUserSessions()
    {
        DateTime revokedAtUtc =
            new(
                year: 2026,
                month: 8,
                day: 1,
                hour: 6,
                minute: 0,
                second: 0,
                kind: DateTimeKind.Utc);

        FakeAuthenticationRepository repository =
            new()
            {
                RevokeUserRefreshTokensToReturn =
                    new RevokeUserRefreshTokensData
                    {
                        UserId = 7,
                        RevokedAtUtc =
                            revokedAtUtc,
                        RevokedCount = 1,
                        WasRevoked = true
                    }
            };

        LogoutService service =
            new(
                repository,
                new FakeRefreshTokenService());

        AuthenticationRequestContext requestContext =
            CreateRequestContext();

        LogoutResult result =
            await service.RevokeUserSessionsAsync(
                new RevokeUserSessionsCommand(
                    UserId: 7,
                    RevokedReason:
                        "UserDeactivated",
                    ActorUserId: 1,
                    RequestContext:
                        requestContext),
                CancellationToken.None);

        Assert.True(result.IsSuccessful);
        Assert.True(result.WasRevoked);
        Assert.Equal(1, result.RevokedCount);
        Assert.Equal(7, result.UserId);
        Assert.Equal(
            revokedAtUtc,
            result.RevokedAtUtc);

        Assert.Equal(
            1,
            repository.RevokeUserRefreshTokensCallCount);
        Assert.Equal(
            7,
            repository.LastRefreshTokensRevocationUserId);
        Assert.Equal(
            1,
            repository.LastRefreshTokensRevocationActorUserId);
        Assert.Equal(
            "UserDeactivated",
            repository.LastUserRefreshTokensRevokedReason);
        Assert.Same(
            requestContext,
            repository
                .LastRevokeUserRefreshTokensRequestContext);
    }

    private static AuthenticationRequestContext
        CreateRequestContext()
    {
        return new AuthenticationRequestContext(
            CorrelationId: Guid.NewGuid(),
            ClientIpAddress: "127.0.0.1",
            UserAgent: "LithoManager.UnitTests",
            RequestPath: "/api/auth/logout");
    }
}
