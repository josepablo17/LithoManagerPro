using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LithoManager.Application.Abstractions.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LithoManager.Infrastructure.Security.Tokens;

internal sealed class TokenService : ITokenService
{
    private const string RoleClaimType = "role";

    private const string EmployeeIdClaimType =
        "employee_id";

    private const string TokenUseClaimType =
        "token_use";

    private const string TokenVersionClaimType =
        "token_version";

    private const string AccessTokenUse =
        "access";

    private const string PasswordChangeTokenUse =
        "password_change";

    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;

    private readonly JwtSecurityTokenHandler
        _tokenHandler;

    private readonly SigningCredentials
        _signingCredentials;

    public TokenService(
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            jwtOptions);

        ArgumentNullException.ThrowIfNull(
            timeProvider);

        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;

        _tokenHandler =
            new JwtSecurityTokenHandler();

        byte[] signingKeyBytes =
            Convert.FromBase64String(
                _jwtOptions.SigningKeyBase64);

        SymmetricSecurityKey signingKey =
            new(signingKeyBytes);

        _signingCredentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);
    }

    public AccessTokenResult GenerateAccessToken(
        AccessTokenUserData user)
    {
        ArgumentNullException.ThrowIfNull(user);

        ValidateUserData(
            user.UserId,
            user.EmailAddress,
            user.TokenVersion);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            user.RoleCode);

        List<Claim> claims =
            CreateBaseClaims(
                user.UserId,
                user.EmailAddress,
                user.TokenVersion,
                AccessTokenUse);

        claims.Add(
            new Claim(
                RoleClaimType,
                user.RoleCode));

        if (user.EmployeeId is int employeeId)
        {
            if (employeeId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(user),
                    "EmployeeId must be greater than zero.");
            }

            claims.Add(
                new Claim(
                    EmployeeIdClaimType,
                    employeeId.ToString(
                        CultureInfo.InvariantCulture)));
        }

        GeneratedToken generatedToken =
            CreateToken(
                claims,
                _jwtOptions
                    .AccessTokenExpirationMinutes);

        return new AccessTokenResult(
            generatedToken.Token,
            generatedToken.ExpiresAtUtc);
    }

    public PasswordChangeTokenResult
        GeneratePasswordChangeToken(
            PasswordChangeTokenUserData user)
    {
        ArgumentNullException.ThrowIfNull(user);

        ValidateUserData(
            user.UserId,
            user.EmailAddress,
            user.TokenVersion);

        List<Claim> claims =
            CreateBaseClaims(
                user.UserId,
                user.EmailAddress,
                user.TokenVersion,
                PasswordChangeTokenUse);

        GeneratedToken generatedToken =
            CreateToken(
                claims,
                _jwtOptions
                    .PasswordChangeTokenExpirationMinutes);

        return new PasswordChangeTokenResult(
            generatedToken.Token,
            generatedToken.ExpiresAtUtc);
    }

    private List<Claim> CreateBaseClaims(
        int userId,
        string emailAddress,
        int tokenVersion,
        string tokenUse)
    {
        return
        [
            new Claim(
                JwtRegisteredClaimNames.Sub,
                userId.ToString(
                    CultureInfo.InvariantCulture)),

            new Claim(
                JwtRegisteredClaimNames.Email,
                emailAddress),

            new Claim(
                TokenVersionClaimType,
                tokenVersion.ToString(
                    CultureInfo.InvariantCulture)),

            new Claim(
                TokenUseClaimType,
                tokenUse),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("N"))
        ];
    }

    private GeneratedToken CreateToken(
        IEnumerable<Claim> claims,
        int expirationMinutes)
    {
        DateTimeOffset issuedAtUtc =
            _timeProvider.GetUtcNow();

        DateTimeOffset expiresAtUtc =
            issuedAtUtc.AddMinutes(
                expirationMinutes);

        SecurityTokenDescriptor descriptor =
            new()
            {
                Subject =
                    new ClaimsIdentity(claims),

                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,

                IssuedAt =
                    issuedAtUtc.UtcDateTime,

                NotBefore =
                    issuedAtUtc.UtcDateTime,

                Expires =
                    expiresAtUtc.UtcDateTime,

                SigningCredentials =
                    _signingCredentials
            };

        SecurityToken securityToken =
            _tokenHandler.CreateToken(descriptor);

        string token =
            _tokenHandler.WriteToken(
                securityToken);

        return new GeneratedToken(
            token,
            expiresAtUtc);
    }

    private static void ValidateUserData(
        int userId,
        string emailAddress,
        int tokenVersion)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "UserId must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            emailAddress);

        if (tokenVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tokenVersion),
                "TokenVersion must be greater than zero.");
        }
    }

    private sealed record GeneratedToken(
        string Token,
        DateTimeOffset ExpiresAtUtc);
}
