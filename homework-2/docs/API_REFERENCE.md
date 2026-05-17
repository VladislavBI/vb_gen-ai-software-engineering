# API Reference: Support Ticket Management System

**Base URL**: `http://localhost:5000`

**API Version**: 1.0

---

## Endpoints Overview

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/tickets` | Create a new ticket |
| `GET` | `/tickets` | List all tickets (with optional filtering) |
| `GET` | `/tickets/{id}` | Retrieve a specific ticket |
| `PUT` | `/tickets/{id}` | Update an existing ticket |
| `DELETE` | `/tickets/{id}` | Delete a ticket |
| `POST` | `/tickets/import` | Batch import tickets (CSV/JSON/XML) |
| `POST` | `/tickets/{id}/auto-classify` | Auto-classify a ticket |

---

## 1. Create Ticket

**Endpoint**: `POST /tickets`

**Description**: Create a new support ticket.

### Request

#### Headers
```
Content-Type: application/json
```

#### Body

```json
{
  "customerId": "string (required, max 50 chars)",
  "customerEmail": "string (required, valid email)",
  "customerName": "string (required, max 100 chars)",
  "subject": "string (required, max 200 chars)",
  "description": "string (required, max 5000 chars)",
  "category": "string (optional, enum: account_access, technical_issue, billing_question, feature_request, bug_report, other)",
  "priority": "string (optional, enum: low, medium, high, urgent)",
  "tags": ["string"] (optional, array of strings)
}
```

#### Example Request (PowerShell)

```powershell
$uri = "http://localhost:5000/tickets"
$body = @{
    customerId = "CUST-001"
    customerEmail = "john@example.com"
    customerName = "John Doe"
    subject = "Unable to access billing portal"
    description = "I cannot log in to my billing portal. I get an error message saying 'Invalid credentials' even though I'm using the correct password."
    category = "billing_question"
    priority = "high"
    tags = @("billing", "urgent")
} | ConvertTo-Json

Invoke-RestMethod -Uri $uri -Method POST `
    -ContentType "application/json" `
    -Body $body
```

### Response

#### Success (201 Created)

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "customerId": "CUST-001",
  "customerEmail": "john@example.com",
  "customerName": "John Doe",
  "subject": "Unable to access billing portal",
  "description": "I cannot log in to my billing portal...",
  "category": "billing_question",
  "priority": "high",
  "status": "new",
  "createdAt": "2026-05-17T14:30:00.000Z",
  "updatedAt": "2026-05-17T14:30:00.000Z",
  "resolvedAt": null,
  "assignedTo": null,
  "tags": ["billing", "urgent"]
}
```

#### Failure (400 Bad Request)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "CustomerEmail": ["'John Doe' is not a valid email address."],
    "Subject": ["The length of 'Subject' must be at most 200 characters."]
  }
}
```

---

## 2. List All Tickets

**Endpoint**: `GET /tickets`

**Description**: Retrieve all tickets with optional filtering.

### Request

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `category` | string | No | Filter by category (e.g., `billing_question`, `bug_report`) |
| `priority` | string | No | Filter by priority (e.g., `high`, `urgent`) |
| `status` | string | No | Filter by status (e.g., `new`, `in_progress`, `resolved`) |

#### Example Requests (PowerShell)

**Get all tickets:**
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:5000/tickets" -Method GET
$response | Format-Table -Property Id, Subject, Priority, Status
```

**Filter by priority:**
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:5000/tickets?priority=high" -Method GET
Write-Host "High-priority tickets: $($response.Count)"
```

**Filter by category:**
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:5000/tickets?category=bug_report" -Method GET
```

**Filter by status:**
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:5000/tickets?status=in_progress" -Method GET
```

**Combine multiple filters:**
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:5000/tickets?priority=urgent&status=new" -Method GET
```

### Response

#### Success (200 OK)

```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "customerId": "CUST-001",
    "customerEmail": "john@example.com",
    "customerName": "John Doe",
    "subject": "Unable to access billing portal",
    "description": "I cannot log in to my billing portal...",
    "category": "billing_question",
    "priority": "high",
    "status": "new",
    "createdAt": "2026-05-17T14:30:00.000Z",
    "updatedAt": "2026-05-17T14:30:00.000Z",
    "resolvedAt": null,
    "assignedTo": null,
    "tags": ["billing", "urgent"]
  },
  {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "customerId": "CUST-002",
    "customerEmail": "jane@example.com",
    "customerName": "Jane Smith",
    "subject": "Feature request: Dark mode",
    "description": "Would like to see a dark mode option in the UI...",
    "category": "feature_request",
    "priority": "low",
    "status": "new",
    "createdAt": "2026-05-17T15:00:00.000Z",
    "updatedAt": "2026-05-17T15:00:00.000Z",
    "resolvedAt": null,
    "assignedTo": null,
    "tags": ["ui", "feature"]
  }
]
```

---

## 3. Get Ticket by ID

**Endpoint**: `GET /tickets/{id}`

**Description**: Retrieve a specific ticket by its ID.

### Request

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | UUID | Yes | The ticket ID |

#### Example Request (PowerShell)

```powershell
$ticketId = "550e8400-e29b-41d4-a716-446655440000"
$response = Invoke-RestMethod -Uri "http://localhost:5000/tickets/$ticketId" -Method GET
$response | ConvertTo-Json
```

### Response

#### Success (200 OK)

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "customerId": "CUST-001",
  "customerEmail": "john@example.com",
  "customerName": "John Doe",
  "subject": "Unable to access billing portal",
  "description": "I cannot log in to my billing portal...",
  "category": "billing_question",
  "priority": "high",
  "status": "new",
  "createdAt": "2026-05-17T14:30:00.000Z",
  "updatedAt": "2026-05-17T14:30:00.000Z",
  "resolvedAt": null,
  "assignedTo": null,
  "tags": ["billing", "urgent"]
}
```

#### Failure (404 Not Found)

```
HTTP/1.1 404 Not Found
Content-Length: 0
```

---

## 4. Update Ticket

**Endpoint**: `PUT /tickets/{id}`

**Description**: Update an existing ticket (partial updates supported).

### Request

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | UUID | Yes | The ticket ID |

#### Headers
```
Content-Type: application/json
```

#### Body (all fields optional)

```json
{
  "subject": "string (optional, max 200 chars)",
  "description": "string (optional, max 5000 chars)",
  "category": "string (optional, enum: account_access, technical_issue, billing_question, feature_request, bug_report, other)",
  "priority": "string (optional, enum: low, medium, high, urgent)",
  "status": "string (optional, enum: new, in_progress, waiting_customer, resolved, closed)",
  "assignedTo": "string (optional, staff member ID)",
  "tags": ["string"] (optional, array of strings)
}
```

#### Example Request (PowerShell)

```powershell
$ticketId = "550e8400-e29b-41d4-a716-446655440000"
$uri = "http://localhost:5000/tickets/$ticketId"
$body = @{
    status = "in_progress"
    assignedTo = "support-agent-42"
    tags = @("billing", "urgent", "assigned")
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri $uri -Method PUT `
    -ContentType "application/json" `
    -Body $body
```

### Response

#### Success (200 OK)

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "customerId": "CUST-001",
  "customerEmail": "john@example.com",
  "customerName": "John Doe",
  "subject": "Unable to access billing portal",
  "description": "I cannot log in to my billing portal...",
  "category": "billing_question",
  "priority": "high",
  "status": "in_progress",
  "createdAt": "2026-05-17T14:30:00.000Z",
  "updatedAt": "2026-05-17T14:45:00.000Z",
  "resolvedAt": null,
  "assignedTo": "support-agent-42",
  "tags": ["billing", "urgent", "assigned"]
}
```

#### Failure - Not Found (404)

```
HTTP/1.1 404 Not Found
```

#### Failure - Validation Error (400)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Priority": ["Invalid priority value"]
  }
}
```

---

## 5. Delete Ticket

**Endpoint**: `DELETE /tickets/{id}`

**Description**: Delete a ticket permanently.

### Request

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | UUID | Yes | The ticket ID |

#### Example Request (PowerShell)

```powershell
$ticketId = "550e8400-e29b-41d4-a716-446655440000"
$uri = "http://localhost:5000/tickets/$ticketId"

Invoke-RestMethod -Uri $uri -Method DELETE
Write-Host "Ticket deleted successfully"
```

### Response

#### Success (204 No Content)

```
HTTP/1.1 204 No Content
```

#### Failure (404 Not Found)

```
HTTP/1.1 404 Not Found
```

---

## 6. Batch Import Tickets

**Endpoint**: `POST /tickets/import`

**Description**: Import multiple tickets from a file (CSV, JSON, or XML format).

### Request

#### Content-Type
```
multipart/form-data
```

#### Form Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `file` | file | Yes | File containing tickets (CSV, JSON, or XML) |

**File Format Requirements:**

**CSV**: Comma-separated values with header row
```csv
CustomerId,CustomerEmail,CustomerName,Subject,Description,Category,Priority,Tags
CUST-001,john@example.com,John Doe,Subject 1,Description 1,billing_question,high,"tag1,tag2"
```

**JSON**: Array of ticket objects
```json
[
  {
    "customerId": "CUST-001",
    "customerEmail": "john@example.com",
    "customerName": "John Doe",
    "subject": "Subject 1",
    "description": "Description 1",
    "category": "billing_question",
    "priority": "high"
  }
]
```

**XML**: Root element `<tickets>` with `<ticket>` children
```xml
<?xml version="1.0" encoding="utf-8"?>
<tickets>
  <ticket>
    <customerId>CUST-001</customerId>
    <customerEmail>john@example.com</customerEmail>
    <customerName>John Doe</customerName>
    <subject>Subject 1</subject>
    <description>Description 1</description>
    <category>billing_question</category>
    <priority>high</priority>
  </ticket>
</tickets>
```

#### Example Request (PowerShell)

```powershell
$uri = "http://localhost:5000/tickets/import"
$filePath = "demo\sample_tickets.csv"

$form = @{
    file = Get-Item -Path $filePath
}

$response = Invoke-RestMethod -Uri $uri -Method POST `
    -Form $form

Write-Host "Imported $($response.successful) tickets"
Write-Host "Failed: $($response.failed)"
if ($response.errors.Count -gt 0) {
    Write-Host "Errors:"
    $response.errors | ForEach-Object { Write-Host "  - $_" }
}
```

### Response

#### Success (200 OK)

```json
{
  "successful": 50,
  "failed": 0,
  "errors": []
}
```

#### With Errors (200 OK - partial success)

```json
{
  "successful": 48,
  "failed": 2,
  "errors": [
    "Row 5: Invalid email format for 'john@invalid'",
    "Row 12: Missing required field 'Subject'"
  ]
}
```

#### Failure - No File (400)

```json
{
  "error": "No file provided"
}
```

#### Failure - Invalid Format (400)

```json
{
  "error": "Failed to parse file: Unexpected token '}' on line 5, position 12"
}
```

---

## 7. Auto-Classify Ticket

**Endpoint**: `POST /tickets/{id}/auto-classify`

**Description**: Automatically classify a ticket based on its subject and description using keyword matching.

### Request

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `id` | UUID | Yes | The ticket ID |

#### Example Request (PowerShell)

```powershell
$ticketId = "550e8400-e29b-41d4-a716-446655440000"
$uri = "http://localhost:5000/tickets/$ticketId/auto-classify"

$response = Invoke-RestMethod -Uri $uri -Method POST

Write-Host "Suggested Category: $($response.category)"
Write-Host "Suggested Priority: $($response.priority)"
Write-Host "Confidence: $($response.confidence)"
Write-Host "Reasoning: $($response.reasoning)"
Write-Host "Keywords Found: $($response.keywordsFound -join ', ')"
```

### Response

#### Success (200 OK)

```json
{
  "category": "billing_question",
  "priority": "high",
  "confidence": 0.92,
  "reasoning": "Detected billing-related keywords: 'billing', 'payment', 'invoice'",
  "keywordsFound": ["billing", "payment", "invoice"]
}
```

**Category Values**: `account_access`, `technical_issue`, `billing_question`, `feature_request`, `bug_report`, `other`

**Priority Values**: `low`, `medium`, `high`, `urgent`

**Confidence Range**: 0.0 to 1.0 (higher = more confident in classification)

#### Failure - Not Found (404)

```
HTTP/1.1 404 Not Found
```

---

## Error Responses

### Validation Error (400)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "CustomerEmail": ["'john' is not a valid email address."]
  }
}
```

### Not Found (404)

```
HTTP/1.1 404 Not Found
Content-Length: 0
```

### Server Error (500)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "An error occurred while processing your request.",
  "status": 500
}
```

---

## Enumerations

### Category
- `account_access`
- `technical_issue`
- `billing_question`
- `feature_request`
- `bug_report`
- `other`

### Priority
- `low`
- `medium`
- `high`
- `urgent`

### Status
- `new`
- `in_progress`
- `waiting_customer`
- `resolved`
- `closed`

---

## Authentication & Security

This API does not currently implement authentication. In a production system, implement:

- OAuth 2.0 or JWT token-based authentication
- Rate limiting to prevent abuse
- HTTPS encryption for all endpoints
- Request validation and sanitization
- CORS policy for cross-origin requests

---

## Rate Limiting

Not currently implemented. Recommended for production:

- 1000 requests per minute per IP
- 100 import requests per day
- 10MB maximum file size for imports

---

## Pagination

Not currently implemented. For large result sets, consider adding:

```
GET /tickets?page=1&pageSize=50
```

Response would include:
```json
{
  "data": [...],
  "page": 1,
  "pageSize": 50,
  "total": 1250,
  "totalPages": 25
}
```

---

## Further Reading

- See `README.md` for project overview
- See `ARCHITECTURE.md` for system design
- See `TESTING_GUIDE.md` for test scenarios
- See `HOWTORUN.md` for step-by-step instructions
