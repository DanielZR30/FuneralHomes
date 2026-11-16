using FH.Financials.Domain.Entities;

namespace FH.Financials.Application.Contracts.Persistence;

/// <summary>Puerto de persistencia del agregado Plan (implementado por EF Core en Persistence).</summary>
public interface IPlanRepository
{
    Task<IReadOnlyList<Plan>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken);
    Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    Task<bool> AnyAsync(CancellationToken cancellationToken);
    Task AddAsync(Plan plan, CancellationToken cancellationToken);
}
