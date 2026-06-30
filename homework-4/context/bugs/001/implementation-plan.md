# Implementation Plan — Bug 001

**Planner:** Inline (Stage 3)
**Date:** 2026-06-17
**Based on:** `context/bugs/001/research/verified-research.md` (Quality: B — Good, Verdict: PASS)

---

## Test Command

Run after each fix to verify nothing regresses:

```powershell
dotnet test "D:\Work\learn\Courses\AI -set\lecture-1\vb_gen-ai-software-engineering\homework-4\src\SampleApp.slnx" --verbosity normal
```

---

## Fix 1 — Off-by-one Discount Boundary

**File:** `src/SampleApp/Pricing/OrderCalculator.cs`
**Method:** `GetDiscountRate(int quantity)`
**Line:** 12

**Before:**
```csharp
return quantity > 10 ? 0.10m : 0m;
```

**After:**
```csharp
return quantity >= 10 ? 0.10m : 0m;
```

**Rationale:** The spec requires 10% discount for 10 **or more** units. Changing `>` to `>=` makes `quantity == 10` evaluate `true` and return `0.10m`.

**Verification:** Run `dotnet test`. The existing baseline tests (`GetDiscountRate_BelowThreshold_ReturnsZero` at qty=5 and `GetDiscountRate_WellAboveThreshold_ReturnsTenPercent` at qty=20) must still pass. The unit-test-generator agent will add the boundary test for qty=10.

---

## Fix 2 — Tax Applied to Post-Discount Subtotal

**File:** `src/SampleApp/Pricing/OrderCalculator.cs`
**Method:** `CalculateTotal(OrderItem item)`
**Line:** 23–24

**Before:**
```csharp
decimal subtotal = item.UnitPrice * item.Quantity;
decimal discount = GetDiscountRate(item.Quantity);
decimal tax = subtotal * TaxRate;           // BUG: should be (subtotal * (1-discount)) * TaxRate
return subtotal - (subtotal * discount) + tax;
```

**After:**
```csharp
decimal subtotal = item.UnitPrice * item.Quantity;
decimal discount = GetDiscountRate(item.Quantity);
decimal discountedSubtotal = subtotal * (1m - discount);
return discountedSubtotal * (1m + TaxRate);
```

**Rationale:** The correct formula is `total = subtotal × (1 − discount) × (1 + tax)`. Introducing `discountedSubtotal` as an intermediate variable makes the intent explicit and eliminates the pre-discount tax bug. The `// BUG:` annotation comment is also removed since it no longer applies.

**Verification:** Run `dotnet test`. All existing passing tests must continue to pass (the `CalculateTotal_NoDiscount_AppliesTaxCorrectly` test uses qty=1, no discount — `1 × (1−0) × 1.08 = 108m`, unchanged). The unit-test-generator agent will add the regression test for the qty=20 case (expected 1944.00).

---

## Fix 3 — Hardcoded Admin Token → Environment Variable

**File:** `src/SampleApp/Auth/TokenAuthenticator.cs`
**Lines:** 9, 11–13

**Before:**
```csharp
namespace SampleApp.Auth;

public static class TokenAuthenticator
{
    // SEEDED SECURITY ISSUE ...
    private const string AdminToken = "super-secret-admin-token-123";

    public static bool IsAdmin(string token)
    {
        return token == AdminToken;
    }
}
```

**After:**
```csharp
namespace SampleApp.Auth;

public static class TokenAuthenticator
{
    public static bool IsAdmin(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        string? adminToken = Environment.GetEnvironmentVariable("ADMIN_TOKEN");
        if (string.IsNullOrEmpty(adminToken))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(token),
            System.Text.Encoding.UTF8.GetBytes(adminToken));
    }
}
```

**Required using directive** — add at top of file:
```csharp
using System.Security.Cryptography;
```

**Rationale:** This single fix addresses all three security findings together:
- **F3 (hardcoded token):** Token is now read from `ADMIN_TOKEN` environment variable at runtime — never stored in source.
- **F4 (timing attack):** `CryptographicOperations.FixedTimeEquals` compares byte-span representations in constant time, closing the timing side-channel.
- **F5 (no null guard):** `string.IsNullOrEmpty(token)` at the top returns `false` immediately for null or empty input, establishing a clear defensive contract.

**Verification:** Run `dotnet test`. The `IsAdmin_WrongToken_ReturnsFalse` test will still pass (wrong token → false). The `IsAdmin_CorrectToken_ReturnsTrue` test currently passes `"super-secret-admin-token-123"` directly — after this fix, it will fail unless the `ADMIN_TOKEN` env var is set to that value. The unit-test-generator agent will update this test to set the env var before asserting.

> **Note for Bug Fixer:** When running tests after Fix 3, set the environment variable:
> ```powershell
> $env:ADMIN_TOKEN = "super-secret-admin-token-123"
> dotnet test "D:\Work\learn\Courses\AI -set\lecture-1\vb_gen-ai-software-engineering\homework-4\src\SampleApp.slnx" --verbosity normal
> ```
> The existing `IsAdmin_CorrectToken_ReturnsTrue` test will need updating to set the env var, or the test run should be preceded by the env var export.

---

## Application Order

Apply fixes in this sequence, running `dotnet test` after each:

1. **Fix 1** — `GetDiscountRate` boundary (`OrderCalculator.cs:12`) — isolated, no test breakage expected.
2. **Fix 2** — `CalculateTotal` tax order (`OrderCalculator.cs:23–24`) — isolated, no test breakage expected.
3. **Fix 3** — `TokenAuthenticator` full rewrite (`TokenAuthenticator.cs`) — the existing `IsAdmin_CorrectToken_ReturnsTrue` test will break; the fixer must also update that test to inject `ADMIN_TOKEN` via env var before the assertion. Update `UnitTest1.cs` accordingly.

---

## Files to Modify

| File | Change |
|---|---|
| `src/SampleApp/Pricing/OrderCalculator.cs` | Lines 12, 23–24 |
| `src/SampleApp/Auth/TokenAuthenticator.cs` | Full rewrite (add using, replace body) |
| `src/tests/SampleApp.Tests/UnitTest1.cs` | Update `IsAdmin_CorrectToken_ReturnsTrue` to use env var |
