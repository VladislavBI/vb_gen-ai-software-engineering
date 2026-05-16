using FluentAssertions;
using Homework1.Bll.Abstractions;
using Homework1.Bll.Domain;
using Homework1.Bll.Services;
using Moq;

#pragma warning disable IDE0007 // Use var instead of explicit type - relaxed for test code
#pragma warning disable IDE0008 // Use explicit type instead of var - relaxed for test code
#pragma warning disable S6608 // Use indexing instead of Enumerable extension methods - relaxed for readability
#pragma warning disable CA1826 // Use indexing instead of Enumerable methods - relaxed for readability
namespace Homework1.Tests.Bll.Services;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _service = new TransactionService(_mockRepository.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidTransaction_CallsRepositoryAndReturnsStoredTransaction()
    {
        // Arrange
        var transaction = new Transaction("ACC-FROM", "ACC-TO", 100m, "USD", "transfer");
        var storedTransaction = new StoredTransaction(Guid.NewGuid(), "ACC-FROM", "ACC-TO", 100m, "USD", "transfer", DateTimeOffset.UtcNow);

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Transaction>()))
            .ReturnsAsync(storedTransaction);

        // Act
        StoredTransaction result = await _service.CreateAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(storedTransaction.Id);
        result.FromAccount.Should().Be("ACC-FROM");
        result.ToAccount.Should().Be("ACC-TO");
        result.Amount.Should().Be(100m);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Transaction>()), Times.Once);
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_NoFilter_ReturnsAllTransactions()
    {
        // Arrange
        IReadOnlyList<StoredTransaction> transactions = new List<StoredTransaction>
        {
            new StoredTransaction(Guid.NewGuid(), "ACC-1", "ACC-2", 50m, "USD", "transfer", DateTimeOffset.UtcNow),
            new StoredTransaction(Guid.NewGuid(), "ACC-3", "ACC-4", 75m, "EUR", "deposit", DateTimeOffset.UtcNow)
        };

        _mockRepository.Setup(r => r.ListAsync())
            .ReturnsAsync(transactions);

        // Act
        var result = await _service.ListAsync();

        // Assert
        result.Should().HaveCount(2);
        _mockRepository.Verify(r => r.ListAsync(), Times.Once);
    }

    [Fact]
    public async Task ListAsync_WithAccountFilter_CallsRepositoryWithFilter()
    {
        // Arrange
        IReadOnlyList<StoredTransaction> transactions = new List<StoredTransaction>
        {
            new StoredTransaction(Guid.NewGuid(), "ACC-1", "ACC-2", 50m, "USD", "transfer", DateTimeOffset.UtcNow)
        };

        _mockRepository.Setup(r => r.ListAsync("ACC-1", null, null, null))
            .ReturnsAsync(transactions);

        // Act
        var result = await _service.ListAsync("ACC-1", null, null, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().FromAccount.Should().Be("ACC-1");
        _mockRepository.Verify(r => r.ListAsync("ACC-1", null, null, null), Times.Once);
    }

    [Fact]
    public async Task ListAsync_WithTypeFilter_CallsRepositoryWithFilter()
    {
        // Arrange
        IReadOnlyList<StoredTransaction> transactions = new List<StoredTransaction>
        {
            new StoredTransaction(Guid.NewGuid(), "ACC-1", "ACC-2", 50m, "USD", "deposit", DateTimeOffset.UtcNow)
        };

        _mockRepository.Setup(r => r.ListAsync(null, "deposit", null, null))
            .ReturnsAsync(transactions);

        // Act
        var result = await _service.ListAsync(null, "deposit", null, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().Type.Should().Be("deposit");
        _mockRepository.Verify(r => r.ListAsync(null, "deposit", null, null), Times.Once);
    }

    [Fact]
    public async Task ListAsync_WithDateRangeFilter_CallsRepositoryWithFilter()
    {
        // Arrange
        DateOnly from = new DateOnly(2026, 5, 1);
        DateOnly to = new DateOnly(2026, 5, 10);
        IReadOnlyList<StoredTransaction> transactions = new List<StoredTransaction>();

        _mockRepository.Setup(r => r.ListAsync(null, null, from, to))
            .ReturnsAsync(transactions);

        // Act
        _ = await _service.ListAsync(null, null, from, to);

        // Assert
        _mockRepository.Verify(r => r.ListAsync(null, null, from, to), Times.Once);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsTransaction()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        StoredTransaction transaction = new StoredTransaction(id, "ACC-FROM", "ACC-TO", 100m, "USD", "transfer", DateTimeOffset.UtcNow);

        _mockRepository.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(transaction);

        // Act
        StoredTransaction? result = await _service.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        _mockRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        Guid id = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync((StoredTransaction?)null);

        // Act
        StoredTransaction? result = await _service.GetByIdAsync(id);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
    }

    #endregion

    #region GetAccountBalanceAsync Tests

    [Fact]
    public async Task GetAccountBalanceAsync_NoTransactions_ReturnsZero()
    {
        // Arrange
        _mockRepository.Setup(r => r.ListAsync())
            .ReturnsAsync(new List<StoredTransaction>().AsReadOnly());

        // Act
        decimal balance = await _service.GetAccountBalanceAsync("ACC-TEST");

        // Assert
        balance.Should().Be(0m);
    }

    [Fact]
    public async Task GetAccountBalanceAsync_WithDeposit_ReturnsPositiveBalance()
    {
        // Arrange
        IReadOnlyList<StoredTransaction> transactions = new List<StoredTransaction>
        {
            new StoredTransaction(Guid.NewGuid(), "ACC-SRC", "ACC-TEST", 100m, "USD", "deposit", DateTimeOffset.UtcNow)
        };

        _mockRepository.Setup(r => r.ListAsync())
            .ReturnsAsync(transactions);

        // Act
        decimal balance = await _service.GetAccountBalanceAsync("ACC-TEST");

        // Assert
        balance.Should().Be(100m);
    }

    [Fact]
    public async Task GetAccountBalanceAsync_WithWithdrawal_ReturnsNegativeBalance()
    {
        // Arrange
        IReadOnlyList<StoredTransaction> transactions = new List<StoredTransaction>
        {
            new StoredTransaction(Guid.NewGuid(), "ACC-TEST", "ACC-DEST", 50m, "USD", "withdrawal", DateTimeOffset.UtcNow)
        };

        _mockRepository.Setup(r => r.ListAsync())
            .ReturnsAsync(transactions);

        // Act
        decimal balance = await _service.GetAccountBalanceAsync("ACC-TEST");

        // Assert
        balance.Should().Be(-50m);
    }

    [Fact]
    public async Task GetAccountBalanceAsync_WithTransfer_CorrectlyCalculatesBalance()
    {
        // Arrange
        IReadOnlyList<StoredTransaction> transactions = new List<StoredTransaction>
        {
            new StoredTransaction(Guid.NewGuid(), "ACC-OTHER", "ACC-TEST", 100m, "USD", "transfer", DateTimeOffset.UtcNow),
            new StoredTransaction(Guid.NewGuid(), "ACC-TEST", "ACC-OTHER", 30m, "USD", "transfer", DateTimeOffset.UtcNow)
        };

        _mockRepository.Setup(r => r.ListAsync())
            .ReturnsAsync(transactions);

        // Act
        decimal balance = await _service.GetAccountBalanceAsync("ACC-TEST");

        // Assert
        balance.Should().Be(70m); // 100 - 30
    }

    #endregion

    #region GetAccountSummaryAsync Tests

    [Fact]
    public async Task GetAccountSummaryAsync_NoTransactions_ReturnsZeroSummary()
    {
        // Arrange
        _mockRepository.Setup(r => r.ListAsync())
            .ReturnsAsync(new List<StoredTransaction>().AsReadOnly());

        // Act
        AccountSummary summary = await _service.GetAccountSummaryAsync("ACC-TEST");

        // Assert
        summary.TotalDeposits.Should().Be(0m);
        summary.TotalWithdrawals.Should().Be(0m);
        summary.TransactionCount.Should().Be(0);
        summary.MostRecentTransactionAt.Should().BeNull();
    }

    [Fact]
    public async Task GetAccountSummaryAsync_WithDeposits_ReturnsSummary()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IReadOnlyList<StoredTransaction> transactions = new List<StoredTransaction>
        {
            new StoredTransaction(Guid.NewGuid(), "ACC-SRC", "ACC-TEST", 100m, "USD", "deposit", now.AddMinutes(-5)),
            new StoredTransaction(Guid.NewGuid(), "ACC-SRC2", "ACC-TEST", 50m, "USD", "deposit", now)
        };

        _mockRepository.Setup(r => r.ListAsync())
            .ReturnsAsync(transactions);

        // Act
        AccountSummary summary = await _service.GetAccountSummaryAsync("ACC-TEST");

        // Assert
        summary.TotalDeposits.Should().Be(150m);
        summary.TotalWithdrawals.Should().Be(0m);
        summary.TransactionCount.Should().Be(2);
        summary.MostRecentTransactionAt.Should().Be(now);
    }

    [Fact]
    public async Task GetAccountSummaryAsync_WithMixedTransactions_ReturnsSummary()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        IReadOnlyList<StoredTransaction> transactions = new List<StoredTransaction>
        {
            new StoredTransaction(Guid.NewGuid(), "ACC-SRC", "ACC-TEST", 100m, "USD", "deposit", now.AddMinutes(-10)),
            new StoredTransaction(Guid.NewGuid(), "ACC-TEST", "ACC-DEST", 25m, "USD", "withdrawal", now.AddMinutes(-5)),
            new StoredTransaction(Guid.NewGuid(), "ACC-OTHER", "ACC-TEST", 50m, "USD", "transfer", now)
        };

        _mockRepository.Setup(r => r.ListAsync())
            .ReturnsAsync(transactions);

        // Act
        AccountSummary summary = await _service.GetAccountSummaryAsync("ACC-TEST");

        // Assert
        summary.TotalDeposits.Should().Be(150m); // 100 + 50 from transfer
        summary.TotalWithdrawals.Should().Be(25m);
        summary.TransactionCount.Should().Be(3);
        summary.MostRecentTransactionAt.Should().Be(now);
    }

    [Fact]
    public async Task GetAccountSummaryAsync_AccountNotInvolved_ReturnsZeroSummary()
    {
        // Arrange
        IReadOnlyList<StoredTransaction> transactions = new List<StoredTransaction>
        {
            new StoredTransaction(Guid.NewGuid(), "ACC-OTHER1", "ACC-OTHER2", 100m, "USD", "transfer", DateTimeOffset.UtcNow)
        };

        _mockRepository.Setup(r => r.ListAsync())
            .ReturnsAsync(transactions);

        // Act
        AccountSummary summary = await _service.GetAccountSummaryAsync("ACC-TEST");

        // Assert
        summary.TotalDeposits.Should().Be(0m);
        summary.TotalWithdrawals.Should().Be(0m);
        summary.TransactionCount.Should().Be(0);
    }

    #endregion
}
