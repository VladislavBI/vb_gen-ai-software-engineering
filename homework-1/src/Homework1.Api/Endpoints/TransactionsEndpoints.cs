using Homework1.Bll.Domain;
using Homework1.Bll.Services;

namespace Homework1.Api.Endpoints;

internal static class TransactionsEndpoints
{
    internal static void MapTransactions(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/transactions")
            .WithName("Transactions");

        group.MapPost("/", CreateTransaction)
            .WithName("CreateTransaction");

        group.MapGet("/", ListTransactions)
            .WithName("ListTransactions");

        group.MapGet("/{id}", GetTransactionById)
            .WithName("GetTransactionById");
    }

    private static async Task<IResult> CreateTransaction(
        CreateTransactionRequest request,
        TransactionService service)
    {
        Transaction transaction = new(
            request.FromAccount,
            request.ToAccount,
            request.Amount,
            request.Currency,
            request.Type);

        StoredTransaction created = await service.CreateAsync(transaction);

        TransactionResponse response = new(
            created.Id,
            created.FromAccount,
            created.ToAccount,
            created.Amount,
            created.Currency,
            created.Type,
            created.CreatedAt);

        return Results.Created($"/transactions/{created.Id}", response);
    }

    private static async Task<IResult> ListTransactions(TransactionService service)
    {
        IReadOnlyList<StoredTransaction> transactions = await service.ListAsync();
        var responses = transactions.Select(t => new TransactionResponse(
            t.Id,
            t.FromAccount,
            t.ToAccount,
            t.Amount,
            t.Currency,
            t.Type,
            t.CreatedAt)).ToList();

        return Results.Ok(responses);
    }

    private static async Task<IResult> GetTransactionById(Guid id, TransactionService service)
    {
        StoredTransaction? transaction = await service.GetByIdAsync(id);
        if (transaction is null)
        {
            return Results.NotFound();
        }

        var response = new TransactionResponse(
            transaction.Id,
            transaction.FromAccount,
            transaction.ToAccount,
            transaction.Amount,
            transaction.Currency,
            transaction.Type,
            transaction.CreatedAt);

        return Results.Ok(response);
    }

    internal sealed record CreateTransactionRequest(
        string FromAccount,
        string ToAccount,
        decimal Amount,
        string Currency,
        string Type);

    internal sealed record TransactionResponse(
        Guid Id,
        string FromAccount,
        string ToAccount,
        decimal Amount,
        string Currency,
        string Type,
        DateTimeOffset CreatedAt);
}
