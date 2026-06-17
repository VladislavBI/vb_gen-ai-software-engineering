using FluentAssertions;
using SampleApp.Auth;
using SampleApp.Pricing;

namespace SampleApp.Tests;

// Baseline tests — cover only CORRECT paths that pass on the buggy code.
// Bug-catching tests (boundary qty=10, tax-order regression) are generated
// by the unit-test-generator agent after fixes are applied.
public class OrderCalculatorTests
{
    [Fact]
    public void GetDiscountRate_BelowThreshold_ReturnsZero()
    {
        decimal rate = OrderCalculator.GetDiscountRate(5);
        rate.Should().Be(0m);
    }

    [Fact]
    public void GetDiscountRate_WellAboveThreshold_ReturnsTenPercent()
    {
        decimal rate = OrderCalculator.GetDiscountRate(20);
        rate.Should().Be(0.10m);
    }

    [Fact]
    public void CalculateTotal_NoDiscount_AppliesTaxCorrectly()
    {
        // qty=1: no discount path — pre-discount and post-discount subtotals are the same,
        // so Bug 2 (tax on pre-discount) is invisible here. Result is deterministic.
        OrderItem item = new("Widget", 100m, 1);
        decimal total = OrderCalculator.CalculateTotal(item);
        total.Should().Be(108m); // 100 + 8% tax, no discount
    }

    [Fact]
    public void AverageUnitPrice_EmptyList_ReturnsZero()
    {
        decimal avg = OrderCalculator.AverageUnitPrice([]);
        avg.Should().Be(0m);
    }

    [Fact]
    public void AverageUnitPrice_MultipleItems_ReturnsCorrectAverage()
    {
        IReadOnlyList<OrderItem> items =
        [
            new("A", 100m, 1),
            new("B", 200m, 1),
            new("C", 300m, 1),
        ];
        decimal avg = OrderCalculator.AverageUnitPrice(items);
        avg.Should().Be(200m);
    }

    [Fact]
    public void IsAdmin_WrongToken_ReturnsFalse()
    {
        bool result = TokenAuthenticator.IsAdmin("wrong-token");
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAdmin_CorrectToken_ReturnsTrue()
    {
        // This test documents the seeded security issue: the token is hardcoded.
        // The security-verifier agent will flag this; the bug-fixer will move the
        // token to config. After the fix this test will be updated to use config.
        bool result = TokenAuthenticator.IsAdmin("super-secret-admin-token-123");
        result.Should().BeTrue();
    }
}
