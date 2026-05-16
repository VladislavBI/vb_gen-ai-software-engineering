using Homework2.Bll.Domain;

namespace Homework2.Api.Models;

/// <summary>Request DTO for creating a new ticket.</summary>
internal sealed record CreateTicketRequest(
    string CustomerId,
    string CustomerEmail,
    string CustomerName,
    string Subject,
    string Description,
    Category? Category = null,
    Priority? Priority = null,
    IReadOnlyList<string>? Tags = null
);

/// <summary>Request DTO for updating an existing ticket.</summary>
internal sealed record UpdateTicketRequest(
    string? Subject = null,
    string? Description = null,
    Category? Category = null,
    Priority? Priority = null,
    Status? Status = null,
    string? AssignedTo = null,
    IReadOnlyList<string>? Tags = null
);

/// <summary>Response DTO for ticket endpoints.</summary>
internal sealed record TicketResponse(
    Guid Id,
    string CustomerId,
    string CustomerEmail,
    string CustomerName,
    string Subject,
    string Description,
    string Category,
    string Priority,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ResolvedAt,
    string? AssignedTo,
    IReadOnlyList<string> Tags
);
