# How to Run the Banking Transactions API

## Prerequisites

- **.NET 10 SDK** or later installed on your system
- **PowerShell 5.1** or later (for running the scripts)
- Port **5080** available (configured in the application)

## Quick Start

### 1. Navigate to the project directory

```powershell
cd homework-1/src
```

### 2. Build the solution

```powershell
dotnet build Homework1.sln
```

### 3. Run the API

```powershell
dotnet run --project Homework1.Api --urls http://localhost:5080
```

The application will start and listen on `http://localhost:5080`.

### 4. Verify the API is running

Open another PowerShell window and test the health endpoint:

```powershell
$health = Invoke-RestMethod -Uri http://localhost:5080/health -Method Get
$health
```

Expected response: `{"status":"ok"}`

## Running Tests

To execute the complete test suite:

```powershell
cd homework-1/src
dotnet test Homework1.sln --verbosity normal
```

Tests use xUnit, FluentAssertions, and Moq for unit and integration testing.

## Using the Demo Scripts

Sample API requests and data are available in the `demo/` directory:

- **demo/sample-requests.http** — Ready-to-use HTTP requests for testing
- **demo/sample-data.json** — Sample transaction data

### Testing with sample-requests.http

If using VS Code with the REST Client extension:

1. Open `demo/sample-requests.http` in VS Code
2. Click "Send Request" above each request to test the API

Alternatively, use `Invoke-RestMethod` from PowerShell:

```powershell
$body = @{
    fromAccount = "ACC-12345"
    toAccount = "ACC-67890"
    amount = 100.50
    currency = "USD"
    type = "transfer"
} | ConvertTo-Json

Invoke-RestMethod -Uri http://localhost:5080/transactions `
  -Method Post `
  -ContentType 'application/json' `
  -Body $body
```

## API Endpoints

### Core Endpoints (Task 1)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/transactions` | Create a new transaction |
| `GET` | `/transactions` | List all transactions |
| `GET` | `/transactions/:id` | Get a specific transaction by ID |
| `GET` | `/accounts/:accountId/balance` | Get account balance |

### Task 3: Filters

The `GET /transactions` endpoint supports filtering:

- `?accountId=ACC-12345` — Filter by account
- `?type=transfer` — Filter by transaction type
- `?from=2024-01-01&to=2024-01-31` — Filter by date range
- Combine multiple filters: `?accountId=ACC-12345&type=deposit`

### Task 4: Additional Features

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/accounts/:accountId/summary` | Account summary (total deposits, withdrawals, count, most recent timestamp) |

**Rate Limiting:** The API enforces a limit of 100 requests per minute per IP address. Exceeding this limit returns HTTP 429 (Too Many Requests).

## Validation Rules

- **Amount:** Must be positive with at most 2 decimal places
- **Account Format:** Must match `ACC-XXXXX` (where X is alphanumeric, e.g., `ACC-12345`)
- **Currency:** Must be a valid ISO 4217 code (USD, EUR, GBP, JPY, etc.)

Invalid requests return HTTP 400 with detailed error messages.

## Project Structure

```
homework-1/
├── src/
│   ├── Homework1.Api/          # REST endpoints and middleware
│   ├── Homework1.Bll/          # Business logic layer
│   ├── Homework1.Dal/          # Data access layer (in-memory storage)
│   ├── Homework1.Tests/        # Unit and integration tests
│   └── Homework1.sln
├── docs/
│   └── screenshots/            # Evidence screenshots
├── demo/
│   ├── run.bat                 # Windows startup script
│   ├── sample-requests.http    # Sample HTTP requests
│   └── sample-data.json        # Sample transaction data
├── README.md                   # Project overview
├── HOWTORUN.md                 # This file
├── PLAN.md                     # Development plan and milestones
└── TASKS.md                    # Assignment specification
```

## Troubleshooting

**Port 5080 is already in use:**
```powershell
# Stop the existing process and try again
netstat -ano | findstr :5080
taskkill /PID <PID> /F
```

**Build fails with missing dependencies:**
```powershell
dotnet restore Homework1.sln
dotnet build Homework1.sln
```

**Tests fail to run:**
```powershell
dotnet clean Homework1.sln
dotnet build Homework1.sln
dotnet test Homework1.sln --no-build
```

For additional help, check `PLAN.md` for detailed milestone descriptions and verify scripts.
