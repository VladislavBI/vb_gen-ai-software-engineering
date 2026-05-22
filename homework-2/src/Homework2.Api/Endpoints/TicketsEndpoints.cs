using FluentValidation;
using Homework2.Api.Models;
using Homework2.Bll.Domain;
using Homework2.Bll.Services;

namespace Homework2.Api.Endpoints;

/// <summary>Endpoints for ticket CRUD operations.</summary>
internal static class TicketsEndpoints
{
    /// <summary>Maps ticket endpoints.</summary>
    public static void MapTickets(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/tickets")
            .WithName("Tickets");

        _ = group.MapPost("/", CreateTicket)
            .WithName("CreateTicket")
            .Produces<TicketResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        _ = group.MapGet("/", GetAllTickets)
            .WithName("GetAllTickets")
            .Produces<IReadOnlyList<TicketResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        _ = group.MapGet("/{id}", GetTicketById)
            .WithName("GetTicketById")
            .Produces<TicketResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        _ = group.MapPut("/{id}", UpdateTicket)
            .WithName("UpdateTicket")
            .Produces<TicketResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        _ = group.MapDelete("/{id}", DeleteTicket)
            .WithName("DeleteTicket")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateTicket(
        CreateTicketRequest request,
        TicketService service,
        IValidator<CreateTicketRequest> validator,
        TicketClassifier classifier,
        bool autoClassify = false)
    {
        FluentValidation.Results.ValidationResult validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(
                validationResult.Errors
                    .GroupBy(failure => failure.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(failure => failure.ErrorMessage).ToArray()
                    )
            );
        }

        Ticket ticket = await service.CreateAsync(
            request.CustomerId,
            request.CustomerEmail,
            request.CustomerName,
            request.Subject,
            request.Description,
            request.Category ?? Category.Other,
            request.Priority ?? Priority.Medium
        );

        if (autoClassify)
        {
            ClassificationResult classification = classifier.Classify(ticket);
            ticket = await service.UpdateAsync(
                ticket.Id,
                category: classification.Category,
                priority: classification.Priority
            ) ?? ticket;
        }

        TicketResponse response = ToResponse(ticket);
        return Results.Created($"/tickets/{response.Id}", response);
    }

    private static async Task<IResult> GetAllTickets(
        TicketService service,
        string? category = null,
        string? priority = null,
        string? status = null)
    {
        // Validate category enum
        if (!string.IsNullOrWhiteSpace(category) && !TryParseSnakeCaseEnumValid<Category>(category, out Category? parsedCategory))
        {
            return Results.BadRequest(new { error = $"Invalid category value: {category}" });
        }

        // Validate priority enum
        if (!string.IsNullOrWhiteSpace(priority) && !TryParseSnakeCaseEnumValid<Priority>(priority, out Priority? parsedPriority))
        {
            return Results.BadRequest(new { error = $"Invalid priority value: {priority}" });
        }

        // Validate status enum
        if (!string.IsNullOrWhiteSpace(status) && !TryParseSnakeCaseEnumValid<Status>(status, out Status? parsedStatus))
        {
            return Results.BadRequest(new { error = $"Invalid status value: {status}" });
        }

        Category? filterCategory = TryParseSnakeCaseEnum<Category>(category);
        Priority? filterPriority = TryParseSnakeCaseEnum<Priority>(priority);
        Status? filterStatus = TryParseSnakeCaseEnum<Status>(status);

        IReadOnlyList<Ticket> tickets = await service.GetAllAsync(filterCategory, filterPriority, filterStatus);
        return Results.Ok(tickets.Select(ToResponse).ToList());
    }

    private static async Task<IResult> GetTicketById(Guid id, TicketService service)
    {
        Ticket? ticket = await service.GetByIdAsync(id);
        if (ticket is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToResponse(ticket));
    }

    private static async Task<IResult> UpdateTicket(
        Guid id,
        UpdateTicketRequest request,
        TicketService service,
        IValidator<UpdateTicketRequest> validator)
    {
        FluentValidation.Results.ValidationResult validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(
                validationResult.Errors
                    .GroupBy(failure => failure.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(failure => failure.ErrorMessage).ToArray()
                    )
            );
        }

        Ticket? updated = await service.UpdateAsync(
            id,
            request.Subject,
            request.Description,
            request.Category,
            request.Priority,
            request.Status,
            request.AssignedTo,
            request.Tags
        );

        if (updated is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(ToResponse(updated));
    }

    private static async Task<IResult> DeleteTicket(Guid id, TicketService service)
    {
        bool deleted = await service.DeleteAsync(id);
        if (!deleted)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }

    private static TicketResponse ToResponse(Ticket ticket)
    {
        return new TicketResponse(
            Id: ticket.Id,
            CustomerId: ticket.CustomerId,
            CustomerEmail: ticket.CustomerEmail,
            CustomerName: ticket.CustomerName,
            Subject: ticket.Subject,
            Description: ticket.Description,
            Category: EnumToSnakeCase(ticket.Category.ToString()),
            Priority: EnumToSnakeCase(ticket.Priority.ToString()),
            Status: EnumToSnakeCase(ticket.Status.ToString()),
            CreatedAt: ticket.CreatedAt,
            UpdatedAt: ticket.UpdatedAt,
            ResolvedAt: ticket.ResolvedAt,
            AssignedTo: ticket.AssignedTo,
            Tags: ticket.Tags
        );
    }

#pragma warning disable CA1308
    private static string EnumToSnakeCase(string enumValue) =>
        System.Text.RegularExpressions.Regex.Replace(enumValue, "(?<!^)(?=[A-Z])", "_").ToLowerInvariant();
#pragma warning restore CA1308

    private static bool TryParseSnakeCaseEnumValid<T>(string snakeCaseValue, out T? result) where T : struct, Enum
    {
        result = null;

        string pascalCaseValue = System.Text.RegularExpressions.Regex.Replace(
            snakeCaseValue,
            "_([a-z])",
            m => m.Groups[1].Value.ToUpperInvariant()
        );
        pascalCaseValue = char.ToUpperInvariant(pascalCaseValue[0]) + pascalCaseValue[1..];

        if (Enum.TryParse<T>(pascalCaseValue, ignoreCase: true, out T parsedResult))
        {
            result = parsedResult;
            return true;
        }

        return false;
    }

    private static T? TryParseSnakeCaseEnum<T>(string? snakeCaseValue) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(snakeCaseValue))
        {
            return null;
        }

        string pascalCaseValue = System.Text.RegularExpressions.Regex.Replace(
            snakeCaseValue,
            "_([a-z])",
            m => m.Groups[1].Value.ToUpperInvariant()
        );
        pascalCaseValue = char.ToUpperInvariant(pascalCaseValue[0]) + pascalCaseValue[1..];

        if (Enum.TryParse<T>(pascalCaseValue, ignoreCase: true, out T result))
        {
            return result;
        }

        return null;
    }
}
