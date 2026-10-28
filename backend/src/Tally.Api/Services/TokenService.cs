using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Tally.Api.Models;

namespace Tally.Api.Services;

// short claim names, inbound claim mapping is turned off in Program.cs
public static class TallyClaims
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
}

public class JwtOptions
{
    public string Issuer { get; set; } = "tally";
    public string Audience { get; set; } = "tally-client";
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 120;
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(AppUser user, IEnumerable<string> roles);
}

public class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTime ExpiresAtUtc) CreateToken(AppUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(TallyClaims.UserId, user.Id),
            new(TallyClaims.Email, user.Email ?? string.Empty),
            new(TallyClaims.Name, user.DisplayName),
        };
        claims.AddRange(roles.Select(r => new Claim(TallyClaims.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expires,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
