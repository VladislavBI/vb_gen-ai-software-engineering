using Homework1.Bll.Abstractions;
using Homework1.Bll.Domain;

namespace Homework1.Bll.Services;

public class TransactionService
{
    private readonly ITransactionRepository _repository;

    public TransactionService(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public Task<StoredTransaction> CreateAsync(Transaction transaction)
    {
        return _repository.CreateAsync(transaction);
    }

    public Task<IReadOnlyList<StoredTransaction>> ListAsync()
    {
        return _repository.ListAsync();
    }
}
