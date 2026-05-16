# Project Architecture (3-Layer)

The default backend layout for any homework with a server is **API + BLL + DAL + Tests** (Clean-Architecture-lite).

## Layout

```
homework-N/
└── src/
    ├── HomeworkN.sln
    ├── .editorconfig          # copied verbatim from .claude/static/
    ├── Directory.Build.props  # copied verbatim from .claude/static/
    ├── Dockerfile             # adapted from .claude/static/Dockerfile
    ├── .dockerignore          # optional, recommended
    ├── docker-compose.yml     # only if TASKS.md requires Postgres/Redis/etc.
    ├── .env.example           # accompanies docker-compose.yml; .env is gitignored
    ├── HomeworkN.Api/         # ASP.NET Core minimal API host
    │   ├── Program.cs
    │   ├── Endpoints/         # one file per resource group
    │   ├── Models/            # request/response DTOs (records)
    │   └── Validators/        # FluentValidation
    ├── HomeworkN.Bll/         # business logic (services, classifiers, rules)
    │   ├── Domain/            # domain types (BLL-owned)
    │   ├── Abstractions/      # repo interfaces consumed by BLL
    │   └── Services/
    ├── HomeworkN.Dal/         # data access (in-memory stores, repos)
    │   ├── Entities/          # DAL-owned entity types
    │   └── Repositories/      # implementations of BLL interfaces
    └── HomeworkN.Tests/       # single xUnit project covering all layers
        ├── Api/
        ├── Bll/
        └── Dal/
```

The three solution-level files (`.editorconfig`, `Directory.Build.props`, `Dockerfile`) are required for every homework. See `common-rules.md#project-setup-files-every-solution` for the first two and `../Infrastructure/docker-conventions.md` for the Dockerfile.

## Layer responsibilities and dependency direction

```
Api  ──▶  Bll  ──▶  Dal
```

- **API never touches DAL.** It receives requests, validates DTOs, calls a BLL service, and shapes the response. No data access logic, no entity types.
- **BLL** holds business rules and the domain model. It consumes DAL through **interfaces** (defined in `Bll/Abstractions/`) — never concrete DAL types. BLL owns its own domain types.
- **DAL** owns its own entity types and implements the interfaces declared by BLL. **BLL maps DAL entities ↔ BLL domain types** at the repository call boundary. This means DAL storage can change without forcing BLL changes.
- Dependencies flow **one way**. DAL must not reference BLL; BLL must not reference API. Enforce by `<ProjectReference>` direction in csproj files.

## Project references

```powershell
# downward references in production code
dotnet add .\HomeworkN.Bll\HomeworkN.Bll.csproj  reference .\HomeworkN.Dal\HomeworkN.Dal.csproj
dotnet add .\HomeworkN.Api\HomeworkN.Api.csproj  reference .\HomeworkN.Bll\HomeworkN.Bll.csproj

# tests reference all three
dotnet add .\HomeworkN.Tests\HomeworkN.Tests.csproj reference .\HomeworkN.Api\HomeworkN.Api.csproj
dotnet add .\HomeworkN.Tests\HomeworkN.Tests.csproj reference .\HomeworkN.Bll\HomeworkN.Bll.csproj
dotnet add .\HomeworkN.Tests\HomeworkN.Tests.csproj reference .\HomeworkN.Dal\HomeworkN.Dal.csproj
```

## Scaffold (PowerShell)

```powershell
# Run from homework-N\
New-Item -ItemType Directory -Path src; Set-Location src
dotnet new sln      -n HomeworkN --format sln                # .NET 10 SDK creates .slnx by default; force classic .sln so PLAN.md verify blocks (`dotnet build HomeworkN.sln`) resolve.
dotnet new webapi   -n HomeworkN.Api  --use-minimal-apis --no-openapi  # `--no-openapi` skips the Microsoft.AspNetCore.OpenApi dep; add it back manually if you want WithOpenApi() routing metadata.
dotnet new classlib -n HomeworkN.Bll
dotnet new classlib -n HomeworkN.Dal
dotnet new xunit    -n HomeworkN.Tests
dotnet sln add .\HomeworkN.Api\HomeworkN.Api.csproj
dotnet sln add .\HomeworkN.Bll\HomeworkN.Bll.csproj
dotnet sln add .\HomeworkN.Dal\HomeworkN.Dal.csproj
dotnet sln add .\HomeworkN.Tests\HomeworkN.Tests.csproj
# (then add the project references above)

# Delete the `dotnet new` template stubs — under TreatWarningsAsErrors + SonarAnalyzer they fail S2094 (empty class), S1186 (empty method), S2699 (no-assert test).
Remove-Item .\HomeworkN.Bll\Class1.cs, .\HomeworkN.Dal\Class1.cs, .\HomeworkN.Tests\UnitTest1.cs -ErrorAction SilentlyContinue

# Copy solution-level templates from .claude/static/
Copy-Item ..\..\.claude\static\.editorconfig          .\.editorconfig
Copy-Item ..\..\.claude\static\Directory.Build.props  .\Directory.Build.props
Copy-Item ..\..\.claude\static\Dockerfile             .\Dockerfile
# Then edit Dockerfile per ../Infrastructure/docker-conventions.md
```

> **.NET 10 SDK gotchas observed during HW1 M1 scaffold.** `dotnet new sln` defaults to the new XML `.slnx` format — pass `--format sln` for the legacy text format that grep-friendly milestone Verify blocks reference by path. The `webapi` template's `Program.cs` uses `WithOpenApi()` which only resolves when `Microsoft.AspNetCore.OpenApi` is installed; pair `--no-openapi` with the template to drop the dependency, or keep the package and the call. The classlib/xunit templates plant `Class1.cs` / `UnitTest1.cs` whose empty bodies trip Sonar S2094/S1186/S2699 the first time you build with this repo's strict `Directory.Build.props` — delete them as part of scaffolding, not as a follow-up.

For PowerShell quirks (encoding, no `&&`) see `../Infrastructure/powershell-conventions.md`. For the Dockerfile substitutions see `../Infrastructure/docker-conventions.md`.

## When to deviate

A trivial homework (single 50-line endpoint, no real domain logic) may collapse `Bll` + `Dal` into the API project. If you do, call it out in `homework-N/CLAUDE.md` so reviewers know it was intentional. **The default is the three-layer split.**
