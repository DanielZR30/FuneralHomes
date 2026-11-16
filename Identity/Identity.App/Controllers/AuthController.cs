using Identity.App.Data;
using Identity.App.DTOs;
using Identity.App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Identity.App.Controllers;

/// <summary>
/// Autenticación de la plataforma: verifica credenciales contra ASP.NET Core Identity y emite
/// el JWT que los demás microservicios (Customers, Financials) validan para proteger sus endpoints.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Tags("Auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService tokenService) : ControllerBase
{
    /// <summary>Autentica con credenciales y emite un JWT firmado con los claims del usuario y sus roles.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "El email y la contraseña son obligatorios." });
        }

        var user = await userManager.FindByEmailAsync(request.Email);

        // CheckPasswordAsync evita el lockout: para la demo interesa ver 401 cuantas veces haga falta
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            // Sin revelar qué falló: credenciales inválidas genéricas
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        var roles = await userManager.GetRolesAsync(user);
        var (token, expiresAtUtc) = tokenService.CreateToken(user, roles);

        return Ok(new LoginResponse(
            token,
            "Bearer",
            expiresAtUtc,
            new UserResponse(user.Id, user.Email!, user.FullName, [.. roles])));
    }

    /// <summary>Devuelve el usuario del token (requiere autenticación: sirve para demostrar la validación del JWT).</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var id = User.FindFirst("sub")?.Value ?? string.Empty;
        var email = User.FindFirst("email")?.Value ?? string.Empty;
        var fullName = User.FindFirst("name")?.Value ?? string.Empty;
        var roles = User.FindAll("role").Select(c => c.Value).ToList();

        return Ok(new UserResponse(id, email, fullName, roles));
    }
}
