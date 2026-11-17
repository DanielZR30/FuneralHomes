using FH.Financials.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FH.Financials.Persistence.Seeds;

/// <summary>
/// Seeder idempotente del catálogo (solo Development). El plan "Familiar Esencial" usa el GUID
/// que el seeder de Customers ya referencia como external_plan_id.
/// </summary>
public static class PlanSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("PlanSeeder");
        var context = scope.ServiceProvider.GetRequiredService<FinancialsDbContext>();

        try
        {
            if (await context.Plans.AsNoTracking().AnyAsync())
            {
                return; // ya hay catálogo: no se siembra dos veces
            }

            context.Plans.AddRange(
                Plan.CreateSeeded(
                    Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    "Plan Familiar Esencial",
                    "Titular + grupo familiar humano y mascotas, cuota con recargo por edad derivada.",
                    50000m, 4),
                Plan.CreateSeeded(
                    Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    "Plan Corporativo Oro",
                    "Empresas: empleados y sus familias, facturación mensual por suscripción.",
                    250000m, 25),
                Plan.CreateSeeded(
                    Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    "Plan Mascotas",
                    "Solo mascotas del titular, recargo fijo por beneficiario.",
                    30000m, 3));

            await context.SaveChangesAsync();
            logger.LogInformation("Catálogo sembrado con 3 planes");
        }
        catch (Exception ex)
        {
            // El seeder nunca debe tumbar el arranque del servicio
            logger.LogWarning(ex, "Error al sembrar el catálogo de planes.");
        }
    }
}
