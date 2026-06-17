namespace SampleApp.Pricing;

public static class OrderCalculator
{
    private const decimal TaxRate = 0.08m;

    // Returns the discount rate for a quantity.
    // Spec: 10 % off for 10 or more units.
    // BUG 1: uses > instead of >= — exactly 10 units receives no discount.
    public static decimal GetDiscountRate(int quantity)
    {
        return quantity >= 10 ? 0.10m : 0m;
    }

    // Calculates the final total for an order item.
    // BUG 2: tax is applied to the pre-discount subtotal instead of the
    //         discounted subtotal — customers are overcharged.
    // Correct formula: total = subtotal * (1 - discount) * (1 + tax)
    public static decimal CalculateTotal(OrderItem item)
    {
        decimal subtotal = item.UnitPrice * item.Quantity;
        decimal discount = GetDiscountRate(item.Quantity);
        decimal discountedSubtotal = subtotal * (1m - discount);
        return discountedSubtotal * (1m + TaxRate);
    }

    // Returns the average unit price across a list of items.
    public static decimal AverageUnitPrice(IReadOnlyList<OrderItem> items)
    {
        if (items.Count == 0)
            return 0m;

        decimal total = 0m;
        foreach (OrderItem item in items)
            total += item.UnitPrice;

        return total / items.Count;
    }
}
