using FH.Financials.Application.Common;
using FH.Financials.Application.Contracts.Persistence;
using FH.Financials.Application.UseCases.Plans.Common;
using FH.Financials.Domain.Entities;
using FH.Financials.Domain.Exceptions;

namespace FH.Financials.Application.UseCases.Plans.CreatePlan;

/// <summary>Crea un plan en el catálogo (solo Admin: se autoriza en el endpoint).</summary>
public sealed record CreatePlanCommand(string Name, string? Description, decimal BaseMonthlyFee, int MaxBeneficiaries);

public sealed class CreatePlanCommandHandler(IPlanRepository planRepository, IUnitOfWork unitOfWork)
{
    public async Task<Result<PlanResponse>> HandleAsync(CreatePlanCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result<PlanResponse>.Failure("El nombre del plan es obligatorio.");
        }
        if (command.BaseMonthlyFee <= 0)
        {
            return Result<PlanResponse>.Failure("La cuota base mensual debe ser mayor que cero.");
        }
        if (command.MaxBeneficiaries < 1)
        {
            return Result<PlanResponse>.Failure("El cupo de beneficiarios debe ser al menos 1.");
        }
        if (await planRepository.ExistsByNameAsync(command.Name.Trim(), cancellationToken))
        {
            return Result<PlanResponse>.Conflict($"Ya existe un plan llamado '{command.Name.Trim()}'.");
        }

        try
        {
            var plan = Plan.Create(command.Name, command.Description, command.BaseMonthlyFee, command.MaxBeneficiaries);
            await planRepository.AddAsync(plan, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<PlanResponse>.Success(
                new PlanResponse(plan.Id, plan.Name, plan.Description, plan.BaseMonthlyFee, plan.MaxBeneficiaries, plan.Status, plan.CreatedAtUtc));
        }
        catch (BusinessRuleException ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result<PlanResponse>.UnprocessableEntity(ex.Message);
        }
    }
}
