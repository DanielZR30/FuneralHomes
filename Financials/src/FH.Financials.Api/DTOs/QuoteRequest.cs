using FH.Financials.Application.UseCases.Plans.QuotePlanFee;

namespace FH.Financials.Api.DTOs;

/// <summary>Cotización de la cuota mensual de un plan para una lista de beneficiarios.</summary>
public record QuoteRequest(IReadOnlyList<QuoteBeneficiaryRequest> Beneficiaries);
