using LithoManager.Application.Abstractions.Security;

namespace LithoManager.UnitTests.TestDoubles.Security;

public sealed class FakeTokenService
    : ITokenService
{
    public AccessTokenResult AccessTokenToReturn
    {
        get;
        set;
    } = new(
        AccessToken: "fake-access-token",
        ExpiresAtUtc:
            new DateTimeOffset(
                2026,
                8,
                1,
                7,
                0,
                0,
                TimeSpan.Zero));

    public PasswordChangeTokenResult
        PasswordChangeTokenToReturn
    {
        get;
        set;
    } = new(
        Token: "fake-password-change-token",
        ExpiresAtUtc:
            new DateTimeOffset(
                2026,
                8,
                1,
                6,
                10,
                0,
                TimeSpan.Zero));

    public int GenerateAccessTokenCallCount
    {
        get;
        private set;
    }

    public AccessTokenUserData?
        AccessTokenUserReceived
    {
        get;
        private set;
    }

    public int GeneratePasswordChangeTokenCallCount
    {
        get;
        private set;
    }

    public PasswordChangeTokenUserData?
        PasswordChangeTokenUserReceived
    {
        get;
        private set;
    }

    public AccessTokenResult GenerateAccessToken(
        AccessTokenUserData user)
    {
        GenerateAccessTokenCallCount++;

        AccessTokenUserReceived =
            user;

        return AccessTokenToReturn;
    }

    public PasswordChangeTokenResult
        GeneratePasswordChangeToken(
            PasswordChangeTokenUserData user)
    {
        GeneratePasswordChangeTokenCallCount++;

        PasswordChangeTokenUserReceived =
            user;

        return PasswordChangeTokenToReturn;
    }
}