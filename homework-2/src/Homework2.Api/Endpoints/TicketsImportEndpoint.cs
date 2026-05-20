using Homework2.Bll.Services;

namespace Homework2.Api.Endpoints;

/// <summary>Endpoints for ticket import operations.</summary>
internal static class TicketsImportEndpoint
{
    /// <summary>Maps ticket import endpoint.</summary>
    public static void MapTicketsImport(this WebApplication app)
    {
        _ = app.MapPost("/tickets/import", ImportTickets)
            .WithName("ImportTickets")
            .Produces<ImportResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> ImportTickets(
        HttpContext context,
        TicketImportService service)
    {
        IFormFileCollection files = context.Request.Form.Files;
        if (files.Count == 0)
        {
            return Results.BadRequest(new { error = "No file provided" });
        }

        IFormFile file = files[0];
        if (file.Length == 0)
        {
            return Results.BadRequest(new { error = "File is empty" });
        }

        string extension = Path.GetExtension(file.FileName).TrimStart('.');
        if (string.IsNullOrEmpty(extension))
        {
            return Results.BadRequest(new { error = "File has no extension" });
        }

        try
        {
            using Stream stream = file.OpenReadStream();
            ImportResult result = await service.ImportAsync(stream, extension);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = $"Failed to parse file: {ex.Message}" });
        }
    }
}
