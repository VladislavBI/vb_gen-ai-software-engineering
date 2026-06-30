# Codebase Research — Bug 001

**Researcher:** Inline (Stage 1)
**Date:** 2026-06-17
**Scope:** `src/SampleApp/` — two subsystems (Pricing, Auth)

---

## Codebase Overview

`SampleApp` is a .NET 10 console CLI with two commands:

- `total <quantity> <unitPrice>` — delegates to `OrderCalculator` (Pricing subsystem)
- `auth <token>` — delegates to `TokenAuthenticator` (Auth subsystem)

Source files surveyed:

| File | Lines | Purpose |
|---|---|---|
| `src/SampleApp/Program.cs` | 49 | Entry point; routes commands |
| `src/SampleApp/Pricing/OrderCalculator.cs` | 39 | Discount + tax calculation |
| `src/SampleApp/Pricing/OrderItem.cs` | 3 | Record type for order items |
| `src/SampleApp/Auth/TokenAuthenticator.cs` | 15 | Admin token verification |
| `src/tests/SampleApp.Tests/UnitTest1.cs` | 72 | Baseline xUnit tests (FluentAssertions) |

---

## Finding 1 — Bug: Off-by-one Discount Boundary

**File:** `src/SampleApp/Pricing/OrderCalculator.cs`
**Method:** `GetDiscountRate(int quantity)`
**Line:** 12

**Source snippet:**
```csharp
public static decimal GetDiscountRate(int quantity)
{
    return quantity > 10 ? 0.10m : 0m;   // line 12
}
```

**Root-cause hypothesis:**
The condition uses `>` (strictly greater than) instead of `>=` (greater than or equal). The spec states the 10% discount applies for **10 or more** units. At `quantity == 10`, `10 > 10` evaluates to `false`, so the discount is `0m` instead of `0.10m`. This is a classic off-by-one boundary error.

**Effect:** Exactly 10 units receives 0% discount instead of the specified 10%.

**Repro:**
```powershell
dotnet run --project src/SampleApp -- total 10 100
# Prints: Discount: 0%  (should be 10%)
```

---

## Finding 2 — Bug: Tax Applied to Pre-Discount Subtotal

**File:** `src/SampleApp/Pricing/OrderCalculator.cs`
**Method:** `CalculateTotal(OrderItem item)`
**Line:** 23

**Source snippet:**
```csharp
public static decimal CalculateTotal(OrderItem item)
{
    decimal subtotal = item.UnitPrice * item.Quantity;          // line 21
    decimal discount = GetDiscountRate(item.Quantity);          // line 22
    decimal tax = subtotal * TaxRate;           // line 23 — BUG: should be (subtotal * (1-discount)) * TaxRate
    return subtotal - (subtotal * discount) + tax;              // line 24
}
```

**Root-cause hypothesis:**
`tax` is computed on the raw `subtotal` (pre-discount) at line 23, then the discount is only subtracted at line 24. The correct formula is:
```
total = subtotal × (1 − discount) × (1 + tax)
```
which means tax must be applied *after* the discount reduction. Expanding the current buggy return:
```
subtotal - (subtotal × discount) + (subtotal × TaxRate)
= subtotal × (1 - discount + TaxRate)
```
vs. the correct:
```
subtotal × (1 - discount) × (1 + TaxRate)
= subtotal × (1 - discount + TaxRate - discount × TaxRate)
```
The bug introduces an extra `subtotal × discount × TaxRate` charge whenever a discount applies.

**Effect:** Customers are overcharged whenever a discount is active. For 20 units at $100:
- Buggy: $2000 × 0.9 + $2000 × 0.08 = $1800 + $160 = **$1960.00**
- Correct: $2000 × 0.9 × 1.08 = **$1944.00**

**Repro:**
```powershell
dotnet run --project src/SampleApp -- total 20 100
# Prints: Total: 1960.00  (should be 1944.00)
```

---

## Finding 3 — Security: Hardcoded Admin Token

**File:** `src/SampleApp/Auth/TokenAuthenticator.cs`
**Line:** 9

**Source snippet:**
```csharp
private const string AdminToken = "super-secret-admin-token-123";   // line 9
```

**Root-cause hypothesis:**
The secret is embedded as a compile-time constant. Anyone with access to the source code, the compiled binary (via decompilation), or the Git history can recover `"super-secret-admin-token-123"`. Secrets must be injected at runtime via environment variables or a secrets manager, never baked into source.

**Severity:** CRITICAL

---

## Finding 4 — Security: Non-Constant-Time String Comparison (Timing Attack)

**File:** `src/SampleApp/Auth/TokenAuthenticator.cs`
**Method:** `IsAdmin(string token)`
**Line:** 13

**Source snippet:**
```csharp
public static bool IsAdmin(string token)
{
    return token == AdminToken;   // line 13
}
```

**Root-cause hypothesis:**
The `==` operator on strings short-circuits at the first mismatched character. This leaks information about how many leading characters match through measurable latency differences — a classic timing side-channel. Token or password comparisons must use a constant-time routine (e.g., `CryptographicOperations.FixedTimeEquals` on byte spans).

**Severity:** HIGH

---

## Finding 5 — Security: No Null/Empty Guard on Token Parameter

**File:** `src/SampleApp/Auth/TokenAuthenticator.cs`
**Method:** `IsAdmin(string token)`
**Line:** 11–13

**Source snippet:**
```csharp
public static bool IsAdmin(string token)
{
    return token == AdminToken;   // line 13 — null token causes NullReferenceException
}
```

**Root-cause hypothesis:**
If the caller passes `null`, the `==` operator on a `null` left operand against a non-null right operand would normally return `false` in C# (null equality is handled), but `string.Equals` semantics via `==` do handle this safely for the equality check itself. However, the method has no documented contract for null and defensive coding should guard against it. More critically: if the token is an empty string, the code silently returns `false` without indicating a contract violation. The primary risk is downstream callers who may not handle `NullReferenceException` from this call site.

**Severity:** MEDIUM

---

## Summary Table

| ID | Type | File | Line | Description | Severity |
|---|---|---|---|---|---|
| F1 | Bug | `Pricing/OrderCalculator.cs` | 12 | `> 10` should be `>= 10` in `GetDiscountRate` | Medium |
| F2 | Bug | `Pricing/OrderCalculator.cs` | 23 | Tax on pre-discount subtotal instead of post-discount | Medium |
| F3 | Security | `Auth/TokenAuthenticator.cs` | 9 | Hardcoded admin secret in source | CRITICAL |
| F4 | Security | `Auth/TokenAuthenticator.cs` | 13 | Non-constant-time `==` comparison (timing attack) | HIGH |
| F5 | Security | `Auth/TokenAuthenticator.cs` | 11–13 | No null/empty guard on `token` parameter | MEDIUM |

---

## Test Coverage Notes

Existing baseline tests in `UnitTest1.cs`:
- `GetDiscountRate` tests use `qty=5` (below threshold) and `qty=20` (above): **does not exercise the `qty=10` boundary** → Bug 1 is not caught by tests.
- `CalculateTotal` test uses `qty=1` (no discount): pre-discount and post-discount subtotals are identical → **Bug 2 is invisible** in this test.
- Auth tests verify the hardcoded token directly, which both confirms the bug and creates a test that will break after the fix.
