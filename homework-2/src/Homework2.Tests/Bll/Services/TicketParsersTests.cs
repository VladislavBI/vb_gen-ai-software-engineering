#pragma warning disable IDE0005, IDE0008
using FluentAssertions;
using Homework2.Bll.Services;
#pragma warning restore IDE0005, IDE0008

namespace Homework2.Tests.Bll.Services;

/// <summary>Unit tests for ticket parsers (CSV, JSON, XML).</summary>
public sealed class TicketParsersTests
{
    [Fact]
    public void ParseCsv_WithValidCsv_ReturnsRecords()
    {
        // Arrange
        var csvContent = "customer_id,customer_email,customer_name,subject,description\nC1,test1@example.com,User1,Subject1,This is a valid description text\nC2,test2@example.com,User2,Subject2,Another valid description text here";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var records = TicketParsers.ParseCsv(stream);

        // Assert
        records.Should().HaveCount(2);
        records[0].CustomerId.Should().Be("C1");
        records[0].CustomerEmail.Should().Be("test1@example.com");
    }

    [Fact]
    public void ParseJson_WithValidJson_ReturnsRecords()
    {
        // Arrange
        var jsonContent = "[{\"customer_id\":\"C1\",\"customer_email\":\"test1@example.com\",\"customer_name\":\"User1\",\"subject\":\"Subject1\",\"description\":\"This is a valid description text\"}]";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Act
        var records = TicketParsers.ParseJson(stream);

        // Assert
        records.Should().HaveCount(1);
        records[0].CustomerId.Should().Be("C1");
    }

    [Fact]
    public void ParseXml_WithValidXml_ReturnsRecords()
    {
        // Arrange
        var xmlContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?><tickets><ticket><customer_id>C1</customer_id><customer_email>test@example.com</customer_email><customer_name>User1</customer_name><subject>Subject1</subject><description>This is a valid description text</description></ticket></tickets>";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));

        // Act
        var records = TicketParsers.ParseXml(stream);

        // Assert
        records.Should().HaveCount(1);
        records[0].CustomerId.Should().Be("C1");
    }

    [Fact]
    public void ParseCsv_WithEmptyContent_ReturnsEmpty()
    {
        var csvContent = "customer_id,customer_email,customer_name,subject,description";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));
        var records = TicketParsers.ParseCsv(stream);
        records.Should().BeEmpty();
    }

    [Fact]
    public void ParseJson_WithEmptyArray_ReturnsEmpty()
    {
        var jsonContent = "[]";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));
        var records = TicketParsers.ParseJson(stream);
        records.Should().BeEmpty();
    }

    [Fact]
    public void ParseXml_WithEmptyTickets_ReturnsEmpty()
    {
        var xmlContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?><tickets></tickets>";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xmlContent));
        var records = TicketParsers.ParseXml(stream);
        records.Should().BeEmpty();
    }
}
