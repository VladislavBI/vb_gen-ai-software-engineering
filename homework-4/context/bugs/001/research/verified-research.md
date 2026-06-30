# Verified Research — Bug 001

**Verifier:** Research Verifier (Stage 2)
**Date:** 2026-06-17
**Source under verification:** `context/bugs/001/research/codebase-research.md`

---

## Verification Summary

**Overall verdict: PASS**
**Research Quality: B — Good**

All 5 findings reference locations that resolve, and every quoted code snippet matches
the source **verbatim** at the stated line number. The four primary findings (F1–F4) are
fully supported by cited evidence. One secondary claim inside Finding 5 (that a `null`
token causes a `NullReferenceException`) is contradicted by C# string-equality semantics —
but this is a non-central, self-acknowledged discrepancy that does not undermine the
trustworthiness of the core bugs. No blocking discrepancies remain.

Reference-resolution rate: **5 / 5 (100%)** references resolve; **5 / 5** snippets match exactly.

---

## Verified Claims

### Finding 1 — Off-by-one Discount Boundary — VERIFIED
- **Reference:** `src/SampleApp/Pricing/OrderCalculator.cs:12` — resolves.
- **Snippet match:** Line 12 reads `        return quantity > 10 ? 0.10m : 0m;` — matches verbatim.
- **Claim support:** Root cause (`>` instead of `>=`; `quantity == 10` yields `false` → `0m`)
  is directly supported by the source. The source even carries a confirming comment at line 9:
  `// BUG 1: uses > instead of >= — exactly 10 units receives no discount.`
- **Repro support:** `Program.cs:28` prints `Discount : {discount * 100:F0}%`; for `qty=10`,
  `discount=0m` → `Discount : 0%`. Supported.

### Finding 2 — Tax Applied to Pre-Discount Subtotal — VERIFIED
- **Reference:** `src/SampleApp/Pricing/OrderCalculator.cs:23` — resolves.
- **Snippet match:** Lines 21–24 match verbatim, including line 23
  `        decimal tax = subtotal * TaxRate;` and line 24
  `        return subtotal - (subtotal * discount) + tax;`.
- **Claim support:** Tax is computed on raw `subtotal` before the discount reduction;
  algebra in the research (`subtotal × (1 − discount + TaxRate)` vs. correct
  `subtotal × (1 − discount)(1 + TaxRate)`) is arithmetically correct.
- **Repro support:** For `qty=20, price=100`: `subtotal=2000`, `discount=0.10`,
  `tax=2000×0.08=160`, return `2000 − 200 + 160 = 1960.00` (buggy); correct is
  `2000×0.9×1.08 = 1944.00`. Both figures verified.

### Finding 3 — Hardcoded Admin Token — VERIFIED
- **Reference:** `src/SampleApp/Auth/TokenAuthenticator.cs:9` — resolves.
- **Snippet match:** Line 9 reads
  `    private const string AdminToken = "super-secret-admin-token-123";` — matches verbatim.
- **Claim support:** Secret is a compile-time `const` embedded in source. Fully supported.

### Finding 4 — Non-Constant-Time Comparison (Timing Attack) — VERIFIED
- **Reference:** `src/SampleApp/Auth/TokenAuthenticator.cs:13` — resolves.
- **Snippet match:** Line 11 `    public static bool IsAdmin(string token)` and line 13
  `        return token == AdminToken;` match verbatim.
- **Claim support:** `==` on strings is not constant-time; timing side-channel reasoning is sound.

### Finding 5 — No Null/Empty Guard on Token — VERIFIED (location) / PARTIAL (claim)
- **Reference:** `src/SampleApp/Auth/TokenAuthenticator.cs:11–13` — resolves; snippet matches verbatim.
- **Claim support:** The structural observation (no explicit null/empty guard, no documented
  null contract) is valid against the source. The *mechanism* claim is partly inaccurate — see
  Discrepancies below.

---

## Discrepancies Found

### D1 — Finding 5: incorrect `NullReferenceException` claim (non-blocking)
- **Research said:** The inline snippet annotation states
  `// line 13 — null token causes NullReferenceException`, and the source comment at line 8
  likewise asserts `No null/empty guard — null token throws NullReferenceException.`
- **Source actually shows:** Line 13 is `return token == AdminToken;`. In C#, the `==`
  operator on `string` resolves to `string.operator==`, which is null-safe — a `null` `token`
  compared against the non-null `AdminToken` returns `false` and does **not** throw a
  `NullReferenceException`.
- **Assessment:** The research's own body text (lines 153–154) explicitly recognizes this
  ("`==` operator on a `null` left operand ... would normally return `false` in C#") and hedges
  the finding accordingly, so the document does not actually rely on the false mechanism for its
  conclusion. The valid residual point — no explicit contract/guard for null or empty input —
  stands. Treated as a **minor, documented, non-blocking** discrepancy. Recommendation for the
  Planner: scope Finding 5 as a defensive-coding/contract improvement, not as an NRE fix; if a
  guard is added, do not justify it with a non-existent NRE.

### Note — Overview line counts (informational, all correct)
- `OrderCalculator.cs` = 39 lines, `TokenAuthenticator.cs` = 15 lines, `OrderItem.cs` = 3 lines,
  `Program.cs` = 49 lines all match the file states. (`UnitTest1.cs` = 72 was not opened; outside
  the 5-finding scope.) No discrepancy.

---

## Research Quality Assessment

**Level: B — Good**

**Reasoning:** Every one of the 5 file:line references resolves and every quoted snippet matches
the source verbatim (100% resolution, 100% exact-match), and the four primary findings (F1–F4)
are each backed by concrete cited evidence with arithmetically correct repro figures
(e.g. `1960.00` vs `1944.00` for F2). Level A is withheld because Finding 5 carries one
contradicted secondary claim — that a `null` token throws `NullReferenceException`, which C#
null-safe `==` semantics refute. That discrepancy is non-central, self-acknowledged by the
research, and leaves the finding's valid core (missing null/empty guard) intact, so it caps the
rating at B rather than dropping to C or D.

---

## References (inspected to verify)

- `src/SampleApp/Pricing/OrderCalculator.cs` — lines 1–39 (focus 10–13, 19–25)
- `src/SampleApp/Auth/TokenAuthenticator.cs` — lines 1–15 (focus 8–14)
- `src/SampleApp/Pricing/OrderItem.cs` — lines 1–3
- `src/SampleApp/Program.cs` — lines 1–49 (focus 23–29, 33–43; repro/output verification)
- `skills/research-quality-measurement/SKILL.md` — quality-level rubric applied
