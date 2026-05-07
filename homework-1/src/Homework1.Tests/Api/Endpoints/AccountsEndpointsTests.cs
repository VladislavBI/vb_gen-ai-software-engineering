using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

#pragma warning disable IDE0008 // Use explicit type instead of var - relaxed for test code
namespace Homework1.Tests.Api.Endpoints;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable - handled by IAsyncLifetime
public class AccountsEndpointsTests : IAsyncLifetime
#pragma warning restore CA1001
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _factory?.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetAccountBalance_NoTransactions_ReturnsZero()
    {
        // Act
        HttpResponseMessage response = await _client!.GetAsync("/accounts/ACC-12345/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        root.GetProperty("balance").GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task GetAccountBalance_WithDeposit_ReturnsPositiveBalance()
    {
        // Arrange
        var deposit = new
        {
            fromAccount = "ACC-SRC",
            toAccount = "ACC-DEST",
            amount = 100m,
            currency = "USD",
            type = "deposit"
        };
        var json = JsonSerializer.Serialize(deposit);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        await _client!.PostAsync("/transactions", content);

        // Act
        var response = await _client!.GetAsync("/accounts/ACC-DEST/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;
        root.GetProperty("balance").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task GetAccountBalance_WithWithdrawal_ReturnsNegativeBalance()
    {
        // Arrange
        var withdrawal = new
        {
            fromAccount = "ACC-SRC",
            toAccount = "ACC-DEST",
            amount = 50m,
            currency = "USD",
            type = "withdrawal"
        };
        var json = JsonSerializer.Serialize(withdrawal);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        await _client!.PostAsync("/transactions", content);

        // Act
        var response = await _client!.GetAsync("/accounts/ACC-SRC/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;
        root.GetProperty("balance").GetDecimal().Should().Be(-50m);
    }

    [Fact]
    public async Task GetAccountBalance_WithMultipleTransactions_ReturnsCorrectBalance()
    {
        // Arrange
        object tx1 = new { fromAccount = "ACC-SRC", toAccount = "ACC-TGT", amount = 100m, currency = "USD", type = "deposit" };
        object tx2 = new { fromAccount = "ACC-TGT", toAccount = "ACC-OTH", amount = 30m, currency = "USD", type = "withdrawal" };

        foreach (object tx in new object[] { tx1, tx2 })
        {
            string json = JsonSerializer.Serialize(tx);
            using var txContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            await _client!.PostAsync("/transactions", txContent);
        }

        // Act
        var response = await _client!.GetAsync("/accounts/ACC-TGT/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string responseContent = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;
        root.GetProperty("balance").GetDecimal().Should().Be(70m); // 100 - 30
    }

    [Fact]
    public async Task GetAccountSummary_NoTransactions_ReturnsZeroSummary()
    {
        // Act
        var response = await _client!.GetAsync("/accounts/ACC-12345/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        root.GetProperty("totalDeposits").GetDecimal().Should().Be(0m);
        root.GetProperty("totalWithdrawals").GetDecimal().Should().Be(0m);
        root.GetProperty("transactionCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetAccountSummary_WithTransactions_ReturnsSummary()
    {
        // Arrange
        object deposit = new { fromAccount = "ACC-SRC", toAccount = "ACC-TGT", amount = 100m, currency = "USD", type = "deposit" };
        object withdrawal = new { fromAccount = "ACC-TGT", toAccount = "ACC-OTH", amount = 25m, currency = "USD", type = "withdrawal" };

        string depositJson = JsonSerializer.Serialize(deposit);
        using var depositContent = new StringContent(depositJson, System.Text.Encoding.UTF8, "application/json");
        await _client!.PostAsync("/transactions", depositContent);

        string withdrawalJson = JsonSerializer.Serialize(withdrawal);
        using var withdrawalContent = new StringContent(withdrawalJson, System.Text.Encoding.UTF8, "application/json");
        await _client!.PostAsync("/transactions", withdrawalContent);

        // Act
        var response = await _client!.GetAsync("/accounts/ACC-TGT/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string responseContent = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;
        root.GetProperty("totalDeposits").GetDecimal().Should().Be(100m);
        root.GetProperty("totalWithdrawals").GetDecimal().Should().Be(25m);
        root.GetProperty("transactionCount").GetInt32().Should().Be(2);
        root.TryGetProperty("mostRecentTransactionAt", out var mostRecentElement).Should().BeTrue();
        mostRecentElement.ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetAccountSummary_MultipleDeposits_ReturnsSumOfDeposits()
    {
        // Arrange
        object tx1 = new { fromAccount = "ACC-SRC1", toAccount = "ACC-TGT", amount = 50m, currency = "USD", type = "deposit" };
        object tx2 = new { fromAccount = "ACC-SRC2", toAccount = "ACC-TGT", amount = 75m, currency = "USD", type = "deposit" };

        foreach (object tx in new object[] { tx1, tx2 })
        {
            string json = JsonSerializer.Serialize(tx);
            using var txContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            await _client!.PostAsync("/transactions", txContent);
        }

        // Act
        var response = await _client!.GetAsync("/accounts/ACC-TGT/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string responseContent = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;
        root.GetProperty("totalDeposits").GetDecimal().Should().Be(125m);
        root.GetProperty("transactionCount").GetInt32().Should().Be(2);
    }
}
