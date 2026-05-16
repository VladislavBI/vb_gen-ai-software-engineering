using Homework1.Bll.Services;

namespace Homework1.Api.Endpoints;

internal static class AccountsEndpoints
{
    internal static void MapAccounts(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/accounts")
            .WithName("Accounts");

        group.MapGet("/{accountId}/balance", GetAccountBalance)
            .WithName("GetAccountBalance");

        group.MapGet("/{accountId}/summary", GetAccountSummary)
            .WithName("GetAccountSummary");
    }

    private static async Task<IResult> GetAccountBalance(string accountId, TransactionService service)
    {
        decimal balance = await service.GetAccountBalanceAsync(accountId);

        var response = new AccountBalanceResponse(accountId, balance);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetAccountSummary(string accountId, TransactionService service)
    {
        AccountSummary summary = await service.GetAccountSummaryAsync(accountId);

        return Results.Ok(summary);
    }

    internal sealed record AccountBalanceResponse(
        string AccountId,
        decimal Balance);
}
