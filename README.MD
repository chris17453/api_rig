# PostmanClone — AI Session Guide

## What We're Building

A cross-platform Postman clone. Import Postman collections, execute requests, manage environments, run pre/post scripts with assertions, track history.

## Team

- Amlan
- Ankur
- Juan
- Edwar

## Time

6 hours, 4 people = 24 person-hours

---

## Tech Stack

- .NET 10
- Avalonia 11 (Fluent theme) — cross-platform UI
- EF Core + SQLite — persistence
- Jint — JavaScript scripting engine
- xUnit — testing
- Newtonsoft.Json — Postman JSON parsing

---

## Project Structure

| Project | Purpose |
|---------|---------|
| PostmanClone.Core | Shared models and interfaces only |
| PostmanClone.Http | Request execution, auth handlers |
| PostmanClone.Data | SQLite repos, Postman collection parser, environment store |
| PostmanClone.Scripting | Jint engine, pm.* API |
| PostmanClone.App | Avalonia UI |

Each project has a matching test project.

---

## Features

### Must Have

- Import Postman collections (v2.0 and v2.1)
- Execute HTTP requests (all methods)
- Request body types: none, raw, JSON, form-data, x-www-form-urlencoded
- Auth: Basic, Bearer, API Key, OAuth2 Client Credentials
- Environment variables with substitution ({{variable}} syntax)
- History (persisted to SQLite)
- Pre-request scripts
- Post-response scripts with pm.test() and pm.expect() assertions
- Export collections

### Nice to Have

- Tabs for multiple requests
- Collection folders
- Search/filter history
- Syntax highlighting

---

## Isolation Rules

1. Define interfaces in Core first — lock them before implementation starts
2. Each person owns one project — no cross-edits
3. UI developer works against interfaces using mocks until real implementations merge
4. One branch per project, merge Core first

---

## Merge Order

1. Core (models + interfaces)
2. Data
3. Http
4. Scripting
5. UI (iterative)

---

## Coding Standards

- snake_case for all identifiers
- Async suffix: `_async`
- CancellationToken on all async methods
- Minimal comments — only for non-obvious logic
- MIT license

---

## Testing

- TDD — write tests first
- xUnit + FluentAssertions
- Test naming: `method_returns_expected_when_condition`
- Each feature needs unit tests before PR

---

## Tickets

### Core

- CORE-001: Solution scaffold
- CORE-002: Request/response models
- CORE-003: Collection models
- CORE-004: Auth models
- CORE-005: History models
- CORE-006: Environment models
- CORE-007: Script context/result models
- CORE-008: Interface definitions

### Http

- HTTP-001: Request executor
- HTTP-002: Basic auth
- HTTP-003: Bearer auth
- HTTP-004: API Key auth
- HTTP-005: OAuth2 Client Credentials
- HTTP-006: Response processing
- HTTP-007: Timeout and cancellation

### Data

- DATA-001: SQLite schema and DbContext
- DATA-002: History repository
- DATA-003: Collection repository
- DATA-004: Environment store with variable resolution
- DATA-005: Postman v2.0 parser
- DATA-006: Postman v2.1 parser
- DATA-007: Collection export

### Scripting

- SCRIPT-001: Jint engine setup with sandbox
- SCRIPT-002: pm.test() implementation
- SCRIPT-003: pm.expect() assertions
- SCRIPT-004: pm.response object
- SCRIPT-005: pm.request object
- SCRIPT-006: pm.environment get/set
- SCRIPT-007: Pre-request script runner
- SCRIPT-008: Post-response script runner

### UI

- UI-001: App shell and navigation
- UI-002: Request builder panel
- UI-003: Response viewer panel
- UI-004: Collection tree view
- UI-005: History list
- UI-006: Environment selector
- UI-007: Script editor panels
- UI-008: Test results display
- UI-009: Import/export dialogs
- UI-010: Wire up DI and real services

### Integration

- INT-001: End-to-end request flow
- INT-002: Collection import to execution
- INT-003: Environment variable substitution
- INT-004: Full script execution flow

---

## Hour-by-Hour

| Hour | Focus |
|------|-------|
| 0–1 | Setup, agree on interfaces, scaffold |
| 1–3 | TDD + implementation (parallel) |
| 3–4 | Integration, wire UI to real services |
| 4–5 | Polish, edge cases, error handling |
| 5–6 | Testing, bug fixes, demo prep |

---

## How to Build and Run

### Prerequisites

- .NET SDK 10.x installed
- Git

### Verify Installation

```bash
dotnet --version  # Should return 10.*
```

### Clone and Build

```bash
git clone <repository-url>
cd api_rig
dotnet restore
dotnet build -c Release
```

### Run Tests

```bash
dotnet test -c Release
```

### Run All Tests with Verbose Output

```bash
dotnet test -c Release --logger "console;verbosity=detailed"
```

### Project-Specific Tests

```bash
# Core tests only
dotnet test tests/PostmanClone.Core.Tests -c Release

# Scripting tests only
dotnet test tests/PostmanClone.Scripting.Tests -c Release
```

---

## Current Implementation Status

**For detailed feature comparison with Postman, see:**
- **[GAP_ANALYSIS.md](GAP_ANALYSIS.md)** - Comprehensive 18-section analysis (911 lines)
- **[GAP_ANALYSIS_SUMMARY.md](GAP_ANALYSIS_SUMMARY.md)** - Quick reference with visual comparison
- **[STATUS.md](STATUS.md)** - Detailed implementation status report

### Completed Modules

| Module | Status | Tests |
|--------|--------|-------|
| PostmanClone.Core | ✅ Complete | 30 tests passing |
| PostmanClone.Data | ✅ Complete | 139 tests passing |
| PostmanClone.Http | ✅ Complete | 33 tests passing |
| PostmanClone.Scripting | ✅ Complete (Phase 1 + 2 Integration) | 41 tests passing |

**Total: 243 tests passing**

### Pending Modules

| Module | Status |
|--------|--------|
| PostmanClone.App | 🔄 Pending (UI layer) |

### Integration Components

| Component | Location | Description |
|-----------|----------|-------------|
| `request_orchestrator` | `PostmanClone.Data/Services` | Orchestrates full request flow with scripts |
| `variable_resolver` | `PostmanClone.Data/Services` | Resolves `{{variable}}` tokens in requests |

---

## Scripting Module API

The Scripting module provides a Postman-compatible `pm.*` API for pre-request and post-response scripts.

### Available APIs

```javascript
// Test assertions
pm.test("test name", function() {
    // assertions here
});

// Expectations
pm.expect(value).to.equal(expected);
pm.expect(value).to.be.ok();
pm.expect(value).to.be.true();
pm.expect(value).to.be.false();
pm.expect(value).to.be.null();
pm.expect(string).to.include(substring);
pm.expect(number).to.be.above(n);
pm.expect(number).to.be.below(n);
pm.expect(string).to.have.length(n);
pm.expect(value).not.to.equal(other);

// Environment variables
var value = pm.environment.get("key");
pm.environment.set("key", "value");
pm.environment.has("key");

// Request info (read-only)
pm.request.url;
pm.request.method;
pm.request.headers;
pm.request.body;

// Response info (post-response only)
pm.response.code;
pm.response.status;
pm.response.responseTime;
pm.response.text();
pm.response.json();
pm.response.headers;

// Console logging
console.log("message");
```

### Sandbox Constraints

- **Timeout**: 5 seconds default (configurable)
- **Statement limit**: 100,000 statements max
- **Recursion limit**: 100 levels max
- **CLR access**: Restricted (no System.* access)

---

## Data Module

The Data module handles persistence and Postman collection parsing.

### Components

| Component | Description |
|-----------|-------------|
| `postman_clone_db_context` | EF Core DbContext for SQLite |
| `environment_store` | CRUD operations for environments and variables |
| `history_repository` | Request/response history persistence |
| `collection_repository` | Collection storage and retrieval |
| `postman_collection_parser` | Parses Postman v2.0/v2.1 JSON files |
| `variable_resolver` | Resolves `{{variable}}` tokens in requests |
| `request_orchestrator` | Orchestrates full request flow with scripts |

### Database Schema

```
environments
├── id (PK)
├── name
├── is_active
├── created_at
└── updated_at

environment_variables
├── id (PK)
├── environment_id (FK)
├── key
└── value

collections
├── id (PK)
├── name
├── description
├── version
├── auth_json
├── variables_json
└── created_at

collection_items
├── id (PK)
├── collection_id (FK)
├── parent_item_id (FK, nullable)
├── name
├── is_folder
├── request_method
├── request_url
├── request_headers_json
├── request_body_json
├── pre_request_script
├── post_response_script
└── sort_order

history_entries
├── id (PK)
├── request_name
├── method
├── url
├── status_code
├── status_description
├── elapsed_ms
├── request_snapshot_json
├── response_snapshot_json
├── executed_at
└── environment_id
```

### Usage Example

```csharp
// Environment store
var env_store = services.GetRequiredService<i_environment_store>();
var env = await env_store.get_active_async(cancellation_token);
await env_store.set_variable_async("token", "abc123", cancellation_token);

// Variable resolver
var resolver = new variable_resolver();
var resolved_url = resolver.resolve("{{base_url}}/users", variables);
```

---

## Http Module

The Http module handles HTTP request execution with authentication support.

### Components

| Component | Description |
|-----------|-------------|
| `http_request_executor` | Executes HTTP requests via HttpClient |
| `basic_auth_handler` | HTTP Basic authentication |
| `bearer_auth_handler` | Bearer token authentication |
| `api_key_auth_handler` | API Key authentication (header/query) |

### Supported Features

- **HTTP Methods**: GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS
- **Body Types**: None, Raw, JSON, Form-Data, URL-Encoded
- **Authentication**: Basic, Bearer, API Key
- **Timeout**: Configurable per-request timeout
- **Cancellation**: Full CancellationToken support

### Usage Example

```csharp
var executor = services.GetRequiredService<i_request_executor>();
var request = new http_request_model
{
    name = "Get Users",
    method = http_method.get,
    url = "https://api.example.com/users",
    auth = new auth_model
    {
        auth_type = auth_type.bearer,
        bearer_token = "your-token"
    }
};
var response = await executor.execute_async(request, cancellation_token);
```

---

## Scripting Module

The Scripting module provides Postman-compatible JavaScript execution using Jint.

### Components

| Component | Description |
|-----------|-------------|
| `script_runner` | Executes pre-request and post-response scripts |
| `jint_engine_factory` | Creates sandboxed Jint engine instances |
| `pm_api` | Main `pm.*` API exposed to scripts |
| `pm_expect` | Chai-style assertion library |
| `pm_environment_api` | Environment variable access |
| `pm_request_api` | Read-only request information |
| `pm_response_api` | Read-only response information |

### pm.* API Reference

```javascript
// Testing
pm.test("test name", function() {
    // assertions
});

// Assertions (Chai-style)
pm.expect(value).to.equal(expected);
pm.expect(value).to.eql(expected);        // deep equal
pm.expect(value).to.be.ok();              // truthy
pm.expect(value).to.be.true();
pm.expect(value).to.be.false();
pm.expect(value).to.be.null();
pm.expect(value).to.be.undefined();
pm.expect(value).to.be.empty();
pm.expect(value).to.be.a("string");       // type check
pm.expect(value).to.be.an("object");
pm.expect(string).to.include(substring);
pm.expect(string).to.contain(substring);
pm.expect(number).to.be.above(n);
pm.expect(number).to.be.below(n);
pm.expect(number).to.be.at.least(n);
pm.expect(number).to.be.at.most(n);
pm.expect(value).to.have.length(n);
pm.expect(value).to.have.property("name");
pm.expect(value).to.be.oneOf([a, b, c]);
pm.expect(value).not.to.equal(other);     // negation

// Environment
pm.environment.get("key");
pm.environment.set("key", "value");
pm.environment.has("key");
pm.environment.unset("key");
pm.environment.clear();
pm.environment.toObject();

// Request (read-only)
pm.request.url;
pm.request.method;
pm.request.headers;
pm.request.body;

// Response (post-response only)
pm.response.code;
pm.response.status;
pm.response.responseTime;
pm.response.responseSize;
pm.response.text();
pm.response.json();
pm.response.headers;
pm.response.to.have.status(200);
pm.response.to.be.ok();
pm.response.to.have.header("Content-Type");
pm.response.to.have.body("text");

// Console
console.log("message", arg1, arg2);
console.info("info");
console.warn("warning");
console.error("error");
```

### Sandbox Constraints

| Constraint | Value | Purpose |
|------------|-------|---------|
| Timeout | 5000ms (default) | Prevent infinite loops |
| Max Statements | 100,000 | CPU exhaustion protection |
| Max Recursion | 100 levels | Stack overflow protection |
| CLR Access | Restricted | Security (no System.* access) |

---

## Playground

The Playground is a console application for testing the full integration without UI.

### Location

`src/PostmanClone.Playground/`

### Run the Playground

```bash
dotnet run --project src/PostmanClone.Playground/PostmanClone.Playground.csproj -c Release
```

### What It Does

1. **Sets up DI container** with all services
2. **Creates SQLite database** (`playground.db`)
3. **Creates test environment** with variables (`base_url`, `user_id`)
4. **Defines a request** with pre-request and post-response scripts
5. **Executes the full flow**:
   - Pre-request script runs
   - Variables are resolved (`{{base_url}}` → actual URL)
   - HTTP request executes
   - Post-response script runs with assertions
6. **Displays results**: status, logs, test results, errors

### Example Output

```
🚀 Starting PostmanClone Playground...
✅ Database initialized.
✅ Environment 'Dev Environment' created and activated.

📡 Executing Request Orchestrator...
   Original URL: {{base_url}}/todos/{{user_id}}

📊 EXECUTION RESULTS
====================

🔹 Final Request:
   Status: 200 OK
   Time: 372ms
   Body Size: 83 bytes

📝 Console Logs:
   🔄 [Pre-Request] Starting script...
      Current ID: 1
   ✅ [Pre-Request] Completed.
   🔄 [Post-Response] Starting validations...
      Title received: delectus aut autem
   ✅ [Post-Response] Completed.

🧪 Test Results:
   ✅ Status code is 200
   ✅ Response time is acceptable
   ✅ Title exists and is a string

🏁 Test completed.
```

---

## Request Orchestrator Flow

The `request_orchestrator` integrates all modules for the complete request lifecycle:

```
┌─────────────────────────────────────────────────────────────┐
│                   request_orchestrator                       │
├─────────────────────────────────────────────────────────────┤
│ 1. Get active environment (i_environment_store)            │
│ 2. Execute pre-request script (i_script_runner)            │
│ 3. Apply environment_updates to store                       │
│ 4. Resolve variables in request (i_variable_resolver)       │
│ 5. Execute HTTP request (i_request_executor)                │
│ 6. Execute post-response script (i_script_runner)           │
│ 7. Return aggregated result with tests/logs/errors          │
└─────────────────────────────────────────────────────────────┘
```

### Usage

```csharp
var orchestrator = services.GetRequiredService<request_orchestrator>();
var result = await orchestrator.execute_request_async(request, cancellation_token);

// Access results
Console.WriteLine($"Status: {result.response?.status_code}");
Console.WriteLine($"Tests passed: {result.all_test_results.Count(t => t.passed)}");
Console.WriteLine($"Errors: {result.all_errors.Count}");
```

---

## Notes

- Postman JSON has quirks — use Newtonsoft, not System.Text.Json
- Jint needs execution timeouts to prevent infinite loops
- Single HttpClient instance for app lifetime
- UI updates from background threads need dispatcher
