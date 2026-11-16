using System.Security.Claims;
using System.Text;
using Identity.App.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Identity.App.Services;

/// <summary>
/// Implementación del emisor de JWT. El contrato de claims y firma es compartido por los tres
/// microservicios (sección "Jwt" del appsettings): issuer fh.api.identity, audiencia fh-apis,
/// algoritmo HS256 y ClockSkew cero en la validación de los consumidores.
/// </summary>
public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public (string Token, DateTime ExpiresAtUtc) CreateToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(configuration.GetValue("Jwt:ExpiresMinutes", 60.0));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("name", user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]
                ?? throw new InvalidOperationException("Falta la configuración Jwt:SigningKey.")));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(claims)
        };

        // JsonWebTokenHandler es el handler moderno (JwtSecurityTokenHandler está obsoleto)
        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expiresAt);
    }
}
