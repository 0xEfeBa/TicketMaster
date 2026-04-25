using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Identity.Application.Abstractions;
using Identity.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure.Security;

public class JwtTokenIssuer(IOptions<JwtOptions> options) : IJwtTokenIssuer
{
    private readonly JwtOptions _options = options.Value;

    public AccessTokenResult CreateAccessToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey))
            throw new InvalidOperationException("Jwt:SigningKey yapılandırılmadı.");

        var expiresInSeconds = Math.Max(60, _options.AccessTokenMinutes * 60);
        var expires = DateTime.UtcNow.AddSeconds(expiresInSeconds);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(_options.Issuer) ? null : _options.Issuer,
            audience: string.IsNullOrWhiteSpace(_options.Audience) ? null : _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessTokenResult(jwt, expiresInSeconds, jti);
    }
}
