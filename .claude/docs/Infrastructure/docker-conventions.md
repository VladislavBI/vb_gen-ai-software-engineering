# Docker Conventions

Every homework backend ships with a **Dockerfile** in its `src/` directory, alongside the `.sln`. Containerization gives reviewers a one-command path to running the API without installing the .NET SDK locally, and it proves the project boots cleanly outside Visual Studio. The Dockerfile is part of the "running app evidence" requirement; add it before opening the PR.

The reference template is **`.claude/static/Dockerfile`** — copy it into each homework's `src/` and substitute the project names. Do not invent a different layout.

## When to add a Dockerfile

- **Always**, for any homework whose `TASKS.md` requires a runnable backend (HW1, HW2, …).
- Even trivial single-endpoint homeworks get one. Reviewers should never need a local .NET install to grade.

## Layout

```
homework-N/
└── src/
    ├── HomeworkN.sln
    ├── Dockerfile          ← here, beside the .sln
    ├── .dockerignore       ← optional, recommended (see below)
    ├── HomeworkN.Api/
    ├── HomeworkN.Bll/
    ├── HomeworkN.Dal/
    └── HomeworkN.Tests/
```

The Dockerfile sits next to the `.sln`. The build context must be `src/` so `dotnet restore` can see every `.csproj` it references.

## The multi-stage pattern

The reference Dockerfile uses **four stages**. Keep this shape; do not collapse it into a single stage.

| Stage     | Base image                              | Purpose                                                |
|-----------|-----------------------------------------|--------------------------------------------------------|
| `base`    | `mcr.microsoft.com/dotnet/aspnet:N.0`   | Runtime-only image — the final image inherits from it. |
| `build`   | `mcr.microsoft.com/dotnet/sdk:N.0`      | Has the SDK; restores and compiles.                    |
| `publish` | (extends `build`)                       | Runs `dotnet publish` to produce the deployable output.|
| `final`   | (extends `base`)                        | Copies only the published output — no SDK, no sources. |

The split exists for **image size and build cache**: csproj files are copied first so `dotnet restore` is cached separately from source changes; only the published output lands in the final image.

## Adapting the static template

`.claude/static/Dockerfile` is shaped for a Bookify-style 4-project DDD layout (`Api / Application / Domain / Infrastructure`). Translate to our 3-layer convention:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HomeworkN.Api/HomeworkN.Api.csproj", "HomeworkN.Api/"]
COPY ["HomeworkN.Bll/HomeworkN.Bll.csproj", "HomeworkN.Bll/"]
COPY ["HomeworkN.Dal/HomeworkN.Dal.csproj", "HomeworkN.Dal/"]
COPY ["Directory.Build.props", "."]
RUN dotnet restore "./HomeworkN.Api/HomeworkN.Api.csproj"
COPY . .
WORKDIR "/src/HomeworkN.Api"
RUN dotnet build "./HomeworkN.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HomeworkN.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HomeworkN.Api.dll"]
```

Substitutions when copying the static template:

- Replace `Bookify` with `HomeworkN` everywhere.
- Drop the `src/` prefix from `COPY` paths — our `Dockerfile` already lives in `src/`, so paths are project-relative.
- Bump base-image tags (`aspnet:8.0` / `sdk:8.0`) to match the SDK LTS version pinned in `../Architecture/dotnet-stack.md` (.NET 10 at the time of writing). The two tags must match each other.
- **Copy `Directory.Build.props` before `dotnet restore`** — analyzers configured there must be available during the restore-and-build phase, otherwise the in-container build fails differently than the local build.
- Do **not** copy the `*.Tests.csproj` — tests are not part of the runtime image. They run via `dotnet test` outside Docker.

## Build and run (PowerShell)

```powershell
# From homework-N\src
docker build -t homeworkN-api .
docker run --rm -p 5000:8080 homeworkN-api

# Smoke test
Invoke-RestMethod http://localhost:5000/health
```

ASP.NET Core's container default listens on port **8080** (post-.NET 8) — map host:container accordingly. The `EXPOSE` lines in the Dockerfile must match what `Program.cs` actually binds to.

## Docker Compose (when an external service is required)

Plain `docker run` is enough for a self-contained API. The moment the homework needs a **second service** — Postgres for persistence, Redis for caching or pub/sub, MinIO for object storage, RabbitMQ for messaging — switch to **`docker compose`**. Compose handles the bridge network, startup ordering, named volumes, and `.env` injection that `docker run` chains do not.

**Gate on `TASKS.md`.** HW1 and HW2 mandate in-memory storage — adding Postgres there violates the assignment (see `../Architecture/common-rules.md`). Only add Compose when `TASKS.md` explicitly requires an external service or persistence across restarts. If you do, mention it in the per-homework `CLAUDE.md` so reviewers know it was intentional.

### Layout

`docker-compose.yml` lives next to the `Dockerfile`:

```
homework-N/
└── src/
    ├── HomeworkN.sln
    ├── Dockerfile
    ├── docker-compose.yml       # ← here
    ├── .dockerignore
    └── HomeworkN.Api/...
```

### Reference shape (Postgres + Redis example)

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:8080"
    environment:
      ConnectionStrings__Default: "Host=postgres;Database=homeworkN;Username=app;Password=${POSTGRES_PASSWORD}"
      Redis__ConnectionString: "redis:6379"
      ASPNETCORE_ENVIRONMENT: Development
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy

  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: homeworkN
      POSTGRES_USER: app
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U app -d homeworkN"]
      interval: 5s
      timeout: 3s
      retries: 5

  redis:
    image: redis:7-alpine
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 5

volumes:
  postgres-data:
```

### Conventions

- **Service names are DNS hostnames** inside the Compose network. The API connects to `postgres:5432` and `redis:6379` — never `localhost`. The hostnames go straight into connection strings via env vars (see `ConnectionStrings__Default` above; the `__` becomes `:` in `IConfiguration`).
- **Always declare healthchecks** on dependency services and gate the API with `depends_on: condition: service_healthy`. Without this, the API starts before Postgres accepts connections and crashes on first query — the most common Compose-only bug.
- **Pin major versions** of dependency images (`postgres:16-alpine`, `redis:7-alpine`). `:latest` makes the build non-reproducible across reviewers.
- **Persist data in named volumes** (`postgres-data:`), not bind mounts. Named volumes are portable across hosts and don't pollute the repo with database files.
- **Never commit secrets.** `POSTGRES_PASSWORD` comes from a `.env` file next to `docker-compose.yml`, which is `.gitignore`'d. Commit a `.env.example` with placeholder values so reviewers know what to set.
- **Do not run tests inside Compose.** Tests use Testcontainers (or in-memory fakes) outside the Compose stack; Compose is the deployable artifact, not the test harness.

### Build and run (PowerShell)

```powershell
# From homework-N\src
Copy-Item .env.example .env  # then edit secrets
docker compose up --build -d
docker compose logs -f api   # tail API logs
docker compose down -v       # stop and wipe volumes (clean slate)
```

The `-d` runs detached; drop it for a foreground stack you can `Ctrl+C`. `down -v` wipes the named volumes — useful to reset Postgres state between demo runs.

### What gets committed

- `homework-N/src/docker-compose.yml` — required when external services are used.
- `homework-N/src/.env.example` — placeholder values, no real secrets.
- `homework-N/HOWTORUN.md` — add a "Run via Docker Compose" section with the three commands above and a list of the env vars in `.env`.
- `homework-N/CLAUDE.md` — note that this homework deviates from the in-memory default and explain why (e.g., "TASKS.md Section 3 requires durable storage").

## `.dockerignore` (recommended)

Sits next to the Dockerfile, keeps the build context small:

```
**/bin/
**/obj/
**/.vs/
**/*.user
**/appsettings.*.json
.git
.gitignore
```

`appsettings.Development.json` and friends are excluded so secrets in dev settings never leak into the image. Pass real config via environment variables on `docker run`.

## Gotchas

- **Build context is `src/`**, not `homework-N/`. Running `docker build` from `homework-N/` will not see csprojs at the paths `COPY` expects.
- **Do not `COPY .env`**. Secrets live in environment variables passed at `docker run` time.
- **`USER app` matters.** The `base` stage runs as a non-root user. Don't `chown` files in later stages in a way that breaks that.
- **Tests stay out of the image.** The image is a deployable artifact, not a CI runner.

## What gets committed

- `homework-N/src/Dockerfile` — required.
- `homework-N/src/.dockerignore` — recommended.
- `homework-N/HOWTORUN.md` — add a "Run via Docker" section with the two commands above. Reviewers should not have to read this doc to find them.
