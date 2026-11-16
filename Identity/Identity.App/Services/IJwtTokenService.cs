using Identity.App.Data;

namespace Identity.App.Services;

/// <summary>
/// Emite los JWT de la plataforma: firma HMAC-SHA256, vigencia configurable y claims
/// sub/email/name/role/jti para que los demás microservicios autoricen por roles.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Construye el token del usuario con sus roles. Devuelve el JWT y su fecha de expiración (UTC).</summary>
    (string Token, DateTime ExpiresAtUtc) CreateToken(ApplicationUser user, IEnumerable<string> roles);
}
