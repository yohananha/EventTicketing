namespace EventTicketing.DataAccess;

/// <summary>Commits staged repository changes; coordinates multi-step writes in one transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Runs <paramref name="action"/> inside a single DB transaction (used by the purchase flow).</summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
}
