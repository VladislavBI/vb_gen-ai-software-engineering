# Test Report — Bug 001

**Agent:** Unit Test Generator (Stage 5)
**Date:** 2026-06-17
**Bug:** 001 — Off-by-one discount boundary, tax on pre-discount subtotal, hardcoded admin token

---

## 1. Scope

Three fixes from `context/bugs/001/fix-summary.md` are under test. Unchanged code (e.g. `AverageUnitPrice`) is not re-tested here — the existing suite already covers it.

| Fix | Changed unit | Behavior under test |
|-----|-------------|---------------------|
| 1 | `OrderCalculator.GetDiscountRate` | `>` → `>=` at qty=10 |
| 2 | `OrderCalculator.CalculateTotal` | Tax applied to discounted subtotal, not pre-discount |
| 3 | `TokenAuthenticator.IsAdmin` | Full rewrite: null/empty guard, env-var guard, constant-time compare via `FixedTimeEquals` |

---

## 2. Generated Tests

**File:** `src/tests/SampleApp.Tests/Bug001RegressionTests.cs`
**Class:** `Bug001RegressionTests` (10 tests)

### Fix 1 — GetDiscountRate boundary

| Test name | What it covers |
|-----------|---------------|
| `GetDiscountRate_ExactlyAtThreshold_ReturnsTenPercent` | **Regression test**: qty=10 must return 0.10m (the fix from `> 10` to `>= 10`) |
| `GetDiscountRate_OneBelowThreshold_ReturnsZero` | Lower boundary neighbor: qty=9 must return 0m |
| `GetDiscountRate_ZeroQuantity_ReturnsZero` | Edge/invalid input: qty=0 |

### Fix 2 — CalculateTotal tax formula

| Test name | What it covers |
|-----------|---------------|
| `CalculateTotal_WithDiscount_AppliesTaxToDiscountedSubtotal` | **Regression test**: qty=20, price=100 → 1944.00 (old bug returned 1960.00) |
| `CalculateTotal_ExactlyAtDiscountBoundary_AppliesBothDiscountAndTaxCorrectly` | Combo test: qty=10 activates both Fix 1 (discount kicks in) and Fix 2 (tax on 450, not 500) |

### Fix 3 — TokenAuthenticator env-var + constant-time compare

| Test name | What it covers |
|-----------|---------------|
| `IsAdmin_NullToken_ReturnsFalse` | Null input guard (early exit before env-var lookup) |
| `IsAdmin_EmptyToken_ReturnsFalse` | Empty-string input guard |
| `IsAdmin_MissingEnvVar_ReturnsFalse` | Missing `ADMIN_TOKEN` env var → false, even for non-empty token |
| `IsAdmin_CorrectTokenWithEnvVarSet_ReturnsTrue` | **Happy path**: env var set to token value → true (exercises `FixedTimeEquals` path) |
| `IsAdmin_WrongTokenWithEnvVarSet_ReturnsFalse` | Negative path with env var present: wrong token → false (exercises `FixedTimeEquals` rejection, not the env-var guard) |

---

## 3. FIRST Compliance

### OrderCalculator tests (5 tests)

| Principle | Status | Notes |
|-----------|--------|-------|
| **F** Fast | PASS | Pure arithmetic, no I/O. All run in < 1 ms. |
| **I** Independent | PASS | Each test creates its own `OrderItem`; no shared state. |
| **R** Repeatable | PASS | No randomness, clock, locale, or environment dependency. Results are deterministic on any machine. |
| **S** Self-validating | PASS | Each test asserts an exact `decimal` value via FluentAssertions `.Should().Be(...)`. Failure message identifies the wrong value. |
| **T** Timely | PASS | All five tests target the exact behaviors changed by Fix 1 and Fix 2. |

### TokenAuthenticator tests (5 tests)

| Principle | Status | Notes |
|-----------|--------|-------|
| **F** Fast | PASS | No network, disk, DB, or sleep. All run in < 15 ms (fastest < 1 ms). |
| **I** Independent | PASS | Each test saves, sets, and restores `ADMIN_TOKEN` in a `finally` block, so no test leaks env state to another. |
| **R** Repeatable | PARTIAL — see gap below | `IsAdmin_NullToken_ReturnsFalse` and `IsAdmin_EmptyToken_ReturnsFalse` do not touch `ADMIN_TOKEN` at all; they return false before reaching the env-var check, so they are fully deterministic. The three env-var tests use save/restore to control the ambient environment, achieving determinism in practice. Ideally there would be an injection seam (see Gaps). |
| **S** Self-validating | PASS | All assertions use `.Should().BeTrue()` / `.Should().BeFalse()` with no manual inspection needed. |
| **T** Timely | PASS | All five tests target the new behaviors introduced by Fix 3. |

---

## 4. Run Result

**Command:**
```
dotnet test "D:\Work\learn\Courses\AI -set\lecture-1\vb_gen-ai-software-engineering\homework-4\src\SampleApp.slnx" --verbosity normal
```

**Output (abridged — relevant lines):**
```
Passed  Bug001RegressionTests.GetDiscountRate_ExactlyAtThreshold_ReturnsTenPercent    [< 1 ms]
Passed  Bug001RegressionTests.GetDiscountRate_OneBelowThreshold_ReturnsZero           [< 1 ms]
Passed  Bug001RegressionTests.GetDiscountRate_ZeroQuantity_ReturnsZero                [< 1 ms]
Passed  Bug001RegressionTests.CalculateTotal_WithDiscount_AppliesTaxToDiscountedSubtotal [< 1 ms]
Passed  Bug001RegressionTests.CalculateTotal_ExactlyAtDiscountBoundary_AppliesBothDiscountAndTaxCorrectly [1 ms]
Passed  Bug001RegressionTests.IsAdmin_NullToken_ReturnsFalse                          [< 1 ms]
Passed  Bug001RegressionTests.IsAdmin_EmptyToken_ReturnsFalse                         [15 ms]
Passed  Bug001RegressionTests.IsAdmin_MissingEnvVar_ReturnsFalse                      [< 1 ms]
Passed  Bug001RegressionTests.IsAdmin_CorrectTokenWithEnvVarSet_ReturnsTrue           [< 1 ms]
Passed  Bug001RegressionTests.IsAdmin_WrongTokenWithEnvVarSet_ReturnsFalse            [< 1 ms]

--- pre-existing suite also passed ---
Passed  OrderCalculatorTests.GetDiscountRate_BelowThreshold_ReturnsZero
Passed  OrderCalculatorTests.GetDiscountRate_WellAboveThreshold_ReturnsTenPercent
Passed  OrderCalculatorTests.CalculateTotal_NoDiscount_AppliesTaxCorrectly
Passed  OrderCalculatorTests.AverageUnitPrice_EmptyList_ReturnsZero
Passed  OrderCalculatorTests.AverageUnitPrice_MultipleItems_ReturnsCorrectAverage
Passed  OrderCalculatorTests.IsAdmin_WrongToken_ReturnsFalse
Passed  OrderCalculatorTests.IsAdmin_CorrectToken_ReturnsTrue

Total tests: 17   Passed: 17   Failed: 0   Skipped: 0
Total time:  0.9010 s
Build: succeeded, 0 warnings, 0 errors
```

**Result: ALL 17 PASSED — 0 FAILURES.**

---

## 5. Coverage Map / Gaps

### Coverage map

| Changed behavior | Covered by |
|-----------------|-----------|
| `GetDiscountRate` returns 0.10m for qty=10 (Fix 1 regression) | `GetDiscountRate_ExactlyAtThreshold_ReturnsTenPercent` |
| `GetDiscountRate` returns 0m for qty=9 (lower boundary) | `GetDiscountRate_OneBelowThreshold_ReturnsZero` |
| `GetDiscountRate` returns 0m for qty=0 (edge input) | `GetDiscountRate_ZeroQuantity_ReturnsZero` |
| `CalculateTotal` tax on discounted subtotal (Fix 2 regression, qty=20) | `CalculateTotal_WithDiscount_AppliesTaxToDiscountedSubtotal` |
| Both fixes together at the exact discount boundary (qty=10) | `CalculateTotal_ExactlyAtDiscountBoundary_AppliesBothDiscountAndTaxCorrectly` |
| `IsAdmin` rejects null token | `IsAdmin_NullToken_ReturnsFalse` |
| `IsAdmin` rejects empty string | `IsAdmin_EmptyToken_ReturnsFalse` |
| `IsAdmin` returns false when `ADMIN_TOKEN` absent | `IsAdmin_MissingEnvVar_ReturnsFalse` |
| `IsAdmin` returns true when token matches env var (FixedTimeEquals path) | `IsAdmin_CorrectTokenWithEnvVarSet_ReturnsTrue` |
| `IsAdmin` returns false when token is wrong with env var present (FixedTimeEquals path) | `IsAdmin_WrongTokenWithEnvVarSet_ReturnsFalse` |

### Gaps and notes

**Gap 1 — Testability: no injection seam for `Environment.GetEnvironmentVariable`**

`TokenAuthenticator.IsAdmin` calls `Environment.GetEnvironmentVariable` directly. There is no constructor or parameter to inject an alternative. The save/restore pattern used here achieves determinism in practice but is not a pure FIRST solution because:
- It mutates process-global state (though it restores it).
- In a parallel test runner, two tests mutating the same env var simultaneously could race.

Minimal refactor that would fully satisfy FIRST (not applied — production code not modified):
```csharp
public static bool IsAdmin(string token, Func<string, string?>? envReader = null)
{
    envReader ??= Environment.GetEnvironmentVariable;
    ...
    string? adminToken = envReader("ADMIN_TOKEN");
    ...
}
```
This would let tests pass a pure in-memory lambda with no process-state side effects.

**Gap 2 — Constant-time comparison cannot be directly timed**

The fix replaced `string ==` with `CryptographicOperations.FixedTimeEquals`. It is not possible to prove constant-time behavior from a unit test (timing measurements are non-deterministic and machine-dependent). The `IsAdmin_WrongTokenWithEnvVarSet_ReturnsFalse` test confirms the correct code path is invoked (wrong token rejected) but cannot assert timing properties. Timing behavior is inherently an integration/property test concern.

**Gap 3 — Independence issue in pre-existing test `IsAdmin_CorrectToken_ReturnsTrue` (UnitTest1.cs)**

The existing test in `OrderCalculatorTests` sets `ADMIN_TOKEN` but does not restore it in a `finally` block. If xUnit runs tests in an order where this test precedes `IsAdmin_WrongToken_ReturnsFalse` (also in `OrderCalculatorTests`), the latter passes for the right reason (wrong token rejected). But if the env var were not set, `IsAdmin_WrongToken_ReturnsFalse` would still pass because the missing-env-var guard fires first — a false positive. This is a pre-existing Independence issue in `UnitTest1.cs`. Production code was not modified; flagged here as an observation only.

---

## 6. Summary

| Category | Count |
|----------|-------|
| New regression/boundary tests added | 10 |
| Pre-existing tests (unchanged, still passing) | 7 |
| **Total tests in suite** | **17** |
| Failures | 0 |
| FIRST deviations | 1 partial (R — env-var save/restore instead of injection) |
| Testability gaps noted | 2 (env injection seam, constant-time assertion) |
