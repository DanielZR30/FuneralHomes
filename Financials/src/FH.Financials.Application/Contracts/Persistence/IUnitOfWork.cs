namespace FH.Financials.Application.Contracts.Persistence;

/// <summary>Unidad de trabajo: confirma los cambios del caso de uso en una sola transacción.</summary>
public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
