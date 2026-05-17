#pragma warning disable IDE0005, IDE0008
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Homework2.Api.Models;
using Homework2.Bll.Abstractions;
using Homework2.Dal.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0005, IDE0008

namespace Homework2.Tests.Api.Endpoints;

/// <summary>Integration tests for Tickets CRUD endpoints.</summary>
public sealed class TicketsEndpointsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace the singleton ITicketRepository with a fresh in-memory instance per test
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITicketRepository));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddSingleton<ITicketRepository, InMemoryTicketRepository>();
                });
            });

        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task PostTickets_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "test@example.com",
            CustomerName: "Test User",
            Subject: "Login fails",
            Description: "I cannot log in to my account at all."
        );

        // Act
        var response = await _client.PostAsJsonAsync("/tickets", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);
        result.Id.Should().NotBeEmpty();
        result.CustomerId.Should().Be("C1");
        result.CustomerEmail.Should().Be("test@example.com");
        result.CustomerName.Should().Be("Test User");
        result.Subject.Should().Be("Login fails");
        result.Description.Should().Be("I cannot log in to my account at all.");
        result.Status.Should().Be("new");
        result.Priority.Should().Be("medium");
        result.Category.Should().Be("other");
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        result.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task PostTickets_WithInvalidEmail_Returns400()
    {
        // Arrange
        var request = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "not-an-email",
            CustomerName: "Test User",
            Subject: "Subject",
            Description: "This is a valid description"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/tickets", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
    }

    [Fact]
    public async Task PostTickets_WithShortDescription_Returns400()
    {
        // Arrange
        var request = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "test@example.com",
            CustomerName: "Test User",
            Subject: "Subject",
            Description: "short"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/tickets", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostTickets_WithLongSubject_Returns400()
    {
        // Arrange
        var longSubject = new string('a', 201);
        var request = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "test@example.com",
            CustomerName: "Test User",
            Subject: longSubject,
            Description: "This is a valid description"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/tickets", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostTickets_WithAutoClassifyTrue_Returns201WithClassification()
    {
        // Arrange
        var request = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "test@example.com",
            CustomerName: "Test User",
            Subject: "Cannot access account",
            Description: "I cannot access my account due to security breach."
        );

        // Act
        var response = await _client.PostAsJsonAsync("/tickets?autoClassify=true", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);
        result.Priority.Should().Be("urgent");
        result.Category.Should().Be("account_access");
    }

    [Fact]
    public async Task GetTickets_WithNoTickets_Returns200WithEmptyArray()
    {
        // Act
        var response = await _client.GetAsync("/tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TicketResponse>>(JsonOptions);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTickets_WithMultipleTickets_Returns200WithAllTickets()
    {
        // Arrange
        var req1 = new CreateTicketRequest("C1", "test1@example.com", "User1", "Subject1", "Description text here");
        var req2 = new CreateTicketRequest("C2", "test2@example.com", "User2", "Subject2", "Description text here");

        await _client.PostAsJsonAsync("/tickets", req1, JsonOptions);
        await _client.PostAsJsonAsync("/tickets", req2, JsonOptions);

        // Act
        var response = await _client.GetAsync("/tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TicketResponse>>(JsonOptions);
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTickets_FilterByPriority_ReturnsFilteredResults()
    {
        // Arrange
        var req1 = new CreateTicketRequest("C1", "test@example.com", "User", "Urgent issue", "Critical production down");
        var req2 = new CreateTicketRequest("C2", "test2@example.com", "User2", "Minor issue", "Small suggestion");

        await _client.PostAsJsonAsync("/tickets?autoClassify=true", req1, JsonOptions);
        await _client.PostAsJsonAsync("/tickets?autoClassify=true", req2, JsonOptions);

        // Act
        var response = await _client.GetAsync("/tickets?priority=urgent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TicketResponse>>(JsonOptions);
        result.Should().AllSatisfy(t => t.Priority.Should().Be("urgent"));
    }

    [Fact]
    public async Task GetTickets_FilterByCategory_ReturnsFilteredResults()
    {
        // Arrange
        var req1 = new CreateTicketRequest("C1", "test@example.com", "User", "Account issue", "Cannot access my account");
        var req2 = new CreateTicketRequest("C2", "test2@example.com", "User2", "Bug report", "System crashes on save");

        await _client.PostAsJsonAsync("/tickets?autoClassify=true", req1, JsonOptions);
        await _client.PostAsJsonAsync("/tickets?autoClassify=true", req2, JsonOptions);

        // Act
        var response = await _client.GetAsync("/tickets?category=account_access");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TicketResponse>>(JsonOptions);
        result.Should().AllSatisfy(t => t.Category.Should().Be("account_access"));
    }

    [Fact]
    public async Task GetTickets_FilterByCategoryAndPriority_ReturnsFilteredResults()
    {
        // Arrange
        var req1 = new CreateTicketRequest("C1", "test@example.com", "User", "Account issue", "Cannot access production account critical");
        var req2 = new CreateTicketRequest("C2", "test2@example.com", "User2", "Account issue", "Cannot access my account");

        await _client.PostAsJsonAsync("/tickets?autoClassify=true", req1, JsonOptions);
        await _client.PostAsJsonAsync("/tickets?autoClassify=true", req2, JsonOptions);

        // Act
        var response = await _client.GetAsync("/tickets?category=account_access&priority=urgent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TicketResponse>>(JsonOptions);
        result.Should().AllSatisfy(t =>
        {
            t.Category.Should().Be("account_access");
            t.Priority.Should().Be("urgent");
        });
    }

    [Fact]
    public async Task GetTicketsById_WithValidId_Returns200()
    {
        // Arrange
        var createRequest = new CreateTicketRequest("C1", "test@example.com", "User", "Subject", "Description here");
        var createResponse = await _client.PostAsJsonAsync("/tickets", createRequest, JsonOptions);

        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        // Act
        var response = await _client.GetAsync($"/tickets/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);
        result.Id.Should().Be(created.Id);
        result.Subject.Should().Be("Subject");
    }

    [Fact]
    public async Task GetTicketsById_WithNonexistentId_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/tickets/{nonexistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutTickets_WithValidUpdate_Returns200()
    {
        // Arrange
        var createRequest = new CreateTicketRequest("C1", "test@example.com", "User", "Subject", "Description here");
        var createResponse = await _client.PostAsJsonAsync("/tickets", createRequest, JsonOptions);

        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        var updateRequest = new UpdateTicketRequest(Subject: "Updated subject");

        // Act
        var response = await _client.PutAsJsonAsync($"/tickets/{created.Id}", updateRequest, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);
        result.Id.Should().Be(created.Id);
        result.Subject.Should().Be("Updated subject");
    }

    [Fact]
    public async Task PutTickets_WithInvalidDescription_Returns400()
    {
        // Arrange
        var createRequest = new CreateTicketRequest("C1", "test@example.com", "User", "Subject", "Description here");
        var createResponse = await _client.PostAsJsonAsync("/tickets", createRequest, JsonOptions);

        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        var updateRequest = new UpdateTicketRequest(Description: "short");

        // Act
        var response = await _client.PutAsJsonAsync($"/tickets/{created.Id}", updateRequest, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutTickets_WithNonexistentId_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid();
        var updateRequest = new UpdateTicketRequest(Subject: "Updated");

        // Act
        var response = await _client.PutAsJsonAsync($"/tickets/{nonexistentId}", updateRequest, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTickets_WithValidId_Returns204()
    {
        // Arrange
        var createRequest = new CreateTicketRequest("C1", "test@example.com", "User", "Subject", "Description here");
        var createResponse = await _client.PostAsJsonAsync("/tickets", createRequest, JsonOptions);

        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        // Act
        var response = await _client.DeleteAsync($"/tickets/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var getResponse = await _client.GetAsync($"/tickets/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTickets_WithNonexistentId_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/tickets/{nonexistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostTickets_ResponseContentTypeIsJson()
    {
        // Arrange
        var request = new CreateTicketRequest("C1", "test@example.com", "User", "Subject", "Description here");

        // Act
        var response = await _client.PostAsJsonAsync("/tickets", request, JsonOptions);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task PostTickets_ResponseBodyHasAllFields()
    {
        // Arrange
        var request = new CreateTicketRequest("C1", "test@example.com", "User", "Subject", "Description here");

        // Act
        var response = await _client.PostAsJsonAsync("/tickets", request, JsonOptions);
        var result = await response.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.CustomerId.Should().NotBeNullOrEmpty();
        result.CustomerEmail.Should().NotBeNullOrEmpty();
        result.CustomerName.Should().NotBeNullOrEmpty();
        result.Subject.Should().NotBeNullOrEmpty();
        result.Description.Should().NotBeNullOrEmpty();
        result.Category.Should().NotBeNullOrEmpty();
        result.Priority.Should().NotBeNullOrEmpty();
        result.Status.Should().NotBeNullOrEmpty();
        result.CreatedAt.Should().NotBe(default);
        result.UpdatedAt.Should().NotBe(default);
        result.Tags.Should().NotBeNull();
    }

    [Fact]
    public async Task PostTickets_WithEmptyCustomerId_Returns400()
    {
        // Arrange
        var request = new CreateTicketRequest("", "test@example.com", "User", "Subject", "Description here");

        // Act
        var response = await _client.PostAsJsonAsync("/tickets", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostTickets_WithEmptyCustomerName_Returns400()
    {
        // Arrange
        var request = new CreateTicketRequest("C1", "test@example.com", "", "Subject", "Description here");

        // Act
        var response = await _client.PostAsJsonAsync("/tickets", request, JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTickets_FilterByStatus_SnakeCaseValueParsedCorrectly()
    {
        // Arrange: create a ticket, then update its status to in_progress
        var createRequest = new CreateTicketRequest("C1", "test@example.com", "User", "Subject", "Description here");
        var createResponse = await _client.PostAsJsonAsync("/tickets", createRequest, JsonOptions);

        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        var updateRequest = new UpdateTicketRequest(Status: Homework2.Bll.Domain.Status.InProgress);
        await _client.PutAsJsonAsync($"/tickets/{created!.Id}", updateRequest, JsonOptions);

        // Act: filter by snake_case status value — verifies Enum.TryParse handles "in_progress"
        var response = await _client.GetAsync("/tickets?status=in_progress");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<TicketResponse>>(JsonOptions);
        result.Should().ContainSingle(t => t.Id == created.Id && t.Status == "in_progress");
    }

    [Fact]
    public async Task PostAutoClassify_WithValidTicketId_Returns200AndClassifiedResult()
    {
        // Arrange: create a ticket first
        var createRequest = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "test@example.com",
            CustomerName: "Test User",
            Subject: "Security breach",
            Description: "I believe my account has been compromised due to unauthorized access."
        );
        var createResponse = await _client.PostAsJsonAsync("/tickets", createRequest, JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        // Act: call the auto-classify endpoint
        var response = await _client.PostAsync($"/tickets/{created!.Id}/auto-classify", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var classified = await response.Content.ReadFromJsonAsync<ClassificationResponse>(JsonOptions);
        classified.Should().NotBeNull();
        classified!.Category.Should().NotBeNullOrEmpty();
        classified.Priority.Should().NotBeNullOrEmpty();
        classified.Confidence.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(1);
        classified.Reasoning.Should().NotBeNullOrEmpty();
        classified.KeywordsFound.Should().NotBeNull();
    }

    [Fact]
    public async Task PostAutoClassify_WithInvalidTicketId_Returns404NotFound()
    {
        // Act: call the auto-classify endpoint with a non-existent ticket ID
        var nonExistentId = Guid.NewGuid();
        var response = await _client.PostAsync($"/tickets/{nonExistentId}/auto-classify", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostAutoClassify_UpdatesTicketWithClassification()
    {
        // Arrange: create a ticket
        var createRequest = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "test@example.com",
            CustomerName: "Test User",
            Subject: "Cannot access account",
            Description: "I cannot access my account due to security breach."
        );
        var createResponse = await _client.PostAsJsonAsync("/tickets", createRequest, JsonOptions);
        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);

        // Act: classify the ticket
        await _client.PostAsync($"/tickets/{created!.Id}/auto-classify", null);

        // Assert: verify the ticket was updated with classification
        var getResponse = await _client.GetAsync($"/tickets/{created.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions);
        updated.Should().NotBeNull();
        updated!.Category.Should().Be("account_access");
        updated.Priority.Should().Be("urgent");
    }

    [Fact]
    public async Task PostImport_WithValidCsv_Returns200AndImportSummary()
    {
        // Arrange
        var csvContent = "customer_id,customer_email,customer_name,subject,description\n" +
                        "C1,test1@example.com,User1,Subject1,This is a valid description text";
        var memStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));
        var formContent = new MultipartFormDataContent
        {
            { new StreamContent(memStream), "file", "test.csv" }
        };

        // Act
        var response = await _client.PostAsync("/tickets/import", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ImportSummaryResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Total.Should().Be(1);
        result.Successful.Should().Be(1);
        result.Failed.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task PostImport_WithInvalidData_Returns200AndImportErrors()
    {
        // Arrange
        var csvContent = "customer_id,customer_email,customer_name,subject,description\n" +
                        "C1,bad-email,User1,Subject1,short";
        var memStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));
        var formContent = new MultipartFormDataContent
        {
            { new StreamContent(memStream), "file", "test.csv" }
        };

        // Act
        var response = await _client.PostAsync("/tickets/import", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ImportSummaryResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Total.Should().Be(1);
        result.Failed.Should().Be(1);
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public async Task PostImport_WithJsonFile_Returns200AndImportSummary()
    {
        // Arrange
        var jsonContent = "[{\"customer_id\":\"C1\",\"customer_email\":\"test@example.com\",\"customer_name\":\"User\",\"subject\":\"Subject\",\"description\":\"This is a valid description\"}]";
        var memStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));
        var formContent = new MultipartFormDataContent
        {
            { new StreamContent(memStream), "file", "test.json" }
        };

        // Act
        var response = await _client.PostAsync("/tickets/import", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ImportSummaryResponse>(JsonOptions);
        result!.Successful.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task PostImport_WithXmlFile_Returns200AndImportSummary()
    {
        // Arrange
        var xmlContent = "<?xml version=\"1.0\"?><tickets><ticket><customer_id>C1</customer_id><customer_email>test@example.com</customer_email><customer_name>User</customer_name><subject>Subject</subject><description>This is a valid description text</description></ticket></tickets>";
        var memStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));
        var formContent = new MultipartFormDataContent
        {
            { new StreamContent(memStream), "file", "test.xml" }
        };

        // Act
        var response = await _client.PostAsync("/tickets/import", formContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ImportSummaryResponse>(JsonOptions);
        result!.Successful.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task PostImport_CreatesTicketsInRepository()
    {
        // Arrange
        var csvContent = "customer_id,customer_email,customer_name,subject,description\n" +
                        "C1,test@example.com,User,Subject,This is a valid description text";
        var memStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));
        var formContent = new MultipartFormDataContent
        {
            { new StreamContent(memStream), "file", "test.csv" }
        };

        // Act
        await _client.PostAsync("/tickets/import", formContent);

        // Assert
        var response = await _client.GetAsync("/tickets");
        var tickets = await response.Content.ReadFromJsonAsync<List<TicketResponse>>(JsonOptions);
        tickets.Should().HaveCountGreaterThanOrEqualTo(1);
        tickets.Should().Contain(t => t.CustomerId == "C1");
    }
}

/// <summary>Response DTO for import summary.</summary>
internal sealed record ImportSummaryResponse(
    int Total,
    int Successful,
    int Failed,
    IReadOnlyList<ImportErrorResponse> Errors
);

/// <summary>Response DTO for import error details.</summary>
internal sealed record ImportErrorResponse(
    int Row,
    string Message
);

/// <summary>Response DTO for classification results.</summary>
internal sealed record ClassificationResponse(
    string Category,
    string Priority,
    double Confidence,
    string Reasoning,
    IReadOnlyList<string> KeywordsFound
);
