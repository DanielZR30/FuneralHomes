using Microsoft.AspNetCore.Identity;

namespace Identity.App.Data;

/// <summary>
/// Usuario de la plataforma Funeral Homes. Extiende el usuario estándar de ASP.NET Core Identity
/// con el nombre completo para mostrar. Las contraseñas las gestiona Identity (hash + salt),
/// nunca se almacenan en texto plano.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public const int FullNameMaxLength = 150;

    public string FullName { get; set; } = string.Empty;
}
