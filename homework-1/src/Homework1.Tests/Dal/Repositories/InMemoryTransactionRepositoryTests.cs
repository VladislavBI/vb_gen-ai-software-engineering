using FluentAssertions;
using Homework1.Bll.Domain;
using Homework1.Dal.Repositories;
using System.Collections.Concurrent;

#pragma warning disable IDE0007 // Use var instead of explicit type - relaxed for test code
#pragma warning disable IDE0008 // Use explicit type instead of var - relaxed for test code
#pragma warning disable S6608 // Use indexing instead of Enumerable extension methods - relaxed for readability
#pragma warning disable CA1826 // Use indexing instead of Enumerable methods - relaxed for readability
namespace Homework1.Tests.Dal.Repositories;

public class InMemoryTransactionRepositoryTests
{
    private readonly ConcurrentDictionary<Guid, InMemoryTransactionRepository.TransactionEntity> _store;
    private readonly InMemoryTransactionRepository _repository;

    public InMemoryTransactionRepositoryTests()
    {
        _store = new ConcurrentDictionary<Guid, InMemoryTransactionRepository.TransactionEntity>();
        _repository = new InMemoryTransactionRepository(_store);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidTransaction_StoresAndReturnsTransaction()
    {
        // Arrange
        var transaction = new Transaction("ACC-FROM", "ACC-TO", 100.50m, "USD", "transfer");

        // Act
        StoredTransaction result = await _repository.CreateAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.FromAccount.Should().Be("ACC-FROM");
        result.ToAccount.Should().Be("ACC-TO");
        result.Amount.Should().Be(100.50m);
        result.Currency.Should().Be("USD");
        result.Type.Should().Be("transfer");
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateAsync_MultipleTransactions_StoresAllTransactions()
    {
        // Arrange
        var tx1 = new Transaction("ACC-1", "ACC-2", 50m, "USD", "transfer");
        var tx2 = new Transaction("ACC-3", "ACC-4", 75m, "EUR", "deposit");

        // Act
        StoredTransaction result1 = await _repository.CreateAsync(tx1);
        StoredTransaction result2 = await _repository.CreateAsync(tx2);

        // Assert
        result1.Id.Should().NotBe(result2.Id);
        _store.Count.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_GeneratesUniqueIds()
    {
        // Arrange
        var tx1 = new Transaction("ACC-1", "ACC-2", 50m, "USD", "transfer");
        var tx2 = new Transaction("ACC-1", "ACC-2", 50m, "USD", "transfer");

        // Act
        StoredTransaction result1 = await _repository.CreateAsync(tx1);
        StoredTransaction result2 = await _repository.CreateAsync(tx2);

        // Assert
        result1.Id.Should().NotBe(result2.Id);
    }

    #endregion

    #region ListAsync (no filter) Tests

    [Fact]
    public async Task ListAsync_NoTransactions_ReturnsEmptyList()
    {
        // Act
        IReadOnlyList<StoredTransaction> result = await _repository.ListAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(0);
    }

    [Fact]
    public async Task ListAsync_WithTransactions_ReturnsAllTransactions()
    {
        // Arrange
        var tx1 = new Transaction("ACC-1", "ACC-2", 50m, "USD", "transfer");
        var tx2 = new Transaction("ACC-3", "ACC-4", 75m, "EUR", "deposit");

        await _repository.CreateAsync(tx1);
        await _repository.CreateAsync(tx2);

        // Act
        IReadOnlyList<StoredTransaction> result = await _repository.ListAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListAsync_ReturnsTransactionsSortedByCreatedAtDescending()
    {
        // Arrange
        var tx1 = new Transaction("ACC-1", "ACC-2", 50m, "USD", "transfer");
        var tx2 = new Transaction("ACC-3", "ACC-4", 75m, "EUR", "deposit");

        StoredTransaction result1 = await _repository.CreateAsync(tx1);
        await Task.Delay(10); // Ensure different timestamps
        StoredTransaction result2 = await _repository.CreateAsync(tx2);

        // Act
        IReadOnlyList<StoredTransaction> results = await _repository.ListAsync();

        // Assert
        results.First().Id.Should().Be(result2.Id);
        results.Last().Id.Should().Be(result1.Id);
    }

    #endregion

    #region ListAsync (with filters) Tests

    [Fact]
    public async Task ListAsync_FilterByAccountId_ReturnsOnlyTransactionsForAccount()
    {
        // Arrange
        var tx1 = new Transaction("ACC-A", "ACC-B", 50m, "USD", "transfer");
        var tx2 = new Transaction("ACC-A", "ACC-C", 75m, "EUR", "deposit");
        var tx3 = new Transaction("ACC-X", "ACC-Y", 100m, "GBP", "withdrawal");

        await _repository.CreateAsync(tx1);
        await _repository.CreateAsync(tx2);
        await _repository.CreateAsync(tx3);

        // Act
        IReadOnlyList<StoredTransaction> result = await _repository.ListAsync("ACC-A", null, null, null);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.FromAccount.Should().Be("ACC-A"));
    }

    [Fact]
    public async Task ListAsync_FilterByType_ReturnsOnlyTransactionsOfType()
    {
        // Arrange
        var tx1 = new Transaction("ACC-1", "ACC-2", 50m, "USD", "transfer");
        var tx2 = new Transaction("ACC-3", "ACC-4", 75m, "EUR", "deposit");
        var tx3 = new Transaction("ACC-5", "ACC-6", 100m, "GBP", "deposit");

        await _repository.CreateAsync(tx1);
        await _repository.CreateAsync(tx2);
        await _repository.CreateAsync(tx3);

        // Act
        IReadOnlyList<StoredTransaction> result = await _repository.ListAsync(null, "deposit", null, null);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.Type.Should().Be("deposit"));
    }

    [Fact]
    public async Task ListAsync_FilterByDateRange_ReturnsTransactionsInRange()
    {
        // Arrange
        var tx1 = new Transaction("ACC-1", "ACC-2", 50m, "USD", "transfer");
        StoredTransaction storedTx1 = await _repository.CreateAsync(tx1);

        DateOnly from = DateOnly.FromDateTime(storedTx1.CreatedAt.DateTime);
        DateOnly to = DateOnly.FromDateTime(storedTx1.CreatedAt.AddDays(1).DateTime);

        // Act
        IReadOnlyList<StoredTransaction> result = await _repository.ListAsync(null, null, from, to);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(storedTx1.Id);
    }

    [Fact]
    public async Task ListAsync_FilterByAccountIdAndType_ReturnsOnlyMatchingTransactions()
    {
        // Arrange
        var tx1 = new Transaction("ACC-A", "ACC-B", 50m, "USD", "transfer");
        var tx2 = new Transaction("ACC-A", "ACC-C", 75m, "EUR", "deposit");
        var tx3 = new Transaction("ACC-X", "ACC-Y", 100m, "GBP", "deposit");

        await _repository.CreateAsync(tx1);
        await _repository.CreateAsync(tx2);
        await _repository.CreateAsync(tx3);

        // Act
        IReadOnlyList<StoredTransaction> result = await _repository.ListAsync("ACC-A", "deposit", null, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().FromAccount.Should().Be("ACC-A");
        result.First().Type.Should().Be("deposit");
    }

    [Fact]
    public async Task ListAsync_FilterByToAccountId_ReturnsTransactionsWhereAccountIsRecipient()
    {
        // Arrange
        var tx1 = new Transaction("ACC-1", "ACC-TARGET", 50m, "USD", "transfer");
        var tx2 = new Transaction("ACC-TARGET", "ACC-2", 75m, "EUR", "withdrawal");

        await _repository.CreateAsync(tx1);
        await _repository.CreateAsync(tx2);

        // Act
        IReadOnlyList<StoredTransaction> result = await _repository.ListAsync("ACC-TARGET", null, null, null);

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsTransaction()
    {
        // Arrange
        var tx = new Transaction("ACC-1", "ACC-2", 50m, "USD", "transfer");
        StoredTransaction created = await _repository.CreateAsync(tx);

        // Act
        StoredTransaction? result = await _repository.GetByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.FromAccount.Should().Be("ACC-1");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        Guid nonExistentId = Guid.NewGuid();

        // Act
        StoredTransaction? result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_AfterCreate_ReturnsCorrectTransaction()
    {
        // Arrange
        var tx1 = new Transaction("ACC-1", "ACC-2", 50m, "USD", "transfer");
        var tx2 = new Transaction("ACC-3", "ACC-4", 75m, "EUR", "deposit");

        _ = await _repository.CreateAsync(tx1);
        StoredTransaction created2 = await _repository.CreateAsync(tx2);

        // Act
        StoredTransaction? result = await _repository.GetByIdAsync(created2.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created2.Id);
        result.Amount.Should().Be(75m);
    }

    #endregion

    #region Isolation and Concurrency Tests

    [Fact]
    public async Task ConcurrentCreates_IsolateTransactions()
    {
        // Arrange
        List<Transaction> transactions = Enumerable.Range(1, 10)
            .Select(i => new Transaction($"ACC-{i}", $"ACC-{i + 1}", i * 10m, "USD", "transfer"))
            .ToList();

        // Act
        List<Task<StoredTransaction>> createTasks = transactions.Select(tx => _repository.CreateAsync(tx)).ToList();
        StoredTransaction[] results = await Task.WhenAll(createTasks);

        // Assert
        results.Should().HaveCount(10);
        _store.Count.Should().Be(10);
        results.Select(r => r.Id).Should().NotContain(Guid.Empty);
        results.Select(r => r.Id).Distinct().Count().Should().Be(results.Length);
    }

    [Fact]
    public async Task StoreDoesNotLeakBetweenRepositoryInstances()
    {
        // Arrange
        var tx = new Transaction("ACC-1", "ACC-2", 50m, "USD", "transfer");

        var repo1 = new InMemoryTransactionRepository(_store);
        StoredTransaction created = await repo1.CreateAsync(tx);

        var repo2 = new InMemoryTransactionRepository(_store);

        // Act
        StoredTransaction? result = await repo2.GetByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    #endregion
}
