using FH.Financials.Domain.Enums;

namespace FH.Financials.Application.UseCases.Plans.Common;

/// <summary>Cotización de la cuota mensual: cuota base + recargos por beneficiario.</summary>
public sealed record QuoteResponse(
    Guid PlanId,
    string PlanName,
    decimal BaseMonthlyFee,
    IReadOnlyList<QuoteSurchargeResponse> Surcharges,
    decimal TotalMonthlyFee);

/// <summary>Recargo de un beneficiario dentro de la cotización.</summary>
public sealed record QuoteSurchargeResponse(
    SubjectType SubjectType,
    int DerivedAge,
    RelationshipType RelationshipType,
    decimal Rate,
    decimal Surcharge);
