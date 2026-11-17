using FH.Financials.Application.Common;
using FH.Financials.Application.Contracts.Persistence;
using FH.Financials.Application.UseCases.Plans.Common;
using FH.Financials.Domain.Enums;
using FH.Financials.Domain.Exceptions;
using FH.Financials.Domain.Services;
using QuoteSurchargeResponse = FH.Financials.Application.UseCases.Plans.Common.QuoteSurchargeResponse;

namespace FH.Financials.Application.UseCases.Plans.QuotePlanFee;

/// <summary>Beneficiario de entrada de la cotización.</summary>
public sealed record QuoteBeneficiaryRequest(SubjectType SubjectType, int DerivedAge, RelationshipType RelationshipType);

/// <summary>Cotiza la cuota mensual de un plan para un conjunto de beneficiarios (FeeCalculator de dominio).</summary>
public sealed record QuotePlanFeeQuery(Guid PlanId, IReadOnlyList<QuoteBeneficiaryRequest> Beneficiaries);

public sealed class QuotePlanFeeQueryHandler(IPlanRepository planRepository)
{
    public async Task<Result<QuoteResponse>> HandleAsync(QuotePlanFeeQuery query, CancellationToken cancellationToken)
    {
        var plan = await planRepository.GetByIdAsync(query.PlanId, cancellationToken);
        if (plan is null)
        {
            return Result<QuoteResponse>.NotFound($"No existe el plan {query.PlanId} en el catálogo.");
        }
        if (plan.Status != PlanStatus.Active)
        {
            return Result<QuoteResponse>.UnprocessableEntity($"El plan '{plan.Name}' no está activo y no se puede cotizar.");
        }
        if (query.Beneficiaries.Count == 0)
        {
            return Result<QuoteResponse>.Failure("Envíe al menos un beneficiario para cotizar.");
        }

        try
        {
            var surcharges = FeeCalculator.Calculate(
                plan.BaseMonthlyFee,
                query.Beneficiaries.Select(b => new QuoteBeneficiary(b.SubjectType, b.DerivedAge, b.RelationshipType)));

            var response = new QuoteResponse(
                plan.Id,
                plan.Name,
                plan.BaseMonthlyFee,
                [.. surcharges.Select(s => new QuoteSurchargeResponse(s.SubjectType, s.DerivedAge, s.RelationshipType, s.Rate, s.Surcharge))],
                plan.BaseMonthlyFee + surcharges.Sum(s => s.Surcharge));

            return Result<QuoteResponse>.Success(response);
        }
        catch (BusinessRuleException ex)
        {
            return Result<QuoteResponse>.UnprocessableEntity(ex.Message);
        }
    }
}
