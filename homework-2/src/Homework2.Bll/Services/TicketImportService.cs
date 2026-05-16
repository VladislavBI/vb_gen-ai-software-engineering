namespace Homework2.Bll.Services;

/// <summary>Result of a bulk import operation.</summary>
public sealed record ImportResult(
    int Total,
    int Successful,
    int Failed,
    IReadOnlyList<ImportError> Errors
);

/// <summary>Details of a single import error.</summary>
public sealed record ImportError(
    int Row,
    string Message
);

/// <summary>Validator for raw ticket imports.</summary>
public sealed record RawTicketImportValidator
{
    /// <summary>Validates a raw ticket import record and returns validation errors.</summary>
    public static IReadOnlyList<string> Validate(RawTicketImport record)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(record.CustomerId))
        {
            errors.Add("'customer_id' is required.");
        }

        if (string.IsNullOrWhiteSpace(record.CustomerEmail))
        {
            errors.Add("'customer_email' is required.");
        }
        else if (!IsValidEmail(record.CustomerEmail))
        {
            errors.Add("'customer_email' must be a valid email address.");
        }

        if (string.IsNullOrWhiteSpace(record.CustomerName))
        {
            errors.Add("'customer_name' is required.");
        }

        if (string.IsNullOrWhiteSpace(record.Subject) || record.Subject.Length < 1 || record.Subject.Length > 200)
        {
            errors.Add("'subject' must be between 1 and 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(record.Description) || record.Description.Length < 10 || record.Description.Length > 2000)
        {
            errors.Add("'description' must be between 10 and 2000 characters.");
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            System.Net.Mail.MailAddress addr = new(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>Service for importing tickets from CSV, JSON, and XML files.</summary>
public class TicketImportService
{
    private readonly TicketService _ticketService;

    /// <summary>Initializes a new instance of the TicketImportService class.</summary>
    public TicketImportService(TicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>Imports tickets from a file stream.</summary>
    /// <param name="stream">The file stream to import from.</param>
    /// <param name="extension">The file extension (csv, json, or xml) to determine format.</param>
    /// <returns>A summary of the import operation.</returns>
    public async Task<ImportResult> ImportAsync(Stream stream, string extension)
    {
        IReadOnlyList<RawTicketImport> records = extension.ToUpperInvariant() switch
        {
            "CSV" => TicketParsers.ParseCsv(stream),
            "JSON" => TicketParsers.ParseJson(stream),
            "XML" => TicketParsers.ParseXml(stream),
            _ => throw new ArgumentException($"Unsupported file format: {extension}"),
        };

        List<ImportError> errors = [];
        int successful = 0;

        for (int i = 0; i < records.Count; i++)
        {
            RawTicketImport record = records[i];
            IReadOnlyList<string> validationErrors = RawTicketImportValidator.Validate(record);

            if (validationErrors.Count > 0)
            {
                string errorMessage = string.Join("; ", validationErrors);
                errors.Add(new ImportError(Row: i + 1, Message: errorMessage));
            }
            else
            {
                _ = await _ticketService.CreateAsync(
                    record.CustomerId,
                    record.CustomerEmail,
                    record.CustomerName,
                    record.Subject,
                    record.Description,
                    Domain.Category.Other,
                    Domain.Priority.Medium
                );

                successful++;
            }
        }

        return new ImportResult(
            Total: records.Count,
            Successful: successful,
            Failed: errors.Count,
            Errors: errors
        );
    }
}
