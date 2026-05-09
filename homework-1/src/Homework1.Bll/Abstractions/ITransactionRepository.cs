using Homework1.Bll.Domain;

namespace Homework1.Bll.Abstractions;

public interface ITransactionRepository
{
    Task<StoredTransaction> CreateAsync(Transaction transaction);
    Task<IReadOnlyList<StoredTransaction>> ListAsync();
    Task<StoredTransaction?> GetByIdAsync(Guid id);
}
