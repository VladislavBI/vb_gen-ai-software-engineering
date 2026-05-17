#pragma warning disable IDE0005, IDE0008
using FluentAssertions;
using FluentValidation;
using Homework2.Api.Models;
using Homework2.Api.Validators;
#pragma warning restore IDE0005, IDE0008

namespace Homework2.Tests.Api.Validators;

/// <summary>Unit tests for ticket validators.</summary>
public sealed class TicketValidatorsTests
{
    [Fact]
    public async Task CreateTicketValidator_WithValidRequest_Passes()
    {
        // Arrange
        var validator = new CreateTicketValidator();
        var request = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "test@example.com",
            CustomerName: "Test User",
            Subject: "Subject",
            Description: "This is a valid description"
        );

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTicketValidator_WithEmptyCustomerId_Fails()
    {
        // Arrange
        var validator = new CreateTicketValidator();
        var request = new CreateTicketRequest(
            CustomerId: "",
            CustomerEmail: "test@example.com",
            CustomerName: "User",
            Subject: "Subject",
            Description: "Description here"
        );

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public async Task CreateTicketValidator_WithInvalidEmail_Fails()
    {
        // Arrange
        var validator = new CreateTicketValidator();
        var request = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "not-an-email",
            CustomerName: "User",
            Subject: "Subject",
            Description: "Description here"
        );

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerEmail");
    }

    [Fact]
    public async Task CreateTicketValidator_WithEmptyCustomerName_Fails()
    {
        // Arrange
        var validator = new CreateTicketValidator();
        var request = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "test@example.com",
            CustomerName: "",
            Subject: "Subject",
            Description: "Description here"
        );

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerName");
    }

    [Fact]
    public async Task CreateTicketValidator_WithEmptySubject_Fails()
    {
        // Arrange
        var validator = new CreateTicketValidator();
        var request = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "test@example.com",
            CustomerName: "User",
            Subject: "",
            Description: "Description here"
        );

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subject");
    }

    [Fact]
    public async Task CreateTicketValidator_WithLongSubject_Fails()
    {
        // Arrange
        var validator = new CreateTicketValidator();
        var longSubject = new string('a', 201);
        var request = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "test@example.com",
            CustomerName: "User",
            Subject: longSubject,
            Description: "Description here"
        );

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subject");
    }

    [Fact]
    public async Task CreateTicketValidator_WithShortDescription_Fails()
    {
        // Arrange
        var validator = new CreateTicketValidator();
        var request = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "test@example.com",
            CustomerName: "User",
            Subject: "Subject",
            Description: "short"
        );

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task CreateTicketValidator_WithLongDescription_Fails()
    {
        // Arrange
        var validator = new CreateTicketValidator();
        var longDescription = new string('a', 2001);
        var request = new CreateTicketRequest(
            CustomerId: "C1",
            CustomerEmail: "test@example.com",
            CustomerName: "User",
            Subject: "Subject",
            Description: longDescription
        );

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task UpdateTicketValidator_WithValidRequest_Passes()
    {
        // Arrange
        var validator = new UpdateTicketValidator();
        var request = new UpdateTicketRequest(
            Subject: "Updated subject",
            Description: "Updated description here"
        );

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTicketValidator_WithNullSubject_Passes()
    {
        // Arrange
        var validator = new UpdateTicketValidator();
        var request = new UpdateTicketRequest(Subject: null);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTicketValidator_WithInvalidSubjectLength_Fails()
    {
        // Arrange
        var validator = new UpdateTicketValidator();
        var longSubject = new string('a', 201);
        var request = new UpdateTicketRequest(Subject: longSubject);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subject");
    }

    [Fact]
    public async Task UpdateTicketValidator_WithInvalidDescriptionLength_Fails()
    {
        // Arrange
        var validator = new UpdateTicketValidator();
        var request = new UpdateTicketRequest(Description: "short");

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public async Task UpdateTicketValidator_WithNullDescription_Passes()
    {
        // Arrange
        var validator = new UpdateTicketValidator();
        var request = new UpdateTicketRequest(Description: null);

        // Act
        var result = await validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
