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
    private const string EmployeeIdClaimType = "employee_id";

    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SigningCredentials _signingCredentials;

    public TokenService(
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(jwtOptions);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
        _tokenHandler = new JwtSecurityTokenHandler();

        byte[] signingKeyBytes =
            Convert.FromBase64String(_jwtOptions.SigningKeyBase64);

        SymmetricSecurityKey signingKey =
            new(signingKeyBytes);

        _signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);
    }

    public AccessTokenResult GenerateAccessToken(
        AccessTokenUserData user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(user),
                "UserId must be greater than zero.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            user.EmailAddress);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            user.RoleCode);

        DateTimeOffset issuedAtUtc =
            _timeProvider.GetUtcNow();

        DateTimeOffset expiresAtUtc =
            issuedAtUtc.AddMinutes(
                _jwtOptions.AccessTokenExpirationMinutes);

        List<Claim> claims =
        [
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.UserId.ToString(CultureInfo.InvariantCulture)),

            new Claim(
                JwtRegisteredClaimNames.Email,
                user.EmailAddress),

            new Claim(
                RoleClaimType,
                user.RoleCode),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("N"))
        ];

        if (user.EmployeeId is int employeeId)
        {
            if (employeeId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(user),
                    "EmployeeId must be greater than zero when provided.");
            }

            claims.Add(
                new Claim(
                    EmployeeIdClaimType,
                    employeeId.ToString(
                        CultureInfo.InvariantCulture)));
        }

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            IssuedAt = issuedAtUtc.UtcDateTime,
            NotBefore = issuedAtUtc.UtcDateTime,
            Expires = expiresAtUtc.UtcDateTime,
            SigningCredentials = _signingCredentials
        };

        SecurityToken securityToken =
            _tokenHandler.CreateToken(tokenDescriptor);

        string accessToken =
            _tokenHandler.WriteToken(securityToken);

        return new AccessTokenResult(
            accessToken,
            expiresAtUtc);
    }
}