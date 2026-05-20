using System.Collections.Concurrent;
using Homework2.Bll.Abstractions;
using Homework2.Bll.Domain;

namespace Homework2.Dal.Repositories;

/// <summary>In-memory implementation of ticket repository using ConcurrentDictionary.</summary>
public sealed class InMemoryTicketRepository : ITicketRepository
{
    private readonly ConcurrentDictionary<Guid, Ticket> _store = [];

    /// <summary>Get all tickets.</summary>
    public Task<IReadOnlyList<Ticket>> GetAllAsync()
    {
        IReadOnlyList<Ticket> tickets = _store.Values.ToList().AsReadOnly();
        return Task.FromResult(tickets);
    }

    /// <summary>Get ticket by ID.</summary>
    public Task<Ticket?> GetByIdAsync(Guid id)
    {
        bool found = _store.TryGetValue(id, out Ticket? ticket);
        return Task.FromResult(found ? ticket : null);
    }

    /// <summary>Create a new ticket.</summary>
    public Task<Ticket> CreateAsync(Ticket ticket)
    {
        bool success = _store.TryAdd(ticket.Id, ticket);
        if (!success)
        {
            throw new InvalidOperationException($"Ticket with id {ticket.Id} already exists.");
        }

        return Task.FromResult(ticket);
    }

    /// <summary>Update an existing ticket.</summary>
    public Task<Ticket?> UpdateAsync(Ticket ticket)
    {
        bool found = _store.TryGetValue(ticket.Id, out _);
        if (!found)
        {
            return Task.FromResult<Ticket?>(null);
        }

        _store[ticket.Id] = ticket;
        return Task.FromResult<Ticket?>(ticket);
    }

    /// <summary>Delete a ticket.</summary>
    public Task<bool> DeleteAsync(Guid id)
    {
        bool removed = _store.TryRemove(id, out _);
        return Task.FromResult(removed);
    }
}
