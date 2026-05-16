using Homework2.Bll.Domain;

namespace Homework2.Bll.Abstractions;

/// <summary>Repository interface for ticket data access.</summary>
public interface ITicketRepository
{
    /// <summary>Get all tickets.</summary>
    Task<IReadOnlyList<Ticket>> GetAllAsync();

    /// <summary>Get ticket by ID.</summary>
    Task<Ticket?> GetByIdAsync(Guid id);

    /// <summary>Create a new ticket.</summary>
    Task<Ticket> CreateAsync(Ticket ticket);

    /// <summary>Update an existing ticket.</summary>
    Task<Ticket?> UpdateAsync(Ticket ticket);

    /// <summary>Delete a ticket.</summary>
    Task<bool> DeleteAsync(Guid id);
}
