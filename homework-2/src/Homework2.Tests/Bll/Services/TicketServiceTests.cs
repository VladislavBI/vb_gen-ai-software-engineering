#pragma warning disable CA1826, IDE0005, IDE0008, S6608, IDE0058

using FluentAssertions;
using Homework2.Bll.Abstractions;
using Homework2.Bll.Domain;
using Homework2.Bll.Services;
using Moq;

#pragma warning restore CA1826, IDE0005, IDE0008, S6608, IDE0058

namespace Homework2.Tests.Bll.Services;

/// <summary>Unit tests for TicketService with mocked repository.</summary>
public sealed class TicketServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesTicketWithNewGuid()
    {
        // Arrange
        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Ticket>()))
            .ReturnsAsync((Ticket t) => t);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.CreateAsync("C1", "test@example.com", "User", "Subject", "Description here");

        // Assert
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_CreatesTicketWithCorrectCustomerFields()
    {
        // Arrange
        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Ticket>()))
            .ReturnsAsync((Ticket t) => t);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.CreateAsync("C1", "test@example.com", "Test User", "Subject", "Description here");

        // Assert
        result.CustomerId.Should().Be("C1");
        result.CustomerEmail.Should().Be("test@example.com");
        result.CustomerName.Should().Be("Test User");
    }

    [Fact]
    public async Task CreateAsync_CreatesTicketWithDefaultCategory()
    {
        // Arrange
        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Ticket>()))
            .ReturnsAsync((Ticket t) => t);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.CreateAsync("C1", "test@example.com", "User", "Subject", "Description here");

        // Assert
        result.Category.Should().Be(Category.Other);
    }

    [Fact]
    public async Task CreateAsync_CreatesTicketWithDefaultPriority()
    {
        // Arrange
        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Ticket>()))
            .ReturnsAsync((Ticket t) => t);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.CreateAsync("C1", "test@example.com", "User", "Subject", "Description here");

        // Assert
        result.Priority.Should().Be(Priority.Medium);
    }

    [Fact]
    public async Task CreateAsync_CreatesTicketWithNewStatus()
    {
        // Arrange
        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Ticket>()))
            .ReturnsAsync((Ticket t) => t);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.CreateAsync("C1", "test@example.com", "User", "Subject", "Description here");

        // Assert
        result.Status.Should().Be(Status.New);
    }

    [Fact]
    public async Task CreateAsync_CreatesTicketWithSourceApi()
    {
        // Arrange
        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Ticket>()))
            .ReturnsAsync((Ticket t) => t);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.CreateAsync("C1", "test@example.com", "User", "Subject", "Description here");

        // Assert
        result.Metadata.Source.Should().Be("api");
    }

    [Fact]
    public async Task CreateAsync_CreatesTicketWithRecentTimestamp()
    {
        // Arrange
        var beforeTime = DateTimeOffset.UtcNow;
        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Ticket>()))
            .ReturnsAsync((Ticket t) => t);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.CreateAsync("C1", "test@example.com", "User", "Subject", "Description here");

        // Assert
        result.CreatedAt.Should().BeOnOrAfter(beforeTime);
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetAllAsync_WithNoFilters_ReturnsAllTickets()
    {
        // Arrange
        var ticket1 = new Ticket(Guid.NewGuid(), "C1", "test1@example.com", "User1", "S1", "Desc", Category.Other, Priority.Medium, Status.New, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], new Metadata("api", null, null));
        var ticket2 = new Ticket(Guid.NewGuid(), "C2", "test2@example.com", "User2", "S2", "Desc", Category.AccountAccess, Priority.High, Status.New, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], new Metadata("api", null, null));

        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new[] { ticket1, ticket2 }.ToList().AsReadOnly());

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(ticket1);
        result.Should().Contain(ticket2);
    }

    [Fact]
    public async Task GetAllAsync_WithNoTickets_ReturnsEmptyList()
    {
        // Arrange
        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(Array.Empty<Ticket>().ToList().AsReadOnly());

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_FilterByCategory_ReturnsOnlyMatchingTickets()
    {
        // Arrange
        var ticket1 = new Ticket(Guid.NewGuid(), "C1", "test1@example.com", "User1", "S1", "Desc", Category.Other, Priority.Medium, Status.New, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], new Metadata("api", null, null));
        var ticket2 = new Ticket(Guid.NewGuid(), "C2", "test2@example.com", "User2", "S2", "Desc", Category.AccountAccess, Priority.High, Status.New, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], new Metadata("api", null, null));

        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new[] { ticket1, ticket2 }.ToList().AsReadOnly());

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.GetAllAsync(category: Category.AccountAccess);

        // Assert
        result.Should().HaveCount(1);
        result.First().Category.Should().Be(Category.AccountAccess);
    }

    [Fact]
    public async Task GetAllAsync_FilterByPriority_ReturnsOnlyMatchingTickets()
    {
        // Arrange
        var ticket1 = new Ticket(Guid.NewGuid(), "C1", "test1@example.com", "User1", "S1", "Desc", Category.Other, Priority.Medium, Status.New, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], new Metadata("api", null, null));
        var ticket2 = new Ticket(Guid.NewGuid(), "C2", "test2@example.com", "User2", "S2", "Desc", Category.AccountAccess, Priority.Urgent, Status.New, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], new Metadata("api", null, null));

        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new[] { ticket1, ticket2 }.ToList().AsReadOnly());

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.GetAllAsync(priority: Priority.Urgent);

        // Assert
        result.Should().HaveCount(1);
        result.First().Priority.Should().Be(Priority.Urgent);
    }

    [Fact]
    public async Task GetAllAsync_FilterByStatus_ReturnsOnlyMatchingTickets()
    {
        // Arrange
        var ticket1 = new Ticket(Guid.NewGuid(), "C1", "test1@example.com", "User1", "S1", "Desc", Category.Other, Priority.Medium, Status.New, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], new Metadata("api", null, null));
        var ticket2 = new Ticket(Guid.NewGuid(), "C2", "test2@example.com", "User2", "S2", "Desc", Category.AccountAccess, Priority.High, Status.InProgress, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], new Metadata("api", null, null));

        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new[] { ticket1, ticket2 }.ToList().AsReadOnly());

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.GetAllAsync(status: Status.InProgress);

        // Assert
        result.Should().HaveCount(1);
        result.First().Status.Should().Be(Status.InProgress);
    }

    [Fact]
    public async Task GetAllAsync_CombinedFilters_ReturnsTicketsMatchingAllCriteria()
    {
        // Arrange
        var ticket1 = new Ticket(Guid.NewGuid(), "C1", "test1@example.com", "User1", "S1", "Desc", Category.AccountAccess, Priority.Medium, Status.New, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], new Metadata("api", null, null));
        var ticket2 = new Ticket(Guid.NewGuid(), "C2", "test2@example.com", "User2", "S2", "Desc", Category.AccountAccess, Priority.Urgent, Status.New, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], new Metadata("api", null, null));

        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new[] { ticket1, ticket2 }.ToList().AsReadOnly());

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.GetAllAsync(category: Category.AccountAccess, priority: Priority.Urgent);

        // Assert
        result.Should().HaveCount(1);
        result.First().Priority.Should().Be(Priority.Urgent);
        result.First().Category.Should().Be(Category.AccountAccess);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsTicket()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var ticket = new Ticket(ticketId, "C1", "test@example.com", "User", "Subject", "Desc", Category.Other, Priority.Medium, Status.New, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], new Metadata("api", null, null));

        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(ticketId))
            .ReturnsAsync(ticket);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.GetByIdAsync(ticketId);

        // Assert
        result.Should().Be(ticket);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonexistentId_ReturnsNull()
    {
        // Arrange
        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Ticket?)null);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithValidUpdate_UpdatesField()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var original = new Ticket(ticketId, "C1", "test@example.com", "User", "Original", "Description", Category.Other, Priority.Medium, Status.New, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], new Metadata("api", null, null));

        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(ticketId))
            .ReturnsAsync(original);
        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
            .ReturnsAsync((Ticket t) => t);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.UpdateAsync(ticketId, subject: "Updated");

        // Assert
        result?.Subject.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateAsync_WithNullFields_PreservesOriginalValues()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var original = new Ticket(ticketId, "C1", "test@example.com", "User", "Original", "Description", Category.Other, Priority.Medium, Status.New, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null, [], new Metadata("api", null, null));

        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(ticketId))
            .ReturnsAsync(original);
        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
            .ReturnsAsync((Ticket t) => t);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.UpdateAsync(ticketId, subject: null);

        // Assert
        result?.Subject.Should().Be("Original");
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentId_ReturnsNull()
    {
        // Arrange
        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Ticket?)null);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.UpdateAsync(Guid.NewGuid(), subject: "Updated");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTimestamp()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var original = new Ticket(ticketId, "C1", "test@example.com", "User", "Original", "Description", Category.Other, Priority.Medium, Status.New, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(-1), null, null, [], new Metadata("api", null, null));

        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(ticketId))
            .ReturnsAsync(original);
        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Ticket>()))
            .ReturnsAsync((Ticket t) => t);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.UpdateAsync(ticketId, subject: "Updated");

        // Assert
        result?.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_ReturnsTrue()
    {
        // Arrange
        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.DeleteAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithNonexistentId_ReturnsFalse()
    {
        // Arrange
        var mockRepo = new Mock<ITicketRepository>();
        mockRepo.Setup(r => r.DeleteAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var service = new TicketService(mockRepo.Object);

        // Act
        var result = await service.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }
}
