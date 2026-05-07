namespace Homework1.Bll.Domain;

public record Transaction(
    string FromAccount,
    string ToAccount,
    decimal Amount,
    string Currency,
    string Type);

public record StoredTransaction(
    Guid Id,
    string FromAccount,
    string ToAccount,
    decimal Amount,
    string Currency,
    string Type,
    DateTimeOffset CreatedAt);

