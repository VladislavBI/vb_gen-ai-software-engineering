# .NET / ASP.NET Core Stack

The default backend stack for homework in this repository is **.NET (ASP.NET Core)**. Most `TASKS.md` files describe assignments using Node.js or Python as examples — translate the requirements to .NET idioms rather than switching stacks.

This file covers **versions and tooling only**. For the rest, see the sibling docs:
- `project-architecture.md` — API / BLL / DAL layout, layer responsibilities, scaffold commands.
- `testing-strategy.md` — xUnit + FluentAssertions + Moq, what to test where.
- `common-rules.md` — type choices, endpoint conventions, things to avoid.
- `../Infrastructure/powershell-conventions.md` — shell quirks for PS 5.1.

## Versions

- **.NET SDK**: latest LTS (odd major, .NET 10 or newer). Verify with `dotnet --version` before scaffolding.
- **C# language version**: SDK default; do not pin manually.

## Project types

- **`webapi`** (minimal API style — `WebApplication.CreateBuilder` → `app.MapGet/MapPost`) for the `Api` project. Use MVC/controllers only if `TASKS.md` explicitly demands them.
- **`classlib`** for `Bll` and `Dal`.
- **`xunit`** for `Tests`.

## NuGet packages (defaults)

| Concern | Package | Where it goes |
|---|---|---|
| Validation | `FluentValidation`, `FluentValidation.AspNetCore` | Api |
| Serialization | `System.Text.Json` (built in) | Api |
| Test runner | `xunit`, `xunit.runner.visualstudio` | Tests (template) |
| Asserts | `FluentAssertions` | Tests |
| Mocking | `Moq` | Tests |
| Coverage | `coverlet.collector` | Tests (template) |
| API integration | `Microsoft.AspNetCore.Mvc.Testing` | Tests |

Do **not** add Entity Framework / SQL Server unless an assignment explicitly requires persistence — HW1 / HW2 mandate in-memory storage.

## Serialization configuration

In `Program.cs`, set `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` to match the JSON shapes shown in `TASKS.md`.
