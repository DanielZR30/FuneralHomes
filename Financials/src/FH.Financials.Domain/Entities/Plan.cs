using FH.Financials.Domain.Enums;
using FH.Financials.Domain.Exceptions;

namespace FH.Financials.Domain.Entities;

/// <summary>
/// Plan comercial del catálogo de Funeral Homes. Financials es el único dueño de la validez
/// comercial y las tarifas: Customers solo guarda el identificador externo (external_plan_id).
/// </summary>
public class Plan
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 300;
    public const int MaxBeneficiariesUpperBound = 100;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal BaseMonthlyFee { get; private set; }
    public int MaxBeneficiaries { get; private set; }
    public PlanStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Plan() { } // EF Core

    /// <summary>Crea un plan activo validando las reglas del catálogo.</summary>
    /// <exception cref="BusinessRuleException">Nombre vacío/muy largo, cuota no positiva o cupo fuera de rango.</exception>
    public static Plan Create(string name, string? description, decimal baseMonthlyFee, int maxBeneficiaries)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleException("El nombre del plan es obligatorio.");
        }
        if (name.Length > NameMaxLength)
        {
            throw new BusinessRuleException($"El nombre del plan no puede exceder {NameMaxLength} caracteres.");
        }
        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > DescriptionMaxLength)
        {
            throw new BusinessRuleException($"La descripción no puede exceder {DescriptionMaxLength} caracteres.");
        }
        if (baseMonthlyFee <= 0)
        {
            throw new BusinessRuleException("La cuota base mensual debe ser mayor que cero.");
        }
        if (maxBeneficiaries < 1 || maxBeneficiaries > MaxBeneficiariesUpperBound)
        {
            throw new BusinessRuleException($"El cupo de beneficiarios debe estar entre 1 y {MaxBeneficiariesUpperBound}.");
        }

        return new Plan
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            BaseMonthlyFee = decimal.Round(baseMonthlyFee, 2),
            MaxBeneficiaries = maxBeneficiaries,
            Status = PlanStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Deactivate() => Status = PlanStatus.Inactive;

    public void Reactivate() => Status = PlanStatus.Active;

    /// <summary>
    /// Fábrica para seeders: igual que Create pero con id fijo, para que el external_plan_id que
    /// guarda Customers siempre resuelva contra el catálogo sembrado.
    /// </summary>
    public static Plan CreateSeeded(Guid id, string name, string? description, decimal baseMonthlyFee, int maxBeneficiaries)
    {
        var plan = Create(name, description, baseMonthlyFee, maxBeneficiaries);
        typeof(Plan).GetProperty(nameof(Id))!.SetValue(plan, id);
        return plan;
    }
}
