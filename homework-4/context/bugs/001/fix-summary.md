# Fix Summary — Bug 001

**Fixer:** Bug Fixer (Stage 4)
**Date:** 2026-06-17
**Overall Status:** COMPLETE — all changes applied, all 7 tests green after each fix.

---

## Changes Made

### Fix 1 — Off-by-one Discount Boundary

**File:** `src/SampleApp/Pricing/OrderCalculator.cs`
**Method:** `GetDiscountRate(int quantity)` — line 12

Before:
```csharp
return quantity > 10 ? 0.10m : 0m;
```

After:
```csharp
return quantity >= 10 ? 0.10m : 0m;
```

**Source match:** Confirmed. The `>` operator was present exactly as the plan described.
**Test result after this change:** 7/7 passed. No regressions.

---

### Fix 2 — Tax Applied to Post-Discount Subtotal

**File:** `src/SampleApp/Pricing/OrderCalculator.cs`
**Method:** `CalculateTotal(OrderItem item)` — lines 23–24

Before:
```csharp
decimal tax = subtotal * TaxRate;           // BUG: should be (subtotal * (1-discount)) * TaxRate
return subtotal - (subtotal * discount) + tax;
```

After:
```csharp
decimal discountedSubtotal = subtotal * (1m - discount);
return discountedSubtotal * (1m + TaxRate);
```

**Source match:** Confirmed. The buggy lines and comment were present exactly as the plan described.
**Test result after this change:** 7/7 passed. The `CalculateTotal_NoDiscount_AppliesTaxCorrectly` (qty=1, no discount path) continued to return 108m as expected.

---

### Fix 3 — Hardcoded Admin Token → Environment Variable

**File:** `src/SampleApp/Auth/TokenAuthenticator.cs` — full rewrite

Before:
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

After:
```csharp
using System.Security.Cryptography;

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

**Source match:** Confirmed. The hardcoded const and `==` comparison were present exactly as the plan described.

**File:** `src/tests/SampleApp.Tests/UnitTest1.cs`
**Test updated:** `IsAdmin_CorrectToken_ReturnsTrue`

Before:
```csharp
// This test documents the seeded security issue: the token is hardcoded.
// The security-verifier agent will flag this; the bug-fixer will move the
// token to config. After the fix this test will be updated to use config.
bool result = TokenAuthenticator.IsAdmin("super-secret-admin-token-123");
result.Should().BeTrue();
```

After:
```csharp
// Token is now read from the ADMIN_TOKEN environment variable at runtime.
Environment.SetEnvironmentVariable("ADMIN_TOKEN", "super-secret-admin-token-123");
bool result = TokenAuthenticator.IsAdmin("super-secret-admin-token-123");
result.Should().BeTrue();
```

**Test result after this change:** 7/7 passed. `IsAdmin_CorrectToken_ReturnsTrue` passed (env var set inline by the test). `IsAdmin_WrongToken_ReturnsFalse` passed (wrong token still returns false).

---

## Overall Status

**COMPLETE**

| Fix | File(s) | Tests after |
|-----|---------|-------------|
| 1 — Off-by-one `>=` | `OrderCalculator.cs:12` | 7/7 green |
| 2 — Tax on discounted subtotal | `OrderCalculator.cs:23–24` | 7/7 green |
| 3 — Token from env var + constant-time compare | `TokenAuthenticator.cs` (full rewrite) + `UnitTest1.cs` | 7/7 green |

Final suite: **7 passed, 0 failed, 0 skipped** in ~0.84 s.

---

## Manual Verification

Run the following commands in PowerShell to reproduce the full verification:

```powershell
# Navigate to the repo root
Set-Location "D:\Work\learn\Courses\AI -set\lecture-1\vb_gen-ai-software-engineering\homework-4"

# Run all tests (ADMIN_TOKEN is set inside the test itself, no shell export needed)
dotnet test src\SampleApp.slnx --verbosity normal
```

Expected output (abridged):
```
Всего тестов: 7
     Пройдено: 7
```

To manually verify Fix 1 (boundary):
```powershell
# Confirm qty=10 now returns 0.10m — add a breakpoint or a temporary test
# The unit-test-generator agent will add the boundary test automatically.
```

To manually verify Fix 2 (tax order), expected total for 20 units at $100 each:
```
subtotal = 2000
discountedSubtotal = 2000 * (1 - 0.10) = 1800
total = 1800 * (1 + 0.08) = 1944.00
```

To manually verify Fix 3 (env var):
```powershell
# Set env var in shell and run again
$env:ADMIN_TOKEN = "super-secret-admin-token-123"
dotnet test src\SampleApp.slnx --verbosity normal
```

---

## References

- Plan: `context/bugs/001/implementation-plan.md`
- Files modified:
  - `D:\Work\learn\Courses\AI -set\lecture-1\vb_gen-ai-software-engineering\homework-4\src\SampleApp\Pricing\OrderCalculator.cs`
  - `D:\Work\learn\Courses\AI -set\lecture-1\vb_gen-ai-software-engineering\homework-4\src\SampleApp\Auth\TokenAuthenticator.cs`
  - `D:\Work\learn\Courses\AI -set\lecture-1\vb_gen-ai-software-engineering\homework-4\src\tests\SampleApp.Tests\UnitTest1.cs`
