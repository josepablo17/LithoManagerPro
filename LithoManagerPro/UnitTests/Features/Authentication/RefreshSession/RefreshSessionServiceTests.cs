using LithoManager.Application.Features.Authentication.Login;
using LithoManager.Application.Features.Authentication
    .RefreshSession;
using LithoManager.Application.Features.Authentication
    .RefreshTokens;
using LithoManager.UnitTests.TestDoubles.Persistence;
using LithoManager.UnitTests.TestDoubles.Security;
using LithoManager.UnitTests.TestDoubles.Time;
using Xunit;

namespace LithoManager.UnitTests.Features.Authentication
    .RefreshSession;

public sealed class RefreshSessionServiceTests
{
    private static readonly DateTimeOffset UtcNow =
        new(
            year: 2026,
            month: 8,
            day: 1,
            hour: 6,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

    [Fact]
    public async Task RefreshAsync_WhenTokenIsValid_ReturnsNewSession()
    {
        byte[] currentHash =
            Enumerable
                .Repeat((byte)1, 32)
                .ToArray();

        byte[] newHash =
            Enumerable
                .Repeat((byte)2, 32)
                .ToArray();

        RefreshTokenContextData context =
            CreateContext();

        FakeAuthenticationRepository repository =
            new()
            {
                RefreshTokenContextToReturn = context,
                RotateRefreshTokenToReturn =
                    new()
                    {
                        CurrentRefreshTokenId =
                            context.RefreshTokenId,
                        NewRefreshTokenId = 2,
                        UserId = context.UserId,
                        TokenFamilyId =
                            context.TokenFamilyId,
                        ExpiresAtUtc =
                            UtcNow
                                .AddDays(1)
                                .UtcDateTime,
                        RotatedAtUtc =
                            UtcNow.UtcDateTime,
                        WasRotated = true,
                        FailureReason = null
                    }
            };

        FakeRefreshTokenService refreshTokenService =
            new()
            {
                ComputedHashToReturn = currentHash,
                GeneratedTokenToReturn =
                    new(
                        token:
                            "new-refresh-token",
                        tokenHash:
                            newHash)
            };

        FakeTokenService tokenService =
            new();

        RefreshSessionService service =
            CreateService(
                repository,
                refreshTokenService,
                tokenService);

        AuthenticationRequestContext requestContext =
            CreateRequestContext();

        RefreshSessionResult result =
            await service.RefreshAsync(
                new RefreshSessionCommand(
                    RefreshToken:
                        "current-refresh-token",
                    RequestContext:
                        requestContext),
                CancellationToken.None);

        Assert.True(result.IsSuccessful);
        Assert.Equal(
            RefreshSessionErrorCode.None,
            result.ErrorCode);
        Assert.Equal(
            "fake-access-token",
            result.AccessToken);
        Assert.Equal(
            "new-refresh-token",
            result.RefreshToken);
        Assert.Equal(
            UtcNow.AddDays(1).UtcDateTime,
            result.RefreshTokenExpiresAtUtc);
        Assert.NotNull(result.User);
        Assert.Equal(
            context.UserId,
            result.User!.UserId);

        Assert.Equal(
            1,
            refreshTokenService.ComputeTokenHashCallCount);
        Assert.Equal(
            "current-refresh-token",
            refreshTokenService.LastTokenToHash);
        Assert.Equal(
            1,
            refreshTokenService.GenerateTokenCallCount);

        Assert.Equal(
            1,
            repository.GetRefreshTokenContextCallCount);
        Assert.Equal(
            currentHash,
            repository.LastRefreshTokenContextHash);
        Assert.Equal(
            1,
            repository.RotateRefreshTokenCallCount);
        Assert.Equal(
            currentHash,
            repository.LastCurrentRefreshTokenHash);
        Assert.Equal(
            newHash,
            repository.LastNewRefreshTokenHash);
        Assert.Same(
            requestContext,
            repository.LastRotateRefreshTokenRequestContext);

        Assert.Equal(
            1,
            tokenService.GenerateAccessTokenCallCount);
        Assert.Equal(
            context.UserId,
            tokenService.AccessTokenUserReceived!.UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RefreshAsync_WhenTokenIsInvalid_ReturnsInvalidRequest(
        string? refreshToken)
    {
        FakeAuthenticationRepository repository =
            new();

        FakeRefreshTokenService refreshTokenService =
            new();

        RefreshSessionService service =
            CreateService(
                repository,
                refreshTokenService,
                new FakeTokenService());

        RefreshSessionResult result =
            await service.RefreshAsync(
                new RefreshSessionCommand(
                    RefreshToken:
                        refreshToken,
                    RequestContext:
                        CreateRequestContext()),
                CancellationToken.None);

        Assert.False(result.IsSuccessful);
        Assert.Equal(
            RefreshSessionErrorCode.InvalidRequest,
            result.ErrorCode);
        Assert.Equal(
            0,
            refreshTokenService.ComputeTokenHashCallCount);
        Assert.Equal(
            0,
            repository.GetRefreshTokenContextCallCount);
    }

    [Fact]
    public async Task RefreshAsync_WhenContextIsMissing_ReturnsInvalidRefreshToken()
    {
        FakeAuthenticationRepository repository =
            new()
            {
                RefreshTokenContextToReturn = null
            };

        FakeRefreshTokenService refreshTokenService =
            new();

        FakeTokenService tokenService =
            new();

        RefreshSessionService service =
            CreateService(
                repository,
                refreshTokenService,
                tokenService);

        RefreshSessionResult result =
            await service.RefreshAsync(
                new RefreshSessionCommand(
                    RefreshToken:
                        "current-refresh-token",
                    RequestContext:
                        CreateRequestContext()),
                CancellationToken.None);

        Assert.False(result.IsSuccessful);
        Assert.Equal(
            RefreshSessionErrorCode.InvalidRefreshToken,
            result.ErrorCode);
        Assert.Equal(
            0,
            repository.RotateRefreshTokenCallCount);
        Assert.Equal(
            0,
            tokenService.GenerateAccessTokenCallCount);
    }

    [Fact]
    public async Task RefreshAsync_WhenRotationFails_ReturnsInvalidRefreshToken()
    {
        FakeAuthenticationRepository repository =
            new()
            {
                RefreshTokenContextToReturn =
                    CreateContext(),
                RotateRefreshTokenToReturn =
                    new()
                    {
                        WasRotated = false,
                        FailureReason =
                            "ReuseDetected"
                    }
            };

        FakeTokenService tokenService =
            new();

        RefreshSessionService service =
            CreateService(
                repository,
                new FakeRefreshTokenService(),
                tokenService);

        RefreshSessionResult result =
            await service.RefreshAsync(
                new RefreshSessionCommand(
                    RefreshToken:
                        "current-refresh-token",
                    RequestContext:
                        CreateRequestContext()),
                CancellationToken.None);

        Assert.False(result.IsSuccessful);
        Assert.Equal(
            RefreshSessionErrorCode.InvalidRefreshToken,
            result.ErrorCode);
        Assert.Equal(
            1,
            repository.RotateRefreshTokenCallCount);
        Assert.Equal(
            0,
            tokenService.GenerateAccessTokenCallCount);
    }

    private static RefreshSessionService CreateService(
        FakeAuthenticationRepository repository,
        FakeRefreshTokenService refreshTokenService,
        FakeTokenService tokenService)
    {
        return new RefreshSessionService(
            repository,
            refreshTokenService,
            tokenService,
            new LithoManager.Application.Features.Authentication
                .AuthenticationSessionOptions
            {
                RefreshTokenExpirationDays = 1
            },
            new FixedTimeProvider(UtcNow));
    }

    private static AuthenticationRequestContext
        CreateRequestContext()
    {
        return new AuthenticationRequestContext(
            CorrelationId: Guid.NewGuid(),
            ClientIpAddress: "127.0.0.1",
            UserAgent: "LithoManager.UnitTests",
            RequestPath: "/api/auth/refresh");
    }

    private static RefreshTokenContextData CreateContext()
    {
        return new RefreshTokenContextData
        {
            RefreshTokenId = 1,
            UserId = 7,
            TokenFamilyId = Guid.NewGuid(),
            RefreshTokenVersion = 3,
            ExpiresAtUtc =
                UtcNow.AddDays(1).UtcDateTime,
            CreatedAtUtc =
                UtcNow.AddDays(-1).UtcDateTime,
            LastUsedAtUtc = null,
            EmailAddress =
                "user@lithomanager.com",
            TokenVersion = 3,
            IsEmailConfirmed = true,
            IsActive = true,
            RequiresPasswordChange = false,
            RoleId = 4,
            RoleCode = "Employee",
            RoleDisplayName = "Employee",
            IsRoleActive = true,
            EmployeeId = 10,
            FirstName = "Ana",
            LastName = "Rodriguez",
            JobTitle = "Business Analyst",
            ProfileImagePath = null,
            IsEmployeeActive = true,
            DepartmentId = 2,
            DepartmentCode = "HR",
            DepartmentName = "Human Resources",
            IsDepartmentActive = true
        };
    }
}
