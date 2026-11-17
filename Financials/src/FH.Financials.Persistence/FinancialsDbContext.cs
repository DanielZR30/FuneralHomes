using FH.Financials.Domain.Entities;
using FH.Financials.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FH.Financials.Persistence;

/// <summary>DbContext del microservicio Financials (fh_db.financial). Un solo agregado: Plan.</summary>
public class FinancialsDbContext(DbContextOptions<FinancialsDbContext> options) : DbContext(options)
{
    public DbSet<Plan> Plans => Set<Plan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinancialsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
