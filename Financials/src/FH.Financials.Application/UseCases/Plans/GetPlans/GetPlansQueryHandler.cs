using FH.Financials.Application.Common;
using FH.Financials.Application.Contracts.Persistence;
using FH.Financials.Application.UseCases.Plans.Common;

namespace FH.Financials.Application.UseCases.Plans.GetPlans;

/// <summary>Lista el catálogo de planes (por defecto solo activos).</summary>
public sealed record GetPlansQuery(bool IncludeInactive = false);

public sealed class GetPlansQueryHandler(IPlanRepository planRepository)
{
    public async Task<Result<IReadOnlyList<PlanResponse>>> HandleAsync(GetPlansQuery query, CancellationToken cancellationToken)
    {
        var plans = await planRepository.GetAllAsync(query.IncludeInactive, cancellationToken);

        return Result<IReadOnlyList<PlanResponse>>.Success(
            [.. plans.Select(p => new PlanResponse(p.Id, p.Name, p.Description, p.BaseMonthlyFee, p.MaxBeneficiaries, p.Status, p.CreatedAtUtc))]);
    }
}
