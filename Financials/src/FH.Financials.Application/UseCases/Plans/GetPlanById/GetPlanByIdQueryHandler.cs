using FH.Financials.Application.Common;
using FH.Financials.Application.Contracts.Persistence;
using FH.Financials.Application.UseCases.Plans.Common;

namespace FH.Financials.Application.UseCases.Plans.GetPlanById;

/// <summary>Obtiene un plan por id (lo consume el gateway de Customers para validar external_plan_id).</summary>
public sealed record GetPlanByIdQuery(Guid Id);

public sealed class GetPlanByIdQueryHandler(IPlanRepository planRepository)
{
    public async Task<Result<PlanResponse>> HandleAsync(GetPlanByIdQuery query, CancellationToken cancellationToken)
    {
        var plan = await planRepository.GetByIdAsync(query.Id, cancellationToken);
        if (plan is null)
        {
            return Result<PlanResponse>.NotFound($"No existe el plan {query.Id} en el catálogo.");
        }

        return Result<PlanResponse>.Success(
            new PlanResponse(plan.Id, plan.Name, plan.Description, plan.BaseMonthlyFee, plan.MaxBeneficiaries, plan.Status, plan.CreatedAtUtc));
    }
}
