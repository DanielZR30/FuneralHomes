using FH.Financials.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FH.Financials.Persistence.UnitOfWorks;

public class UnitOfWork(FinancialsDbContext context) : IUnitOfWork
{
    public async Task CommitAsync(CancellationToken cancellationToken) =>
        await context.SaveChangesAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask; // El DbContext se descarta con el scope; nada persistido sin Commit
}
