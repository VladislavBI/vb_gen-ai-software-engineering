namespace Homework2.Bll.Domain;

/// <summary>Ticket category enumeration.</summary>
public enum Category
{
    /// <summary>Account access issue.</summary>
    AccountAccess,
    /// <summary>Technical issue.</summary>
    TechnicalIssue,
    /// <summary>Billing question.</summary>
    BillingQuestion,
    /// <summary>Feature request.</summary>
    FeatureRequest,
    /// <summary>Bug report.</summary>
    BugReport,
    /// <summary>Other.</summary>
    Other
}

/// <summary>Ticket priority enumeration.</summary>
public enum Priority
{
    /// <summary>Low priority.</summary>
    Low,
    /// <summary>Medium priority.</summary>
    Medium,
    /// <summary>High priority.</summary>
    High,
    /// <summary>Urgent priority.</summary>
    Urgent
}

/// <summary>Ticket status enumeration.</summary>
public enum Status
{
    /// <summary>New ticket.</summary>
    New,
    /// <summary>In progress.</summary>
    InProgress,
    /// <summary>Waiting for customer.</summary>
    WaitingCustomer,
    /// <summary>Resolved.</summary>
    Resolved,
    /// <summary>Closed.</summary>
    Closed
}

/// <summary>Metadata associated with a ticket.</summary>
public record Metadata(string Source, string? Browser, string? DeviceType);

/// <summary>Support ticket domain model.</summary>
public record Ticket(
    Guid Id,
    string CustomerId,
    string CustomerEmail,
    string CustomerName,
    string Subject,
    string Description,
    Category Category,
    Priority Priority,
    Status Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ResolvedAt,
    string? AssignedTo,
    IReadOnlyList<string> Tags,
    Metadata Metadata
);
