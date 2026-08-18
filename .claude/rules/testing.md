# Testing Rules

## Default Test Types

**Backend (default)**: Real API integration tests with actual ASP.NET Core pipeline, JWT auth, MediatR pipeline, EF Core, and **real PostgreSQL** (Testcontainers). Do NOT create unit test suites unless explicitly requested.

**Frontend acceptance (default)**: Real React app against real backend and real PostgreSQL, tested via Playwright without API interception. Do NOT create mocked frontend tests unless explicitly requested.

## Backend Tests Must Verify

- HTTP status
- Response contract (`thanhCong`, `duLieu`, `maLoi`)
- Database persistence
- FluentValidation
- Functional permissions (MediatR pipeline)
- Organization scope (`don_vi_id` filtering)
- Workflow transitions (where applicable)
- Duplicate prevention
- Audit log side effects
- Optimistic concurrency
- Follow-up retrieval via separate request

## Prohibited in Tests

- Mock DbContext, repositories, MediatR handlers
- Mock authorization pipeline or current-user context
- Mock organization-scope filter
- Use EF Core InMemory as acceptance evidence
- Use `page.route()`, `route.fulfill()`, MSW for BlueIdea APIs in acceptance tests
- Fake localStorage authentication, permissions, or organization context
- Delete or skip valid failing tests to make CI green

## Test Coverage Focus

Tests must include applicable:
- Happy path
- Negative cases (validation failures)
- Boundary cases
- Authorization cases (unauthenticated, unauthorized, wrong organization)
- Regression cases

## Efficient Execution

- Do NOT print complete successful logs — redirect verbose output to files.
- On success: report only command, duration, test count, exit code.
- On failure: search for `error`, `failed`, `exception`, failing test name — read only relevant lines.
- Run affected feature tests first, dependent regression only per impact map.
- Reuse a healthy running stack — do NOT restart containers unless required.
