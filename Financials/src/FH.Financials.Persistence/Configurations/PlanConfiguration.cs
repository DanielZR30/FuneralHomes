using FH.Financials.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FH.Financials.Persistence.Configurations;

/// <summary>Mapeo de la tabla plans (columnas snake_case, como el esquema de Customers).</summary>
public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(p => p.Name).HasColumnName("name").HasColumnType("varchar(100)").IsRequired();
        builder.HasIndex(p => p.Name).IsUnique().HasDatabaseName("ix_plans_name_unique");

        builder.Property(p => p.Description).HasColumnName("description").HasColumnType("varchar(300)");

        builder.Property(p => p.BaseMonthlyFee).HasColumnName("base_monthly_fee").HasColumnType("numeric(18,2)").IsRequired();

        builder.Property(p => p.MaxBeneficiaries).HasColumnName("max_beneficiaries").IsRequired();

        builder.Property(p => p.Status).HasColumnName("status").HasColumnType("varchar(20)").IsRequired()
            .HasConversion(
                status => status.ToString(),
                value => Enum.Parse<FH.Financials.Domain.Enums.PlanStatus>(value));

        builder.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
    }
}
