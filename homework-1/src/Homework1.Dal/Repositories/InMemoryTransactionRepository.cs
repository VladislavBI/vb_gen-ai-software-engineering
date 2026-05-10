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
        return ListAsync(null, null, null, null);
    }

    public Task<IReadOnlyList<StoredTransaction>> ListAsync(string? accountId, string? type, DateOnly? from, DateOnly? to)
    {
        IEnumerable<TransactionEntity> query = _store.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(accountId))
        {
            query = query.Where(e => e.FromAccount == accountId || e.ToAccount == accountId);
        }

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(e => e.Type == type);
        }

        if (from.HasValue)
        {
            query = query.Where(e => DateOnly.FromDateTime(e.CreatedAt.DateTime) >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => DateOnly.FromDateTime(e.CreatedAt.DateTime) <= to.Value);
        }

        var transactions = query
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

    public Task<StoredTransaction?> GetByIdAsync(Guid id)
    {
        if (_store.TryGetValue(id, out TransactionEntity? entity))
        {
            StoredTransaction result = new(
                entity.Id,
                entity.FromAccount,
                entity.ToAccount,
                entity.Amount,
                entity.Currency,
                entity.Type,
                entity.CreatedAt);
            return Task.FromResult((StoredTransaction?)result);
        }

        return Task.FromResult((StoredTransaction?)null);
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
