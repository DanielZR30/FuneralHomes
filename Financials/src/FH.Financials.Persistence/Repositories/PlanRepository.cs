using FH.Financials.Application.Contracts.Persistence;
using FH.Financials.Domain.Entities;
using FH.Financials.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FH.Financials.Persistence.Repositories;

public class PlanRepository(FinancialsDbContext context) : IPlanRepository
{
    public async Task<IReadOnlyList<Plan>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var query = context.Plans.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(p => p.Status == PlanStatus.Active);
        }

        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Plans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        context.Plans.AsNoTracking().AnyAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken) =>
        context.Plans.AsNoTracking().AnyAsync(p => p.Name == name, cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken) =>
        context.Plans.AsNoTracking().AnyAsync(cancellationToken);

    public async Task AddAsync(Plan plan, CancellationToken cancellationToken) =>
        await context.Plans.AddAsync(plan, cancellationToken);
}
