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

## Notes

- Postman JSON has quirks — use Newtonsoft, not System.Text.Json
- Jint needs execution timeouts to prevent infinite loops
- Single HttpClient instance for app lifetime
- UI updates from background threads need dispatcher
