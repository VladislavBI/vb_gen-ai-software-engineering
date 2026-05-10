using FluentAssertions;
using Homework1.Api.Endpoints;
using Homework1.Bll.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;

#pragma warning disable IDE0008 // Use explicit type instead of var - relaxed for test code
namespace Homework1.Tests.Api.Endpoints;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable - handled by IAsyncLifetime
public class TransactionsEndpointsTests : IAsyncLifetime
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
    public async Task CreateTransaction_ValidRequest_ReturnsCreatedStatusAndTransaction()
    {
        // Arrange
        var request = new
        {
            fromAccount = "ACC-12345",
            toAccount = "ACC-67890",
            amount = 100.50m,
            currency = "USD",
            type = "transfer"
        };
        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client!.PostAsync("/transactions", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseContent = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;
        root.TryGetProperty("id", out var idElement).Should().BeTrue();
        root.TryGetProperty("amount", out var amountElement).Should().BeTrue();
        amountElement.GetDecimal().Should().Be(100.50m);
        root.TryGetProperty("fromAccount", out var fromElement).Should().BeTrue();
        fromElement.GetString().Should().Be("ACC-12345");
    }

    [Fact]
    public async Task CreateTransaction_InvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            fromAccount = "ACC-12345",
            toAccount = "ACC-67890",
            amount = -5m,
            currency = "USD",
            type = "transfer"
        };
        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client!.PostAsync("/transactions", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_InvalidAccountFormat_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            fromAccount = "invalid-format",
            toAccount = "ACC-67890",
            amount = 50m,
            currency = "USD",
            type = "transfer"
        };
        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client!.PostAsync("/transactions", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_InvalidCurrency_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            fromAccount = "ACC-12345",
            toAccount = "ACC-67890",
            amount = 50m,
            currency = "ZZZ",
            type = "transfer"
        };
        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client!.PostAsync("/transactions", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTransaction_TooManyDecimalPlaces_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            fromAccount = "ACC-12345",
            toAccount = "ACC-67890",
            amount = 12.555m,
            currency = "USD",
            type = "transfer"
        };
        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await _client!.PostAsync("/transactions", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListTransactions_NoTransactions_ReturnsEmptyList()
    {
        // Act
        var response = await _client!.GetAsync("/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;
        root.ValueKind.Should().Be(JsonValueKind.Array);
        root.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ListTransactions_AfterCreate_ReturnsTransaction()
    {
        // Arrange
        var createRequest = new
        {
            fromAccount = "ACC-AAAAA",
            toAccount = "ACC-BBBBB",
            amount = 10m,
            currency = "USD",
            type = "transfer"
        };
        var createJson = JsonSerializer.Serialize(createRequest);
        using var createContent = new StringContent(createJson, System.Text.Encoding.UTF8, "application/json");

        // Act - Create
        var createResponse = await _client!.PostAsync("/transactions", createContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - List
        var listResponse = await _client.GetAsync("/transactions");

        // Assert
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listContent = await listResponse.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(listContent);
        var root = jsonDoc.RootElement;
        root.ValueKind.Should().Be(JsonValueKind.Array);
        root.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetTransactionById_ExistingTransaction_ReturnsTransaction()
    {
        // Arrange
        var createRequest = new
        {
            fromAccount = "ACC-AAAAA",
            toAccount = "ACC-BBBBB",
            amount = 25.50m,
            currency = "USD",
            type = "transfer"
        };
        var createJson = JsonSerializer.Serialize(createRequest);
        using var createContent = new StringContent(createJson, System.Text.Encoding.UTF8, "application/json");

        // Act - Create
        var createResponse = await _client!.PostAsync("/transactions", createContent);
        var createdContent = await createResponse.Content.ReadAsStringAsync();
        using var createdDoc = JsonDocument.Parse(createdContent);
        var createdId = createdDoc.RootElement.GetProperty("id").GetString();

        // Act - Get by id
        var getResponse = await _client.GetAsync($"/transactions/{createdId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await getResponse.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;
        root.GetProperty("id").GetString().Should().Be(createdId);
        root.GetProperty("amount").GetDecimal().Should().Be(25.50m);
    }

    [Fact]
    public async Task GetTransactionById_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _client!.GetAsync($"/transactions/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListTransactions_WithAccountIdFilter_ReturnsSingleAccountTransactions()
    {
        // Arrange
        object tx1 = new { fromAccount = "ACC-AAA", toAccount = "ACC-BBB", amount = 10m, currency = "USD", type = "transfer" };
        object tx2 = new { fromAccount = "ACC-AAA", toAccount = "ACC-CCC", amount = 20m, currency = "USD", type = "deposit" };
        object tx3 = new { fromAccount = "ACC-XXX", toAccount = "ACC-YYY", amount = 30m, currency = "EUR", type = "transfer" };

        foreach (object tx in new object[] { tx1, tx2, tx3 })
        {
            string json = JsonSerializer.Serialize(tx);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            await _client!.PostAsync("/transactions", content);
        }

        // Act
        var response = await _client!.GetAsync("/transactions?accountId=ACC-AAA");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string responseContent = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;
        root.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task ListTransactions_WithTypeFilter_ReturnsFilteredTransactions()
    {
        // Arrange
        object tx1 = new { fromAccount = "ACC-AAA", toAccount = "ACC-BBB", amount = 10m, currency = "USD", type = "transfer" };
        object tx2 = new { fromAccount = "ACC-BBB", toAccount = "ACC-CCC", amount = 20m, currency = "USD", type = "deposit" };

        foreach (object tx in new object[] { tx1, tx2 })
        {
            string json = JsonSerializer.Serialize(tx);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            await _client!.PostAsync("/transactions", content);
        }

        // Act
        var response = await _client!.GetAsync("/transactions?type=deposit");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string responseContent = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;
        root.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task ListTransactions_WithCombinedFilters_ReturnsMatchingTransactions()
    {
        // Arrange
        object tx1 = new { fromAccount = "ACC-AAA", toAccount = "ACC-BBB", amount = 10m, currency = "USD", type = "transfer" };
        object tx2 = new { fromAccount = "ACC-AAA", toAccount = "ACC-CCC", amount = 20m, currency = "USD", type = "deposit" };
        object tx3 = new { fromAccount = "ACC-XXX", toAccount = "ACC-YYY", amount = 30m, currency = "EUR", type = "transfer" };

        foreach (object tx in new object[] { tx1, tx2, tx3 })
        {
            string json = JsonSerializer.Serialize(tx);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            await _client!.PostAsync("/transactions", content);
        }

        // Act
        var response = await _client!.GetAsync("/transactions?accountId=ACC-AAA&type=transfer");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string responseContent = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;
        root.GetArrayLength().Should().Be(1);
    }
}
