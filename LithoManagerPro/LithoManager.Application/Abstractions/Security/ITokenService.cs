namespace LithoManager.Application.Abstractions.Security;

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(
        AccessTokenUserData user);
}