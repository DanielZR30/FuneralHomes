using FH.Financials.Application.Contracts.Persistence;
using FH.Financials.Persistence.Repositories;
using FH.Financials.Persistence.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FH.Financials.Persistence;

public static class DependencyInjection
{
    /// <summary>Registra la capa Persistence: DbContext (PostgreSQL), repositorio de planes y Unit of Work.</summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        var connectionString = configuration?.GetConnectionString("FinancialsDb")
            ?? "Host=localhost;Port=5432;Database=fh_db_financial;Username=postgres;Password=secret";

        var migrationsAssembly = typeof(DependencyInjection).Assembly.FullName;

        services.AddDbContext<FinancialsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(migrationsAssembly)));

        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
