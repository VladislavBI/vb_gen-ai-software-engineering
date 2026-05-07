# Project Architecture (3-Layer)

The default backend layout for any homework with a server is **API + BLL + DAL + Tests** (Clean-Architecture-lite).

## Layout

```
homework-N/
└── src/
    ├── HomeworkN.sln
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
dotnet new sln      -n HomeworkN
dotnet new webapi   -n HomeworkN.Api  --use-minimal-apis
dotnet new classlib -n HomeworkN.Bll
dotnet new classlib -n HomeworkN.Dal
dotnet new xunit    -n HomeworkN.Tests
dotnet sln add .\HomeworkN.Api\HomeworkN.Api.csproj
dotnet sln add .\HomeworkN.Bll\HomeworkN.Bll.csproj
dotnet sln add .\HomeworkN.Dal\HomeworkN.Dal.csproj
dotnet sln add .\HomeworkN.Tests\HomeworkN.Tests.csproj
# (then add the project references above)
```

For PowerShell quirks (encoding, no `&&`) see `../Infrastructure/powershell-conventions.md`.

## When to deviate

A trivial homework (single 50-line endpoint, no real domain logic) may collapse `Bll` + `Dal` into the API project. If you do, call it out in `homework-N/CLAUDE.md` so reviewers know it was intentional. **The default is the three-layer split.**
