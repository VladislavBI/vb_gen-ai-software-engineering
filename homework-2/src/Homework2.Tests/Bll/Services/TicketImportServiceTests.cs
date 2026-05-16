#pragma warning disable IDE0005, IDE0008
using FluentAssertions;
using Homework2.Bll.Domain;
using Homework2.Bll.Services;
using Homework2.Dal.Repositories;
#pragma warning restore IDE0005, IDE0008

namespace Homework2.Tests.Bll.Services;

/// <summary>Unit tests for TicketImportService.</summary>
public sealed class TicketImportServiceTests
{
    private readonly InMemoryTicketRepository _repository = new();
    private readonly TicketService _ticketService;
    private readonly TicketImportService _importService;

    public TicketImportServiceTests()
    {
        _ticketService = new TicketService(_repository);
        _importService = new TicketImportService(_ticketService);
    }

    [Fact]
    public async Task ImportAsync_WithValidCsvStream_ReturnsSuccessfulCount()
    {
        // Arrange
        var csvContent = "customer_id,customer_email,customer_name,subject,description\n" +
                        "C1,test1@example.com,User1,Subject1,This is a valid description text\n" +
                        "C2,test2@example.com,User2,Subject2,Another valid description text here";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportAsync(stream, "csv");

        // Assert
        result.Total.Should().Be(2);
        result.Successful.Should().Be(2);
        result.Failed.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_WithValidJsonStream_ReturnsSuccessfulCount()
    {
        // Arrange
        var jsonContent = "[" +
            "{\"customer_id\":\"C1\",\"customer_email\":\"test1@example.com\",\"customer_name\":\"User1\",\"subject\":\"Subject1\",\"description\":\"This is a valid description text\"}," +
            "{\"customer_id\":\"C2\",\"customer_email\":\"test2@example.com\",\"customer_name\":\"User2\",\"subject\":\"Subject2\",\"description\":\"Another valid description text here\"}" +
            "]";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Act
        var result = await _importService.ImportAsync(stream, "json");

        // Assert
        result.Total.Should().Be(2);
        result.Successful.Should().Be(2);
        result.Failed.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_WithValidXmlStream_ReturnsSuccessfulCount()
    {
        // Arrange
        var xmlContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?><tickets>" +
            "<ticket><customer_id>C1</customer_id><customer_email>test1@example.com</customer_email><customer_name>User1</customer_name><subject>Subject1</subject><description>This is a valid description text</description></ticket>" +
            "<ticket><customer_id>C2</customer_id><customer_email>test2@example.com</customer_email><customer_name>User2</customer_name><subject>Subject2</subject><description>Another valid description text here</description></ticket>" +
            "</tickets>";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));

        // Act
        var result = await _importService.ImportAsync(stream, "xml");

        // Assert
        result.Total.Should().Be(2);
        result.Successful.Should().Be(2);
        result.Failed.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportAsync_WithInvalidEmail_ReturnsFailedCount()
    {
        // Arrange
        var csvContent = "customer_id,customer_email,customer_name,subject,description\n" +
                        "C1,invalid-email,User1,Subject1,This is a valid description text";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportAsync(stream, "csv");

        // Assert
        result.Total.Should().Be(1);
        result.Successful.Should().Be(0);
        result.Failed.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Row.Should().Be(1);
        result.Errors[0].Message.Should().Contain("customer_email");
    }

    [Fact]
    public async Task ImportAsync_WithShortDescription_ReturnsFailedCount()
    {
        // Arrange
        var csvContent = "customer_id,customer_email,customer_name,subject,description\n" +
                        "C1,test@example.com,User1,Subject1,short";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportAsync(stream, "csv");

        // Assert
        result.Failed.Should().Be(1);
        result.Total.Should().Be(1);
        result.Successful.Should().Be(0);
        result.Errors[0].Message.Should().Contain("description");
    }

    [Fact]
    public async Task ImportAsync_WithMissingCustomerId_ReturnsFailedCount()
    {
        // Arrange
        var csvContent = "customer_id,customer_email,customer_name,subject,description\n" +
                        ",test@example.com,User1,Subject1,This is a valid description text";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportAsync(stream, "csv");

        // Assert
        result.Failed.Should().Be(1);
        result.Total.Should().Be(1);
        result.Successful.Should().Be(0);
        result.Errors[0].Message.Should().Contain("customer_id");
    }

    [Fact]
    public async Task ImportAsync_WithMultipleErrors_ListsAllErrors()
    {
        // Arrange
        var csvContent = "customer_id,customer_email,customer_name,subject,description\n" +
                        ",bad-email,,S,short";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportAsync(stream, "csv");

        // Assert
        result.Failed.Should().Be(1);
        result.Total.Should().Be(1);
        result.Successful.Should().Be(0);
        result.Errors[0].Message.Should().Contain("customer_id");
        result.Errors[0].Message.Should().Contain("customer_email");
        result.Errors[0].Message.Should().Contain("customer_name");
        result.Errors[0].Message.Should().Contain("description");
    }

    [Fact]
    public async Task ImportAsync_WithUnsupportedFormat_ThrowsArgumentException()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _importService.ImportAsync(stream, "txt"));
    }

    [Fact]
    public async Task ImportAsync_WithMixedValidAndInvalid_CountsBoth()
    {
        // Arrange
        var csvContent = "customer_id,customer_email,customer_name,subject,description\n" +
                        "C1,test1@example.com,User1,Subject1,This is a valid description text\n" +
                        "C2,bad-email,User2,Subject2,Another valid description text here\n" +
                        "C3,test3@example.com,User3,Subject3,Third valid description text here";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportAsync(stream, "csv");

        // Assert
        result.Total.Should().Be(3);
        result.Successful.Should().Be(2);
        result.Failed.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Row.Should().Be(2);
    }

    [Fact]
    public async Task ImportAsync_CreatesTicketsInRepository()
    {
        // Arrange
        var csvContent = "customer_id,customer_email,customer_name,subject,description\n" +
                        "C1,test@example.com,User1,Subject1,This is a valid description text";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        await _importService.ImportAsync(stream, "csv");

        // Assert
        var tickets = await _ticketService.GetAllAsync();
        tickets.Should().HaveCount(1);
        tickets[0].CustomerId.Should().Be("C1");
        tickets[0].CustomerEmail.Should().Be("test@example.com");
    }

    [Fact]
    public async Task ImportAsync_WithEmptyStream_ReturnsZeroRecords()
    {
        // Arrange
        var csvContent = "customer_id,customer_email,customer_name,subject,description";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _importService.ImportAsync(stream, "csv");

        // Assert
        result.Total.Should().Be(0);
        result.Successful.Should().Be(0);
        result.Failed.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }
}

/// <summary>Unit tests for RawTicketImportValidator.</summary>
public sealed class RawTicketImportValidatorTests
{
    [Fact]
    public void Validate_WithValidRecord_ReturnsEmpty()
    {
        // Arrange
        var record = new RawTicketImport("C1", "test@example.com", "User", "Subject", "This is a valid description text");

        // Act
        var errors = RawTicketImportValidator.Validate(record);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMissingCustomerId_ReturnsError()
    {
        // Arrange
        var record = new RawTicketImport("", "test@example.com", "User", "Subject", "This is a valid description text");

        // Act
        var errors = RawTicketImportValidator.Validate(record);

        // Assert
        errors.Should().ContainSingle(e => e.Contains("customer_id"));
    }

    [Fact]
    public void Validate_WithInvalidEmail_ReturnsError()
    {
        // Arrange
        var record = new RawTicketImport("C1", "not-an-email", "User", "Subject", "This is a valid description text");

        // Act
        var errors = RawTicketImportValidator.Validate(record);

        // Assert
        errors.Should().ContainSingle(e => e.Contains("customer_email"));
    }

    [Fact]
    public void Validate_WithMissingCustomerName_ReturnsError()
    {
        // Arrange
        var record = new RawTicketImport("C1", "test@example.com", "", "Subject", "This is a valid description text");

        // Act
        var errors = RawTicketImportValidator.Validate(record);

        // Assert
        errors.Should().ContainSingle(e => e.Contains("customer_name"));
    }

    [Fact]
    public void Validate_WithShortDescription_ReturnsError()
    {
        // Arrange
        var record = new RawTicketImport("C1", "test@example.com", "User", "Subject", "short");

        // Act
        var errors = RawTicketImportValidator.Validate(record);

        // Assert
        errors.Should().ContainSingle(e => e.Contains("description"));
    }

    [Fact]
    public void Validate_WithLongDescription_ReturnsError()
    {
        // Arrange
        var longDesc = new string('a', 2001);
        var record = new RawTicketImport("C1", "test@example.com", "User", "Subject", longDesc);

        // Act
        var errors = RawTicketImportValidator.Validate(record);

        // Assert
        errors.Should().ContainSingle(e => e.Contains("description"));
    }

    [Fact]
    public void Validate_WithEmptySubject_ReturnsError()
    {
        // Arrange
        var record = new RawTicketImport("C1", "test@example.com", "User", "", "This is a valid description text");

        // Act
        var errors = RawTicketImportValidator.Validate(record);

        // Assert
        errors.Should().ContainSingle(e => e.Contains("subject"));
    }

    [Fact]
    public void Validate_WithLongSubject_ReturnsError()
    {
        // Arrange
        var longSubject = new string('a', 201);
        var record = new RawTicketImport("C1", "test@example.com", "User", longSubject, "This is a valid description text");

        // Act
        var errors = RawTicketImportValidator.Validate(record);

        // Assert
        errors.Should().ContainSingle(e => e.Contains("subject"));
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var record = new RawTicketImport("", "bad-email", "", "S", "short");

        // Act
        var errors = RawTicketImportValidator.Validate(record);

        // Assert
        errors.Should().HaveCountGreaterThanOrEqualTo(4);
    }
}
