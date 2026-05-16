using Homework2.Bll.Domain;
using Homework2.Bll.Services;

namespace Homework2.Api.Endpoints;

/// <summary>Endpoints for ticket classification.</summary>
internal static class TicketsClassifyEndpoint
{
    /// <summary>Maps ticket classification endpoint.</summary>
    public static void MapClassify(this WebApplication app)
    {
        _ = app.MapPost("/tickets/{id}/auto-classify", AutoClassify)
            .WithName("AutoClassify")
            .Produces<ClassificationResultResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> AutoClassify(Guid id, TicketService ticketService, TicketClassifier classifier)
    {
        Ticket? ticket = await ticketService.GetByIdAsync(id);
        if (ticket is null)
        {
            return Results.NotFound();
        }

        ClassificationResult classification = classifier.Classify(ticket);

        Ticket? updated = await ticketService.UpdateAsync(
            id,
            category: classification.Category,
            priority: classification.Priority
        );

        if (updated is null)
        {
            return Results.NotFound();
        }

        ClassificationResultResponse response = ToResponse(classification);
        return Results.Ok(response);
    }

#pragma warning disable CA1308
    private static ClassificationResultResponse ToResponse(ClassificationResult result)
    {
        string categorySnake = System.Text.RegularExpressions.Regex.Replace(result.Category.ToString(), "(?<!^)(?=[A-Z])", "_").ToLowerInvariant();
        string prioritySnake = result.Priority.ToString().ToLowerInvariant();

        return new ClassificationResultResponse(
            Category: categorySnake,
            Priority: prioritySnake,
            Confidence: result.Confidence,
            Reasoning: result.Reasoning,
            KeywordsFound: result.KeywordsFound
        );
    }
#pragma warning restore CA1308
}

/// <summary>Response for classification result.</summary>
internal sealed record ClassificationResultResponse(
    string Category,
    string Priority,
    double Confidence,
    string Reasoning,
    IReadOnlyList<string> KeywordsFound
);
