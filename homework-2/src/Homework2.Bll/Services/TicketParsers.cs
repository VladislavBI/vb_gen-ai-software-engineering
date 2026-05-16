using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace Homework2.Bll.Services;

/// <summary>Raw ticket import record from parsed file.</summary>
public sealed record RawTicketImport(
    [property: JsonPropertyName("customer_id")]
    string CustomerId,
    [property: JsonPropertyName("customer_email")]
    string CustomerEmail,
    [property: JsonPropertyName("customer_name")]
    string CustomerName,
    string Subject,
    string Description
);

/// <summary>Parsers for converting CSV, JSON, and XML files to raw ticket import records.</summary>
internal static class TicketParsers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Parses a CSV stream into RawTicketImport objects.</summary>
    /// <remarks>Expected columns: customer_id, customer_email, customer_name, subject, description</remarks>
    public static IReadOnlyList<RawTicketImport> ParseCsv(Stream stream)
    {
        CsvConfiguration config = new(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
        };

        using StreamReader reader = new(stream);
        using CsvReader csv = new(reader, config);

        List<RawTicketImport> records = [];
        if (!csv.Read())
        {
            return records;
        }

        _ = csv.ReadHeader();

        while (csv.Read())
        {
            RawTicketImport record = new(
                CustomerId: csv.GetField("customer_id") ?? string.Empty,
                CustomerEmail: csv.GetField("customer_email") ?? string.Empty,
                CustomerName: csv.GetField("customer_name") ?? string.Empty,
                Subject: csv.GetField("subject") ?? string.Empty,
                Description: csv.GetField("description") ?? string.Empty
            );

            records.Add(record);
        }

        return records;
    }

    /// <summary>Parses a JSON stream into RawTicketImport objects.</summary>
    /// <remarks>Expects an array of objects with customer_id, customer_email, customer_name, subject, description fields.</remarks>
    public static IReadOnlyList<RawTicketImport> ParseJson(Stream stream)
    {
        using StreamReader reader = new(stream);
        string json = reader.ReadToEnd();

        List<RawTicketImport>? records = JsonSerializer.Deserialize<List<RawTicketImport>>(json, JsonOptions);
        return records ?? [];
    }

    /// <summary>Parses an XML stream into RawTicketImport objects.</summary>
    /// <remarks>Expects root element 'tickets' with child 'ticket' elements containing customer_id, customer_email, customer_name, subject, description.</remarks>
    public static IReadOnlyList<RawTicketImport> ParseXml(Stream stream)
    {
        var doc = XDocument.Load(stream);
        XElement? root = doc.Root;

        if (root == null)
        {
            return [];
        }

        List<RawTicketImport> records = [];

        foreach (XElement ticketElement in root.Elements("ticket"))
        {
            string customerId = ticketElement.Element("customer_id")?.Value ?? string.Empty;
            string customerEmail = ticketElement.Element("customer_email")?.Value ?? string.Empty;
            string customerName = ticketElement.Element("customer_name")?.Value ?? string.Empty;
            string subject = ticketElement.Element("subject")?.Value ?? string.Empty;
            string description = ticketElement.Element("description")?.Value ?? string.Empty;

            RawTicketImport record = new(
                CustomerId: customerId,
                CustomerEmail: customerEmail,
                CustomerName: customerName,
                Subject: subject,
                Description: description
            );

            records.Add(record);
        }

        return records;
    }
}
