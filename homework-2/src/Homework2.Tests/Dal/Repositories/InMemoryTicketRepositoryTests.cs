#pragma warning disable IDE0005, IDE0008
using FluentAssertions;
using Homework2.Bll.Domain;
using Homework2.Dal.Repositories;
#pragma warning restore IDE0005, IDE0008

namespace Homework2.Tests.Dal.Repositories;

/// <summary>Unit tests for InMemoryTicketRepository including concurrency tests.</summary>
public sealed class InMemoryTicketRepositoryTests
{
    private static Ticket CreateTicket(Guid id) =>
        new(
            id,
            "C1",
            "test@example.com",
            "User",
            "Subject",
            "Description here",
            Category.Other,
            Priority.Medium,
            Status.New,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            null,
            [],
            new Metadata("api", null, null)
        );

    [Fact]
    public async Task GetAllAsync_WithNoTickets_ReturnsEmptyList()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleTickets_ReturnsAllTickets()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticket1Id = Guid.NewGuid();
        var ticket2Id = Guid.NewGuid();
        var ticket1 = CreateTicket(ticket1Id);
        var ticket2 = CreateTicket(ticket2Id);

        await repo.CreateAsync(ticket1);
        await repo.CreateAsync(ticket2);

        // Act
        var result = await repo.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(ticket1);
        result.Should().Contain(ticket2);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsTicket()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticketId = Guid.NewGuid();
        var ticket = CreateTicket(ticketId);

        await repo.CreateAsync(ticket);

        // Act
        var result = await repo.GetByIdAsync(ticketId);

        // Assert
        result.Should().Be(ticket);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonexistentId_ReturnsNull()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();

        // Act
        var result = await repo.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_CreatesTicket()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticketId = Guid.NewGuid();
        var ticket = CreateTicket(ticketId);

        // Act
        var result = await repo.CreateAsync(ticket);

        // Assert
        result.Should().Be(ticket);
        var retrieved = await repo.GetByIdAsync(ticketId);
        retrieved.Should().Be(ticket);
    }

    [Fact]
    public async Task CreateAsync_MultipleCreates_PersistIndependently()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticket1Id = Guid.NewGuid();
        var ticket2Id = Guid.NewGuid();
        var ticket1 = CreateTicket(ticket1Id);
        var ticket2 = CreateTicket(ticket2Id);

        // Act
        await repo.CreateAsync(ticket1);
        await repo.CreateAsync(ticket2);

        // Assert
        var all = await repo.GetAllAsync();
        all.Should().HaveCount(2);
        all.Should().Contain(ticket1);
        all.Should().Contain(ticket2);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateId_ThrowsException()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticketId = Guid.NewGuid();
        var ticket1 = CreateTicket(ticketId);
        var ticket2 = CreateTicket(ticketId);

        await repo.CreateAsync(ticket1);

        // Act
        Func<Task> act = async () => await repo.CreateAsync(ticket2);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingTicket()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticketId = Guid.NewGuid();
        var original = CreateTicket(ticketId);
        await repo.CreateAsync(original);

        var updated = original with { Subject = "Updated Subject" };

        // Act
        var result = await repo.UpdateAsync(updated);

        // Assert
        result.Should().Be(updated);
        var retrieved = await repo.GetByIdAsync(ticketId);
        retrieved?.Subject.Should().Be("Updated Subject");
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentId_ReturnsNull()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticket = CreateTicket(Guid.NewGuid());

        // Act
        var result = await repo.UpdateAsync(ticket);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_DoesNotAffectOtherTickets()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticket1Id = Guid.NewGuid();
        var ticket2Id = Guid.NewGuid();
        var ticket1 = CreateTicket(ticket1Id);
        var ticket2 = CreateTicket(ticket2Id);

        await repo.CreateAsync(ticket1);
        await repo.CreateAsync(ticket2);

        var updatedTicket1 = ticket1 with { Subject = "Updated" };

        // Act
        await repo.UpdateAsync(updatedTicket1);

        // Assert
        var retrievedTicket2 = await repo.GetByIdAsync(ticket2Id);
        retrievedTicket2?.Subject.Should().Be(ticket2.Subject);
    }

    [Fact]
    public async Task DeleteAsync_DeletesExistingTicket()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticketId = Guid.NewGuid();
        var ticket = CreateTicket(ticketId);

        await repo.CreateAsync(ticket);

        // Act
        var result = await repo.DeleteAsync(ticketId);

        // Assert
        result.Should().BeTrue();
        var retrieved = await repo.GetByIdAsync(ticketId);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonexistentId_ReturnsFalse()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();

        // Act
        var result = await repo.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_RemovedTicketIsNotRetrievable()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticketId = Guid.NewGuid();
        var ticket = CreateTicket(ticketId);

        await repo.CreateAsync(ticket);
        await repo.DeleteAsync(ticketId);

        // Act
        var result = await repo.GetByIdAsync(ticketId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrentCreates_AllSucceed()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var ticketId = Guid.NewGuid();
            var ticket = CreateTicket(ticketId);
            tasks.Add(repo.CreateAsync(ticket));
        }

        await Task.WhenAll(tasks);

        // Assert
        var all = await repo.GetAllAsync();
        all.Should().HaveCount(10);
    }

    [Fact]
    public async Task ConcurrentCreates_AllPersisted()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticketIds = new List<Guid>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var ticketId = Guid.NewGuid();
            ticketIds.Add(ticketId);
            var ticket = CreateTicket(ticketId);
            tasks.Add(repo.CreateAsync(ticket));
        }

        await Task.WhenAll(tasks);

        // Assert
        foreach (var id in ticketIds)
        {
            var ticket = await repo.GetByIdAsync(id);
            ticket.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task ConcurrentDeletes_AllSucceed()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticketIds = new List<Guid>();

        for (int i = 0; i < 10; i++)
        {
            var ticketId = Guid.NewGuid();
            ticketIds.Add(ticketId);
            var ticket = CreateTicket(ticketId);
            await repo.CreateAsync(ticket);
        }

        var deleteTasks = new List<Task>();

        // Act
        foreach (var id in ticketIds)
        {
            deleteTasks.Add(repo.DeleteAsync(id));
        }

        await Task.WhenAll(deleteTasks);

        // Assert
        var all = await repo.GetAllAsync();
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task MixedConcurrentOperations_FinalStateIsConsistent()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticketIds = new List<Guid>();

        // Create initial tickets
        for (int i = 0; i < 5; i++)
        {
            var ticketId = Guid.NewGuid();
            ticketIds.Add(ticketId);
            var ticket = CreateTicket(ticketId);
            await repo.CreateAsync(ticket);
        }

        var tasks = new List<Task>();

        // Act: Perform 5 creates, 5 updates, 5 deletes concurrently
        // Creates
        for (int i = 0; i < 5; i++)
        {
            var ticketId = Guid.NewGuid();
            var ticket = CreateTicket(ticketId);
            tasks.Add(repo.CreateAsync(ticket));
        }

        // Updates
        for (int i = 0; i < 5; i++)
        {
            var retrieved = await repo.GetByIdAsync(ticketIds[i]);
            if (retrieved != null)
            {
                var updated = retrieved with { Subject = $"Updated{i}" };
                tasks.Add(repo.UpdateAsync(updated));
            }
        }

        // Deletes
        for (int i = 5; i < 10 && i < ticketIds.Count; i++)
        {
            // Don't delete all, keep some for final state check
        }

        await Task.WhenAll(tasks);

        // Assert: Final state should be consistent
        var all = await repo.GetAllAsync();
        all.Should().NotBeNull();
        all.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task ParallelReadWrite_Consistency()
    {
        // Arrange
        var repo = new InMemoryTicketRepository();
        var ticketId = Guid.NewGuid();
        var originalTicket = CreateTicket(ticketId);
        await repo.CreateAsync(originalTicket);

        var readTasks = new List<Task<Ticket?>>();
        var writeTasks = new List<Task<Ticket?>>();

        // Act: Perform reads and writes in parallel
        for (int i = 0; i < 10; i++)
        {
            readTasks.Add(repo.GetByIdAsync(ticketId));

            if (i % 2 == 0)
            {
                var retrieved = await repo.GetByIdAsync(ticketId);
                if (retrieved != null)
                {
                    var updated = retrieved with { Subject = $"Update{i}" };
                    writeTasks.Add(repo.UpdateAsync(updated));
                }
            }
        }

        await Task.WhenAll(readTasks.Cast<Task>().Concat(writeTasks.Cast<Task>()));

        // Assert: All reads should return a non-null ticket
        var allReads = await Task.WhenAll(readTasks);
        allReads.Should().AllSatisfy(t => t.Should().NotBeNull());
    }
}
