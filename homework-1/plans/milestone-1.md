# Milestone 1: Scaffold solution and DI skeleton — Session Plan

**Started:** 2026-05-08
**Super-plan reference:** ../PLAN.md milestone 1

## Approach

Use `dotnet new` CLI commands to create a four-project solution under `homework-1/src/`: one `webapi` project (minimal API style) for `Homework1.Api`, two `classlib` projects for `Homework1.Bll` and `Homework1.Dal`, and one `xunit` project for `Homework1.Tests`. Wire project references in the correct dependency direction (Api → Bll → Dal, Tests → all three). Copy `.editorconfig` and `Directory.Build.props` verbatim from `.claude/static/` into `src/` immediately after `dotnet new sln` so the strict analyzer and style rules are enforced from the first compilation. The `Program.cs` for the Api project will boot the ASP.NET Core minimal API pipeline and register a `GET /health` endpoint returning a JSON object with a `status` field; `AddSwaggerGen` and `UseSwagger` are enabled in Development so the running-app screenshot requirement is satisfied. DI registration is left open for later milestones — M1 only needs the build to compile and the health endpoint to respond. The alternative of writing all four projects manually was rejected: `dotnet new` guarantees valid `.csproj` metadata and keeps the approach consistent with the scaffold guide in `project-architecture.md`.

## Touch list

- `homework-1/src/Homework1.sln` — new solution file; add all four projects.
- `homework-1/src/.editorconfig` — copied verbatim from `.claude/static/.editorconfig`.
- `homework-1/src/Directory.Build.props` — copied verbatim from `.claude/static/Directory.Build.props`.
- `homework-1/src/Homework1.Api/Program.cs` — replace generated template; configure JSON camelCase, add Swagger in Dev, add `GET /health` returning `{ status: "ok" }`.
- `homework-1/src/Homework1.Api/Homework1.Api.csproj` — ensure framework `net10.0`, add `FluentValidation.AspNetCore` package reference; add project reference to `Homework1.Bll`.
- `homework-1/src/Homework1.Bll/Homework1.Bll.csproj` — classlib targeting `net10.0`; add project reference to `Homework1.Dal`.
- `homework-1/src/Homework1.Dal/Homework1.Dal.csproj` — classlib targeting `net10.0`; no extra references.
- `homework-1/src/Homework1.Tests/Homework1.Tests.csproj` — xunit project; add references to all three production projects; add `FluentAssertions`, `Moq`, `Microsoft.AspNetCore.Mvc.Testing` packages.

## Review focus

- Project reference direction: verify Api → Bll → Dal one-way chain; Tests → all three; no DAL → BLL or BLL → API references.
- `Directory.Build.props` and `.editorconfig` must be present in `src/` (not inside a project subdirectory) so they apply to all projects.
- `Program.cs` must use file-scoped namespace, `var` only when type is apparent, no `this.` qualification — enforced by `.editorconfig` at `error` severity.
- `GET /health` must return a non-empty response body (the verify checks truthiness of `$health`); confirm the endpoint is wired with `app.MapGet` before `app.Run()`.
- No leftover `WeatherForecast` generated files or using the template's `Controllers/` folder — this is minimal API style.

## Notes

- .NET 10 SDK defaults to creating `.slnx` format solution files; had to pass `--format sln` to get the classic `.sln` format that the PLAN.md Verify block references as `Homework1.sln`.
- `dotnet new webapi` with `--no-openapi` generates a minimal Program.cs without the Microsoft.AspNetCore.OpenApi package; `WithOpenApi()` extension was removed since it requires that package and is not needed for M1's health endpoint.
- `dotnet new classlib` and `dotnet new xunit` generate placeholder `Class1.cs` and `UnitTest1.cs` files that Sonar rules S2094/S2699/S1186 flag as errors under `TreatWarningsAsErrors`. These were deleted as part of standard scaffold cleanup (not feature code).
- Explicit `using` directives were kept in `Program.cs` for clarity even though `ImplicitUsings=enable` covers them; both approaches compile cleanly and the reviewer flagged this as minor/NIT only.
- Build passes with 0 errors and 0 warnings. `GET /health` returns `{"status":"ok"}` as verified by `Invoke-RestMethod`. Process started and stopped cleanly in the Verify block.
