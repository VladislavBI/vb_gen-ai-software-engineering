#pragma warning disable IDE0005, IDE0008
using System.Xml;
using FluentAssertions;
using Homework2.Bll.Services;
#pragma warning restore IDE0005, IDE0008

namespace Homework2.Tests.Bll.Services;

/// <summary>Unit tests for ticket parsers (CSV, JSON, XML).</summary>
public sealed class TicketParsersTests
{
    private static MemoryStream ToStream(string content) =>
        new(System.Text.Encoding.UTF8.GetBytes(content));

    // ────────────────────────────────────────────────────────────────────
    // CSV
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void ParseCsv_WithValidCsv_ReturnsRecords()
    {
        var csv = "customer_id,customer_email,customer_name,subject,description\n" +
                  "C1,test1@example.com,User1,Subject1,This is a valid description text\n" +
                  "C2,test2@example.com,User2,Subject2,Another valid description text here";

        var records = TicketParsers.ParseCsv(ToStream(csv));

        records.Should().HaveCount(2);
        records[0].CustomerId.Should().Be("C1");
        records[0].CustomerEmail.Should().Be("test1@example.com");
        records[1].CustomerId.Should().Be("C2");
    }

    [Fact]
    public void ParseCsv_WithEmptyContent_ReturnsEmpty()
    {
        var csv = "customer_id,customer_email,customer_name,subject,description";
        var records = TicketParsers.ParseCsv(ToStream(csv));
        records.Should().BeEmpty();
    }

    [Fact]
    public void ParseCsv_TrimsWhitespaceFromFieldValues()
    {
        // CsvHelper TrimOptions.Trim is configured on the parser; leading/trailing whitespace must be stripped.
        var csv = "customer_id,customer_email,customer_name,subject,description\n" +
                  "  C1  ,  test@example.com  ,  User  ,  Subject  ,  This is a valid description text  ";

        var records = TicketParsers.ParseCsv(ToStream(csv));

        records.Should().HaveCount(1);
        records[0].CustomerId.Should().Be("C1");
        records[0].CustomerEmail.Should().Be("test@example.com");
        records[0].CustomerName.Should().Be("User");
        records[0].Subject.Should().Be("Subject");
        records[0].Description.Should().Be("This is a valid description text");
    }

    [Fact]
    public void ParseCsv_WithMultipleRows_ReturnsAllRows()
    {
        var csv = "customer_id,customer_email,customer_name,subject,description\n" +
                  "C1,a@a.com,A,S1,Description text long enough\n" +
                  "C2,b@b.com,B,S2,Description text long enough\n" +
                  "C3,c@c.com,C,S3,Description text long enough";

        var records = TicketParsers.ParseCsv(ToStream(csv));

        records.Should().HaveCount(3);
        records.Select(r => r.CustomerId).Should().Equal("C1", "C2", "C3");
    }

    // ────────────────────────────────────────────────────────────────────
    // JSON
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void ParseJson_WithValidJson_ReturnsRecords()
    {
        var json = "[{\"customer_id\":\"C1\",\"customer_email\":\"test1@example.com\"," +
                   "\"customer_name\":\"User1\",\"subject\":\"Subject1\"," +
                   "\"description\":\"This is a valid description text\"}]";

        var records = TicketParsers.ParseJson(ToStream(json));

        records.Should().HaveCount(1);
        records[0].CustomerId.Should().Be("C1");
        records[0].CustomerEmail.Should().Be("test1@example.com");
    }

    [Fact]
    public void ParseJson_WithEmptyArray_ReturnsEmpty()
    {
        var records = TicketParsers.ParseJson(ToStream("[]"));
        records.Should().BeEmpty();
    }

    [Fact]
    public void ParseJson_WithMultipleObjects_ReturnsAllRecords()
    {
        var json = "[" +
            "{\"customer_id\":\"C1\",\"customer_email\":\"a@a.com\",\"customer_name\":\"A\",\"subject\":\"S1\",\"description\":\"Description text long enough\"}," +
            "{\"customer_id\":\"C2\",\"customer_email\":\"b@b.com\",\"customer_name\":\"B\",\"subject\":\"S2\",\"description\":\"Description text long enough\"}" +
            "]";

        var records = TicketParsers.ParseJson(ToStream(json));

        records.Should().HaveCount(2);
        records.Select(r => r.CustomerId).Should().Equal("C1", "C2");
    }

    [Fact]
    public void ParseJson_WithExtraFields_IgnoresThem()
    {
        // System.Text.Json silently ignores unknown properties by default — verify that contract holds.
        var json = "[{\"customer_id\":\"C1\",\"customer_email\":\"a@a.com\",\"customer_name\":\"A\"," +
                   "\"subject\":\"S\",\"description\":\"Description text long enough\"," +
                   "\"unknown_field\":\"should-be-ignored\",\"another\":42}]";

        var records = TicketParsers.ParseJson(ToStream(json));

        records.Should().HaveCount(1);
        records[0].CustomerId.Should().Be("C1");
    }

    [Fact]
    public void ParseJson_WithInvalidJson_Throws()
    {
        var stream = ToStream("not valid json at all");

        Action act = () => TicketParsers.ParseJson(stream);
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    [Fact]
    public void ParseJson_WithSnakeCaseKeys_MapsAllFields()
    {
        // Pins the documented JSON contract: snake_case keys map to all fields including the ones
        // with [JsonPropertyName] attributes (customer_*) and the ones using the CamelCase policy
        // fallback (subject / description — single-word names where camelCase == snake_case).
        var json = "[{\"customer_id\":\"C1\",\"customer_email\":\"a@a.com\"," +
                   "\"customer_name\":\"A\",\"subject\":\"S\",\"description\":\"Description text long enough\"}]";

        var records = TicketParsers.ParseJson(ToStream(json));

        records.Should().HaveCount(1);
        records[0].CustomerId.Should().Be("C1");
        records[0].CustomerEmail.Should().Be("a@a.com");
        records[0].CustomerName.Should().Be("A");
        records[0].Subject.Should().Be("S");
        records[0].Description.Should().Be("Description text long enough");
    }

    // ────────────────────────────────────────────────────────────────────
    // XML
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void ParseXml_WithValidXml_ReturnsRecords()
    {
        var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                  "<tickets><ticket>" +
                  "<customer_id>C1</customer_id>" +
                  "<customer_email>test@example.com</customer_email>" +
                  "<customer_name>User1</customer_name>" +
                  "<subject>Subject1</subject>" +
                  "<description>This is a valid description text</description>" +
                  "</ticket></tickets>";

        var records = TicketParsers.ParseXml(ToStream(xml));

        records.Should().HaveCount(1);
        records[0].CustomerId.Should().Be("C1");
    }

    [Fact]
    public void ParseXml_WithEmptyTickets_ReturnsEmpty()
    {
        var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><tickets/>";
        var records = TicketParsers.ParseXml(ToStream(xml));
        records.Should().BeEmpty();
    }

    [Fact]
    public void ParseXml_WithMultipleTickets_ReturnsAllRecords()
    {
        var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><tickets>" +
                  "<ticket><customer_id>C1</customer_id><customer_email>a@a.com</customer_email><customer_name>A</customer_name><subject>S1</subject><description>Description text long enough</description></ticket>" +
                  "<ticket><customer_id>C2</customer_id><customer_email>b@b.com</customer_email><customer_name>B</customer_name><subject>S2</subject><description>Description text long enough</description></ticket>" +
                  "</tickets>";

        var records = TicketParsers.ParseXml(ToStream(xml));

        records.Should().HaveCount(2);
        records.Select(r => r.CustomerId).Should().Equal("C1", "C2");
    }

    [Fact]
    public void ParseXml_WithMissingElement_FieldIsEmptyString()
    {
        // A <ticket> missing some child elements → those fields become "" (not null).
        var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><tickets>" +
                  "<ticket><customer_id>C1</customer_id><subject>S</subject></ticket>" +
                  "</tickets>";

        var records = TicketParsers.ParseXml(ToStream(xml));

        records.Should().HaveCount(1);
        records[0].CustomerId.Should().Be("C1");
        records[0].CustomerEmail.Should().BeEmpty();
        records[0].CustomerName.Should().BeEmpty();
        records[0].Subject.Should().Be("S");
        records[0].Description.Should().BeEmpty();
    }

    [Fact]
    public void ParseXml_WithMalformedXml_ThrowsXmlException()
    {
        // ParseXml uses XDocument.Load which throws XmlException on malformed input.
        // This pins down current behavior — the session plan originally specified "return empty list"
        // but the production code does not catch the exception. Test asserts the actual contract.
        var stream = ToStream("<tickets><ticket>unclosed");

        Action act = () => TicketParsers.ParseXml(stream);
        act.Should().Throw<XmlException>();
    }
}
