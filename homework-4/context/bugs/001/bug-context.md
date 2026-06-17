# Bug Context — 001

This file seeds the 4-agent pipeline. The Bug Researcher (Stage 1) reads it to
know where to look in `src/` before writing `research/codebase-research.md`.

---

## App overview

`src/SampleApp` is a .NET 10 console CLI with two subsystems:

- `Pricing/OrderCalculator.cs` — discount + tax calculation for order items.
- `Auth/TokenAuthenticator.cs` — admin token verification.

Run the app:
```powershell
dotnet run --project src/SampleApp -- total <quantity> <unitPrice>
dotnet run --project src/SampleApp -- auth <token>
```

Run tests:
```powershell
dotnet test src/SampleApp.slnx
```

---

## Seeded flaws

### Bug 1 — Off-by-one discount boundary

| Field | Detail |
|---|---|
| **File** | `src/SampleApp/Pricing/OrderCalculator.cs` |
| **Method** | `GetDiscountRate(int quantity)` |
| **Line** | ~13 (the `return quantity > 10 ? ...` line) |
| **Spec** | 10 % discount for **10 or more** units |
| **Defect** | `quantity > 10` should be `quantity >= 10` |
| **Effect** | Exactly 10 units gets 0 % discount instead of 10 % |
| **Repro** | `dotnet run --project src/SampleApp -- total 10 100` → prints `Discount: 0%` (should be 10%) |

---

### Bug 2 — Tax applied before discount

| Field | Detail |
|---|---|
| **File** | `src/SampleApp/Pricing/OrderCalculator.cs` |
| **Method** | `CalculateTotal(OrderItem item)` |
| **Line** | ~21 (the `decimal tax = subtotal * TaxRate` line) |
| **Spec** | Tax is calculated on the **discounted** subtotal |
| **Defect** | Tax is computed on the **pre-discount** subtotal |
| **Effect** | Customers are overcharged whenever a discount applies |
| **Correct formula** | `total = subtotal * (1 - discount) * (1 + tax)` |
| **Repro** | `dotnet run --project src/SampleApp -- total 20 100` → total should be `1944.00` (2000 × 0.9 × 1.08) but current code returns `1960.00` (2000×0.9 + 2000×0.08) |

---

### Security Issue — Hardcoded token + insecure comparison + missing validation

| Field | Detail |
|---|---|
| **File** | `src/SampleApp/Auth/TokenAuthenticator.cs` |
| **Method** | `IsAdmin(string token)` |
| **Line** | ~8 (`private const string AdminToken`) and ~12 (`return token == AdminToken`) |
| **Finding 1** | `AdminToken` is hardcoded in source — secrets must come from environment/config, never source |
| **Finding 2** | `token == AdminToken` uses non-constant-time string equality — susceptible to timing attack |
| **Finding 3** | No null/empty guard — `null` token causes `NullReferenceException` |
| **Severity** | CRITICAL (hardcoded secret), HIGH (timing attack), MEDIUM (missing null guard) |
| **Repro** | `dotnet run --project src/SampleApp -- auth super-secret-admin-token-123` → `Access GRANTED` |
