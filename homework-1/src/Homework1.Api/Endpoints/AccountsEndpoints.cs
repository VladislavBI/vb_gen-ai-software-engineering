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
    }

    private static async Task<IResult> GetAccountBalance(string accountId, TransactionService service)
    {
        decimal balance = await service.GetAccountBalanceAsync(accountId);

        var response = new AccountBalanceResponse(accountId, balance);

        return Results.Ok(response);
    }

    internal sealed record AccountBalanceResponse(
        string AccountId,
        decimal Balance);
}
