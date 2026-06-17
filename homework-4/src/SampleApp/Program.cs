using SampleApp.Auth;
using SampleApp.Pricing;

if (args.Length < 1)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  SampleApp total <quantity> <unitPrice>   -- calculate order total");
    Console.WriteLine("  SampleApp auth  <token>                  -- check admin token");
    return 1;
}

string command = args[0].ToLowerInvariant();

switch (command)
{
    case "total":
    {
        if (args.Length < 3 || !int.TryParse(args[1], out int qty) || !decimal.TryParse(args[2], out decimal price))
        {
            Console.WriteLine("Error: total requires <quantity> (int) and <unitPrice> (decimal).");
            return 1;
        }
        OrderItem item = new("Widget", price, qty);
        decimal discount = OrderCalculator.GetDiscountRate(qty);
        decimal total = OrderCalculator.CalculateTotal(item);
        Console.WriteLine($"Quantity : {qty}");
        Console.WriteLine($"UnitPrice: {price:F2}");
        Console.WriteLine($"Discount : {discount * 100:F0}%");
        Console.WriteLine($"Total    : {total:F2}");
        return 0;
    }

    case "auth":
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Error: auth requires <token>.");
            return 1;
        }
        string token = args[1];
        bool ok = TokenAuthenticator.IsAdmin(token);
        Console.WriteLine(ok ? "Access GRANTED (admin)" : "Access DENIED");
        return ok ? 0 : 1;
    }

    default:
        Console.WriteLine($"Unknown command: {command}");
        return 1;
}
