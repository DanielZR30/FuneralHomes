using Identity.App.Data;
using Microsoft.AspNetCore.Identity;

namespace Identity.App.Seeds;

/// <summary>
/// Seeder de usuarios y roles iniciales (solo Development, idempotente). En esta entrega no se
/// requiere CRUD de usuarios ni roles: los cuatro usuarios semilla cubren los escenarios de
/// autenticación y autorización (401/403) de la demostración.
/// </summary>
public static class IdentitySeeder
{
    private sealed record SeedUser(string Email, string Password, string FullName, string Role);

    private static readonly string[] Roles = ["Admin", "Operator", "Auditor", "ServiceAccount"];

    private static readonly SeedUser[] Users =
    [
        new("admin@funeralhomes.local", "Admin*2026", "Administrador Funeral Homes", "Admin"),
        new("operator@funeralhomes.local", "Operator*2026", "Operador de Sucursal", "Operator"),
        new("auditor@funeralhomes.local", "Auditor*2026", "Auditor de Cumplimiento", "Auditor"),
        new("svc-customers@funeralhomes.local", "Svc*2026", "Cuenta de Servicio Customers", "ServiceAccount")
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        try
        {
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            foreach (var seed in Users)
            {
                if (await userManager.FindByEmailAsync(seed.Email) is not null)
                {
                    continue; // ya existe: el seeder es idempotente
                }

                var user = new ApplicationUser
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    EmailConfirmed = true,
                    FullName = seed.FullName
                };

                var created = await userManager.CreateAsync(user, seed.Password);
                if (created.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, seed.Role);
                    logger.LogInformation("Usuario semilla creado: {Email} ({Role})", seed.Email, seed.Role);
                }
                else
                {
                    logger.LogWarning("No se pudo crear {Email}: {Errors}",
                        seed.Email, string.Join("; ", created.Errors.Select(e => e.Description)));
                }
            }
        }
        catch (Exception ex)
        {
            // El seeder nunca debe tumbar el arranque del servicio
            logger.LogWarning(ex, "Error al sembrar usuarios/roles de Identity.");
        }
    }
}
