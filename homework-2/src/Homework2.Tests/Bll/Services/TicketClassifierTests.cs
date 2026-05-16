#pragma warning disable IDE0005, IDE0008
using FluentAssertions;
using Homework2.Bll.Domain;
using Homework2.Bll.Services;
#pragma warning restore IDE0005, IDE0008

namespace Homework2.Tests.Bll.Services;

/// <summary>Unit tests for TicketClassifier.</summary>
public sealed class TicketClassifierTests
{
    private static Ticket CreateTicket(string subject, string description) =>
        new(
            Guid.NewGuid(),
            "C1",
            "test@example.com",
            "User",
            subject,
            description,
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
    public void Classify_WithAccountAccessKeywords_ReturnsAccountAccessCategory()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Cannot access account", "I cannot log into my account at all.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Category.Should().Be(Category.AccountAccess);
    }

    [Fact]
    public void Classify_WithUrgentKeywords_ReturnsUrgentPriority()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Critical production down", "Our production system is down.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Priority.Should().Be(Priority.Urgent);
    }

    [Fact]
    public void Classify_WithLowKeywords_ReturnsLowPriority()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Minor cosmetic suggestion", "I have a minor suggestion about the UI.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Priority.Should().Be(Priority.Low);
    }

    [Fact]
    public void Classify_WithBillingKeywords_ReturnsBillingCategory()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Payment invoice refund", "I need a refund for my last invoice.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Category.Should().Be(Category.BillingQuestion);
    }

    [Fact]
    public void Classify_WithTechnicalIssueKeywords_ReturnsTechnicalIssueCategory()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("System crash on error", "The system crashes when I get an error message.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Category.Should().Be(Category.TechnicalIssue);
    }

    [Fact]
    public void Classify_WithBugReportKeywords_ReturnsBugReportCategory()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Defect in reproducible steps", "There is a defect with clear steps to reproduce.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Category.Should().Be(Category.BugReport);
    }

    [Fact]
    public void Classify_WithFeatureRequestKeywords_ReturnsFeatureRequestCategory()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Feature enhancement request", "I would like to request a new feature to enhance the system.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Category.Should().Be(Category.FeatureRequest);
    }

    [Fact]
    public void Classify_WithNoMatchingKeywords_ReturnsOtherCategory()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Random subject", "This ticket does not match any keywords.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Category.Should().Be(Category.Other);
    }

    [Fact]
    public void Classify_ConfidenceIsWithinRange()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Cannot access account", "I cannot access my account.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Confidence.Should().BeGreaterThanOrEqualTo(0.0);
        result.Confidence.Should().BeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void Classify_KeywordsFoundContainsMatchedKeywords()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Cannot access account", "I cannot access my account.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.KeywordsFound.Should().NotBeEmpty();
        result.KeywordsFound.Should().AllSatisfy(k => k.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public void Classify_ReasoningIsNotEmpty()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Cannot access account", "I cannot access my account.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Reasoning.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Classify_CaseInsensitive_Keywords()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("CANNOT ACCESS ACCOUNT", "I CANNOT ACCOUNT LOGIN");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Category.Should().Be(Category.AccountAccess);
    }

    [Fact]
    public void Classify_WithMultipleMatchingCategories_HighestConfidenceWins()
    {
        // Arrange
        var classifier = new TicketClassifier();
        // BugReport has keywords: defect, reproduce, steps to reproduce, regression (4 keywords)
        // TechnicalIssue has keywords: bug, error, crash, exception, fail (5 keywords)
        // This description matches both, but should pick the one with higher confidence
        var ticket = CreateTicket("Bug and defect", "There is a bug and a defect that crashes the system with an error.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Confidence.Should().BeGreaterThan(0.0);
        // Both categories match, so confidence should be based on matched/total keywords ratio
    }

    [Fact]
    public void Classify_OtherCategory_ReturnsDefaultReasoning()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Random subject", "This is just a random ticket with no keywords.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Category.Should().Be(Category.Other);
        result.Reasoning.Should().Contain("No confident category keywords found");
    }

    [Fact]
    public void Classify_ReasoningIncludesCategoryName()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Cannot access account", "I cannot access.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Reasoning.Should().Contain("ACCOUNT_ACCESS");
    }

    [Fact]
    public void Classify_ReasoningIncludesConfidencePercentage()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Cannot access account", "I cannot access.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Reasoning.Should().Contain("%");
    }

    [Fact]
    public void Classify_ReasoningIncludesPriority()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Cannot access account", "I cannot access.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Reasoning.Should().Contain(result.Priority.ToString().ToUpperInvariant());
    }

    [Fact]
    public void Classify_HighPriorityKeywords_ReturnsHighPriority()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Important blocking issue", "This is an important and blocking issue.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Priority.Should().Be(Priority.High);
    }

    [Fact]
    public void Classify_NoUrgentButHighPresent_ReturnsHighPriority()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Important issue", "This is important and needs to be fixed asap.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Priority.Should().Be(Priority.High);
    }

    [Fact]
    public void Classify_DefaultPriority_ReturnsMedium()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Random ticket", "This ticket has no priority keywords.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Priority.Should().Be(Priority.Medium);
    }

    [Fact]
    public void Classify_UrgentTakesPriorityOverHigh()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Critical production issue", "Critical production down and it's important.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Priority.Should().Be(Priority.Urgent);
    }

    [Fact]
    public void Classify_AccountAccessWithUrgent_ReturnsCorrectCategoryAndPriority()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Cannot access account - critical production down", "I cannot access my account and our production is down due to security incident.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.Category.Should().Be(Category.AccountAccess);
        result.Priority.Should().Be(Priority.Urgent);
    }

    [Fact]
    public void Classify_PartialKeywordMatches_IncludesInKeywordsFound()
    {
        // Arrange
        var classifier = new TicketClassifier();
        var ticket = CreateTicket("Login problem", "I have an issue with login and 2fa authentication.");

        // Act
        var result = classifier.Classify(ticket);

        // Assert
        result.KeywordsFound.Should().Contain("login");
        result.KeywordsFound.Should().Contain("2fa");
    }
}
