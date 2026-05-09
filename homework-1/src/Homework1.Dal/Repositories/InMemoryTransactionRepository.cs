using System.Collections.Concurrent;
using Homework1.Bll.Abstractions;
using Homework1.Bll.Domain;

namespace Homework1.Dal.Repositories;

public class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly ConcurrentDictionary<Guid, TransactionEntity> _store;

    public InMemoryTransactionRepository(ConcurrentDictionary<Guid, TransactionEntity> store)
    {
        _store = store;
    }

    public Task<StoredTransaction> CreateAsync(Transaction transaction)
    {
        var id = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var entity = new TransactionEntity(
            id,
            transaction.FromAccount,
            transaction.ToAccount,
            transaction.Amount,
            transaction.Currency,
            transaction.Type,
            now);

        _store[id] = entity;
        var result = new StoredTransaction(id, transaction.FromAccount, transaction.ToAccount, transaction.Amount, transaction.Currency, transaction.Type, now);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<StoredTransaction>> ListAsync()
    {
        var transactions = _store.Values
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new StoredTransaction(
                e.Id,
                e.FromAccount,
                e.ToAccount,
                e.Amount,
                e.Currency,
                e.Type,
                e.CreatedAt))
            .ToList();

        IReadOnlyList<StoredTransaction> result = transactions.AsReadOnly();
        return Task.FromResult(result);
    }

    public record TransactionEntity(
        Guid Id,
        string FromAccount,
        string ToAccount,
        decimal Amount,
        string Currency,
        string Type,
        DateTimeOffset CreatedAt);
}
