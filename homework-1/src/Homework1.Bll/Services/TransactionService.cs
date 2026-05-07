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

    public Task<IReadOnlyList<StoredTransaction>> ListAsync(string? accountId, string? type, DateOnly? from, DateOnly? to)
    {
        return _repository.ListAsync(accountId, type, from, to);
    }

    public Task<StoredTransaction?> GetByIdAsync(Guid id)
    {
        return _repository.GetByIdAsync(id);
    }

    public async Task<decimal> GetAccountBalanceAsync(string accountId)
    {
        IReadOnlyList<StoredTransaction> transactions = await _repository.ListAsync();
        decimal balance = 0m;

        foreach (StoredTransaction transaction in transactions)
        {
            bool isDebit = transaction.FromAccount == accountId;
            bool isCredit = transaction.ToAccount == accountId;

            if (isCredit && (transaction.Type == "deposit" || transaction.Type == "credit"))
            {
                balance += transaction.Amount;
            }
            else if (isCredit && transaction.Type == "transfer")
            {
                balance += transaction.Amount;
            }
            else if (isDebit && (transaction.Type == "withdrawal" || transaction.Type == "debit"))
            {
                balance -= transaction.Amount;
            }
            else if (isDebit && transaction.Type == "transfer")
            {
                balance -= transaction.Amount;
            }
        }

        return balance;
    }

    public async Task<AccountSummary> GetAccountSummaryAsync(string accountId)
    {
        IReadOnlyList<StoredTransaction> transactions = await _repository.ListAsync();
        decimal totalDeposits = 0m;
        decimal totalWithdrawals = 0m;
        int transactionCount = 0;
        DateTimeOffset? mostRecentTransactionAt = null;

        foreach (StoredTransaction transaction in transactions)
        {
            bool isAccountAsRecipient = transaction.ToAccount == accountId;
            bool isAccountAsSender = transaction.FromAccount == accountId;

            if (isAccountAsRecipient && (transaction.Type == "deposit" || transaction.Type == "credit" || transaction.Type == "transfer"))
            {
                totalDeposits += transaction.Amount;
                transactionCount++;
                if (mostRecentTransactionAt == null || transaction.CreatedAt > mostRecentTransactionAt)
                {
                    mostRecentTransactionAt = transaction.CreatedAt;
                }
            }
            else if (isAccountAsSender && (transaction.Type == "withdrawal" || transaction.Type == "debit" || transaction.Type == "transfer"))
            {
                totalWithdrawals += transaction.Amount;
                transactionCount++;
                if (mostRecentTransactionAt == null || transaction.CreatedAt > mostRecentTransactionAt)
                {
                    mostRecentTransactionAt = transaction.CreatedAt;
                }
            }
        }

        return new AccountSummary(accountId, totalDeposits, totalWithdrawals, transactionCount, mostRecentTransactionAt);
    }
}

public record AccountSummary(
    string AccountId,
    decimal TotalDeposits,
    decimal TotalWithdrawals,
    int TransactionCount,
    DateTimeOffset? MostRecentTransactionAt);
