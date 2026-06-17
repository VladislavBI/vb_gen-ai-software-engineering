# Security Report — Bug 001

**Agent:** Security Verifier (Stage 5)
**Date:** 2026-06-17
**Files reviewed:** `src/SampleApp/Pricing/OrderCalculator.cs`, `src/SampleApp/Auth/TokenAuthenticator.cs`, `src/tests/SampleApp.Tests/UnitTest1.cs`

---

## Overall Risk: LOW

The headline remediation is correctly applied — hardcoded `const` and `==` comparison are gone, env-var read fails closed on missing/empty value, and `CryptographicOperations.FixedTimeEquals` is used with correct matching UTF-8 byte encoding. No exploitable high-impact vulnerability found in the changed code.

---

## Findings

### F1 — Real-looking secret committed to version control in test fixture
- **Severity:** MEDIUM
- **Location:** `src/tests/SampleApp.Tests/UnitTest1.cs:67–68`
- **Detail:** `"super-secret-admin-token-123"` — the exact previously-hardcoded production token value — is hardcoded in committed test code. The `const` was removed from `TokenAuthenticator.cs` but the value remains in git history and in the working tree. Secret scanners will flag it. If that value is reused as a real `ADMIN_TOKEN` in any environment, it is fully disclosed.
- **Remediation:** Use an obviously-fixture throwaway value generated at runtime:
  ```csharp
  string fixture = "test-only-" + Guid.NewGuid().ToString("N");
  Environment.SetEnvironmentVariable("ADMIN_TOKEN", fixture);
  TokenAuthenticator.IsAdmin(fixture).Should().BeTrue();
  ```
  Rotate the token if it was ever live.

### F2 — `FixedTimeEquals` leaks admin-token length via early length mismatch
- **Severity:** LOW
- **Location:** `src/SampleApp/Auth/TokenAuthenticator.cs:16–18`
- **Detail:** `CryptographicOperations.FixedTimeEquals` is constant-time only for equal-length buffers. It returns `false` immediately when lengths differ — this is an observable timing difference that narrows an attacker's search space to tokens of the correct byte-length.
- **Remediation:** Pre-hash both tokens to a fixed-width digest before comparing:
  ```csharp
  using var sha = SHA256.Create();
  byte[] a = sha.ComputeHash(Encoding.UTF8.GetBytes(token));
  byte[] b = sha.ComputeHash(Encoding.UTF8.GetBytes(adminToken));
  return CryptographicOperations.FixedTimeEquals(a, b);
  ```

### F3 — Admin token is a static shared secret with no rotation/expiry
- **Severity:** LOW
- **Location:** `src/SampleApp/Auth/TokenAuthenticator.cs:12`
- **Detail:** Authentication is a single long-lived bearer string from one env var. No expiry, rotation, per-user identity, or revocation mechanism. A leaked `ADMIN_TOKEN` grants permanent admin access until manually rotated.
- **Remediation:** For production use, move to signed expiring tokens (JWT) or an identity provider.

### F4 — `FixedTimeEquals` byte/encoding usage — VERIFIED CORRECT (no issue)
- **Severity:** INFO
- **Location:** `src/SampleApp/Auth/TokenAuthenticator.cs:16–18`
- **Detail:** Both operands are produced with `Encoding.UTF8.GetBytes`, so identical strings yield identical byte arrays. The null/empty guards make the method fail closed when either input or `ADMIN_TOKEN` is absent. Correct and safe.

### F5 — Process-wide env-var mutation in test (isolation concern, not a product vulnerability)
- **Severity:** INFO
- **Location:** `src/tests/SampleApp.Tests/UnitTest1.cs:67`
- **Detail:** `Environment.SetEnvironmentVariable("ADMIN_TOKEN", ...)` sets a process-level variable that is never cleared in the existing `IsAdmin_CorrectToken_ReturnsTrue` test, potentially bleeding into other tests in the same run.
- **Remediation:** Set and restore via `try/finally`, or use a test fixture that scopes the variable. (The new regression tests added by unit-test-generator already use save/restore in `finally`.)

### F6 — `OrderCalculator.cs` — no security issues
- **Severity:** INFO
- **Detail:** Pure decimal arithmetic. No injection, I/O, secrets, or untrusted-input surface. Division in `AverageUnitPrice` is guarded by the `items.Count == 0` check. No findings.

---

## Summary Table

| ID | Severity | Location | Description |
|---|---|---|---|
| F1 | MEDIUM | `UnitTest1.cs:67–68` | Real token value hardcoded in committed test |
| F2 | LOW | `TokenAuthenticator.cs:16–18` | `FixedTimeEquals` leaks token length |
| F3 | LOW | `TokenAuthenticator.cs:12` | Static secret with no expiry/rotation |
| F4 | INFO | `TokenAuthenticator.cs:16–18` | FixedTimeEquals encoding — verified correct |
| F5 | INFO | `UnitTest1.cs:67` | Process-wide env-var mutation in test |
| F6 | INFO | `OrderCalculator.cs` | No issues in pricing code |

| Severity | Count |
|---|---|
| CRITICAL | 0 |
| HIGH | 0 |
| MEDIUM | 1 |
| LOW | 2 |
| INFO | 3 |
