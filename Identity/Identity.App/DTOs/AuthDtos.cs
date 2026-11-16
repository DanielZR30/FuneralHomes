namespace Identity.App.DTOs;

/// <summary>Credenciales para el login. La validación de formato la hace Identity + FluentValidation del controller.</summary>
public record LoginRequest(string Email, string Password);

/// <summary>Respuesta exitosa del login: token listo para usar como "Bearer {accessToken}".</summary>
public record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    UserResponse User);

/// <summary>Vista pública del usuario autenticado.</summary>
public record UserResponse(string Id, string Email, string FullName, IReadOnlyList<string> Roles);
