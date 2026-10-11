using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Perfume.Infrastructure.Identity;

namespace Perfume.Web.Security;

public sealed class JwtTokenService
{
    private readonly byte[] _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenService(IConfiguration configuration, IHostEnvironment environment, ILogger<JwtTokenService> logger)
    {
        _issuer = configuration["Jwt:Issuer"] ?? "PerfumeLuxe";
        _audience = configuration["Jwt:Audience"] ?? "PerfumeLuxe.Clients";
        var configuredKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Jwt:SigningKey must be set to a secret of at least 32 bytes outside Development.");
            _key = RandomNumberGenerator.GetBytes(64);
            logger.LogWarning("Jwt:SigningKey is not configured. Development JWTs will be invalid after an app restart.");
        }
        else
        {
            _key = Encoding.UTF8.GetBytes(configuredKey);
            if (_key.Length < 32) throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");
        }
    }

    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(AppUser user, IEnumerable<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_issuer, _audience, claims, DateTime.UtcNow, expiresAt, credentials);
        return (_handler.WriteToken(token), expiresAt);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        try
        {
            var principal = _handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(_key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            }, out var validatedToken);
            if (validatedToken is not JwtSecurityToken jwt || jwt.Header.Alg != SecurityAlgorithms.HmacSha256) return null;
            return principal;
        }
        catch (SecurityTokenException) { return null; }
        catch (ArgumentException) { return null; }
    }
}
