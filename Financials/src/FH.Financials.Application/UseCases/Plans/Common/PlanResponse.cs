using FH.Financials.Domain.Enums;

namespace FH.Financials.Application.UseCases.Plans.Common;

/// <summary>Vista pública de un plan del catálogo.</summary>
public sealed record PlanResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal BaseMonthlyFee,
    int MaxBeneficiaries,
    PlanStatus Status,
    DateTime CreatedAtUtc);
