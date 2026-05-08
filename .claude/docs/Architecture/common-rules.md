# Common Code Rules

Conventions applied to every homework's API/BLL/DAL projects. Read once; reapply across homeworks.

## Project setup files (every solution)

Every homework's `src/` directory ships with two files copied **verbatim** from `.claude/static/`:

- **`.editorconfig`** — formatting, naming, and Roslyn analyzer rules. Severities are `error` for almost every style preference, so violations break the build (combined with `EnforceCodeStyleInBuild=true` below). The IDE and `dotnet build` both auto-discover it; no project-level wiring is needed.
- **`Directory.Build.props`** — applied automatically to **every** `.csproj` underneath it (Api, Bll, Dal, Tests). Enables `TreatWarningsAsErrors=true`, `CodeAnalysisTreatWarningsAsErrors=true`, `EnforceCodeStyleInBuild=true`, `AnalysisMode=All`, and pulls in `SonarAnalyzer.CSharp` as a transitive analyzer for the whole solution.

Copy both into `homework-N/src/` (next to the `.sln`) immediately after `dotnet new sln`. Do **not** modify them per homework — if a rule is wrong, fix it in the static template so every homework picks up the change. Per-project opt-outs (e.g. relaxing `TreatWarningsAsErrors` for `Tests`) belong in the project's own `.csproj`, not in this shared file.

**Authoritative style source**: when generating, modifying, or reviewing C# in this repo, treat `.editorconfig` as the source of truth, not surrounding code (which may pre-date a rule). Read it before authoring code and apply its rules in the diff. The high-impact rules to internalize:

- `csharp_style_namespace_declarations = file_scoped:error` — file-scoped namespaces only.
- `csharp_style_var_for_built_in_types = false:error`, `csharp_style_var_when_type_is_apparent = true:error` — `int x = 1;` not `var x = 1;`, but `var list = new List<Foo>();` is correct.
- `dotnet_style_qualification_for_*_field/method/property = false:error` — never write `this.Foo`.
- `csharp_using_directive_placement = outside_namespace:error` — `using` statements above the namespace.
- `csharp_style_expression_bodied_properties/operators/accessors = true:error` — single-expression members use `=>` form.
- `dotnet_style_readonly_field = true:error` — fields that are never reassigned must be `readonly`.
- `dotnet_code_quality_unused_parameters = all:error` — unused parameters break the build; remove them or discard with `_`.

If a generated diff would fail one of these, the build fails. Self-check against `.editorconfig` before claiming a code change is complete; do not rely on the test suite to surface style errors.

## Type choices

- **DTOs and domain types**: `record` (immutable, structural equality, plays well with FluentAssertions).
- **Money**: `decimal`. Never `double` or `float`.
- **Timestamps / `created_at`**: `DateTimeOffset`. Never `DateTime` (timezone ambiguity bites in tests).
- **Identifiers**: `Guid`. Generate at the BLL boundary, not in DAL.
- **Collection return types**: `IReadOnlyList<T>`. Use `List<T>` only as a local builder.
- **No `dynamic`, no `object`, no untyped `Dictionary<string, object>`** for request bodies. Use a record DTO.

## Endpoints (API)

- One file per resource group: `Endpoints/TransactionsEndpoints.cs` exposes `MapTransactions(this IEndpointRouteBuilder app)`.
- `Program.cs` calls each `Map*` extension. Keeps `Program.cs` short.
- Use `Results.Created(...)`, `Results.NotFound()`, `Results.BadRequest()`, `Results.ValidationProblem(...)` helpers — they set status codes and `Location` headers correctly.
- Validation errors flow as RFC 7807 `ProblemDetails` (`details: [{field, message}]`). Map FluentValidation errors into the dictionary `Results.ValidationProblem` expects.

## Services (BLL)

- Inject DAL repos as **interfaces** (e.g., `ITransactionRepository`), declared in `Bll/Abstractions/`. Never inject concrete DAL types.
- BLL owns the domain types. Map DAL entities ↔ BLL domain types **inside the BLL** — DAL types must not leak to API.
- Throw domain-specific exceptions (or return `Result<T>` if you prefer); the API layer translates them to HTTP status codes.

## Storage (DAL)

- In-memory stores: `ConcurrentDictionary<Guid, TEntity>` registered as a **singleton** service. Never `static` fields.
- Repository **interface** lives in BLL (`Bll/Abstractions/`); the **implementation** lives in DAL. BLL depends on the abstraction, not the implementation.
- DAL has its own entity types. Do not reuse BLL domain types as the storage shape.
- Don't add Entity Framework / SQL Server unless `TASKS.md` explicitly requires persistence — HW1 and HW2 mandate in-memory.

## Validation

- **FluentValidation** for request DTOs (preferred over data annotations for HW1 Task 2 and HW2 Task 1).
- One validator per DTO, registered in DI. Keep rules declarative; no business-rule branching inside validators.

## Serialization

- Built-in `System.Text.Json` with `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` to match the JSON shapes in `TASKS.md`.

## Logging

- `ILogger<T>` injected via DI.
- **Structured** events only: `logger.LogInformation("Classified ticket {TicketId} as {Category}", id, category)`. Never interpolated strings — they break log aggregators and HW2's "log all classification decisions" grading.

## OpenAPI / Swagger

- Keep enabled in Development. A `/swagger` screenshot satisfies the "API running" evidence requirement.

## Things to avoid

- Catching `Exception` broadly. Let validation surface through FluentValidation; let the framework handle 500s.
- Hardcoding ports. Use the default `dotnet run` assignment and document the actual URL in `HOWTORUN.md` after first run.
- Cross-layer leaks: API knowing about DAL types, DAL referencing BLL — both indicate a missing mapping or wrong project-reference direction.
