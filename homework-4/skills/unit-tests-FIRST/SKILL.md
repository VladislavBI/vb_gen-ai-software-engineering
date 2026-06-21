---
name: unit-tests-first
description: The FIRST principles (Fast, Independent, Repeatable, Self-validating, Timely) the unit-test-generator agent applies when generating tests for changed code and reports in test-report.md. Private to that agent; not registered as a runtime skill.
---

# Skill: FIRST Unit-Test Principles

> **Scope:** Used **only** by the `unit-test-generator` agent when generating tests for changed
> code. It lives under `homework-4/skills/` (not `.claude/skills/`) so it is not loaded into the
> normal session flow — it is private to the agent that reads it.

## Purpose

Every unit test the generator writes must satisfy **FIRST**. FIRST is the bar that separates
a useful unit test from a flaky or low-value one. The generator applies each principle while
writing tests and confirms compliance in `test-report.md`.

## The FIRST Principles

| Letter | Principle | Rule the test must follow |
|--------|-----------|---------------------------|
| **F** | **Fast** | Runs in milliseconds. No real network, disk, database, sleeps, or wall-clock waits. Mock or inject anything slow. |
| **I** | **Independent** | No ordering dependency and no shared mutable state between tests. Each test sets up its own fixtures and can run alone or in any order. |
| **R** | **Repeatable** | Deterministic — same result on every run, every machine. No reliance on current time, random seeds, locale, env vars, or external services; inject those as fakes. |
| **S** | **Self-validating** | Passes or fails via explicit assertions. No manual inspection of output/logs to judge the result; the assertion encodes the expectation. |
| **T** | **Timely** | Scoped to the code that just changed (per `fix-summary.md`). Cover the behaviors the fix introduced or repaired — happy path, boundaries, invalid input, and the specific bug's regression case. |

## Application Checklist (run before finalizing each test)

- [ ] No I/O, network, DB, or `sleep` in the test path (F, R).
- [ ] All external collaborators and non-deterministic sources (clock, RNG, env) are mocked/injected (R).
- [ ] Test creates its own arrange state; removing any other test does not break it (I).
- [ ] Exactly the expected outcome is asserted; failure message is meaningful (S).
- [ ] At least one test targets the regression the fix addressed (T).

## Code Examples

**Good — FIRST-compliant test (xUnit + Moq, C#):**

```csharp
// Fast, Independent, Repeatable, Self-validating, Timely
[Fact]
public void CalculateBalance_DeductsDebitsFromCredits()
{
    // Arrange — own state, no shared fixtures
    var transactions = new List<Transaction>
    {
        new(Id: Guid.NewGuid(), Amount: 100m, Type: TransactionType.Credit, Status: TransactionStatus.Completed),
        new(Id: Guid.NewGuid(), Amount: 30m,  Type: TransactionType.Debit,  Status: TransactionStatus.Completed),
    };
    var repo = new Mock<ITransactionRepository>();
    repo.Setup(r => r.GetByAccountId(It.IsAny<Guid>())).Returns(transactions);
    var sut = new AccountService(repo.Object);

    // Act
    var balance = sut.CalculateBalance(Guid.NewGuid());

    // Assert — explicit, no log inspection
    balance.Should().Be(70m);
}
```

**Bad — violates Fast (real DB) and Repeatable (wall-clock):**

```csharp
// ❌ Do NOT write tests like this
[Fact]
public async Task CreateTicket_PersistsToDatabase()
{
    using var db = new AppDbContext(realConnectionString); // I/O — violates F
    var service = new TicketService(db);
    var ticket = new Ticket { CreatedAt = DateTime.Now }; // wall-clock — violates R
    await service.CreateAsync(ticket);
    var result = await db.Tickets.FindAsync(ticket.Id);
    Assert.NotNull(result);
}
```

**Regression-case pattern (Timely — targets the specific bug):**

```csharp
[Fact]
public void ProcessItems_EmptyList_DoesNotThrow()
{
    // Regression for bug-001: NullReferenceException when items list was empty
    var sut = new ItemProcessor();
    var act = () => sut.Process(new List<Item>());
    act.Should().NotThrow();
}
```

## How to Report

In `test-report.md`, include a **FIRST Compliance** section: for each generated test (or test
group), confirm F/I/R/S/T are met, and flag any test that needed a deviation with the reason.
If a piece of changed code could not be tested under FIRST (e.g. no seam to inject a
dependency), record it as a gap with the minimal refactor that would make it testable — do not
weaken FIRST to force a test through.
