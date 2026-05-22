using Homework2.Bll.Abstractions;
using Homework2.Bll.Domain;

namespace Homework2.Bll.Services;

/// <summary>Service for ticket operations.</summary>
public class TicketService
{
    private readonly ITicketRepository _repository;

    /// <summary>Initializes a new instance of the TicketService class.</summary>
    public TicketService(ITicketRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Creates a new ticket.</summary>
    public async Task<Ticket> CreateAsync(string customerId, string customerEmail, string customerName, string subject, string description, Category category = Category.Other, Priority priority = Priority.Medium)
    {
        Ticket ticket = new(
            Id: Guid.NewGuid(),
            CustomerId: customerId,
            CustomerEmail: customerEmail,
            CustomerName: customerName,
            Subject: subject,
            Description: description,
            Category: category,
            Priority: priority,
            Status: Status.New,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            ResolvedAt: null,
            AssignedTo: null,
            Tags: [],
            Metadata: new Metadata(Source: "api", Browser: null, DeviceType: null)
        );

        return await _repository.CreateAsync(ticket);
    }

    /// <summary>Gets all tickets with optional filtering by category, priority, and status.</summary>
    public async Task<IReadOnlyList<Ticket>> GetAllAsync(Category? category = null, Priority? priority = null, Status? status = null)
    {
        IReadOnlyList<Ticket> tickets = await _repository.GetAllAsync();

        return tickets
            .Where(t => !category.HasValue || t.Category == category.Value)
            .Where(t => !priority.HasValue || t.Priority == priority.Value)
            .Where(t => !status.HasValue || t.Status == status.Value)
            .ToList();
    }

    /// <summary>Gets a ticket by ID.</summary>
    public async Task<Ticket?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    /// <summary>Updates an existing ticket.</summary>
    public async Task<Ticket?> UpdateAsync(Guid id, string? subject = null, string? description = null, Category? category = null, Priority? priority = null, Status? status = null, string? assignedTo = null, IReadOnlyList<string>? tags = null)
    {
        Ticket? existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return null;
        }

        Ticket updated = existing with
        {
            Subject = subject ?? existing.Subject,
            Description = description ?? existing.Description,
            Category = category ?? existing.Category,
            Priority = priority ?? existing.Priority,
            Status = status ?? existing.Status,
            AssignedTo = assignedTo ?? existing.AssignedTo,
            Tags = tags ?? existing.Tags,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _repository.UpdateAsync(updated);
    }

    /// <summary>Deletes a ticket.</summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _repository.DeleteAsync(id);
    }
}
