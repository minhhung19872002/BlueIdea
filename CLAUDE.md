# BlueIdea — Project Constitution

## What This Is

**BlueIdea** is a government innovation management platform for Vietnamese local authorities.
Full lifecycle: submission → reception → review → council scoring → recognition → reporting.

All business logic, data, and UI use **Vietnamese with diacritics (Unicode NFC)**.
Workflows, scoring criteria, forms, menus, and roles are **admin-configurable** — changing business rules must not require code changes.

## Source of Truth

| Document | Purpose |
|---|---|
| `docs/Chuong V - Yeu cau ve ky thuat (1).pdf` | Original customer requirement (51 functions) |
| `docs/00-MASTER-SPEC.md` | Normalized technical specification |
| `docs/TRANG-THAI-TRIEN-KHAI.md` | Deployment status per function |
| `docs/KICH-BAN-NGHIEM-THU.md` | Acceptance test scenarios |
| `docs/AN-TOAN-THONG-TIN.md` | Security requirements (Level 2) |
| `docs/ADR/` | Architecture decision records |
| `docs/requirements/traceability.yaml` | Machine-readable requirement traceability |

## Required Reading Before Changes

Before any architectural or functional change, read in this order:
1. This file
2. The relevant `.claude/rules/` file for the area you're changing
3. The requirement in `docs/00-MASTER-SPEC.md`
4. The ADR if one exists for that area
5. The current implementation

## Stack

**Backend**: .NET 8, C# 12, PostgreSQL 16 + pgvector, MediatR (CQRS), FluentValidation, EF Core, Hangfire, SignalR, MinIO
**Frontend**: React 18, TypeScript 5, Vite, Ant Design 5, TanStack Query v5, Zustand, react-hook-form + zod
**AI/OCR**: Tesseract 5 (internal FastAPI container), ONNX Runtime, SimHash/MinHash/TF-IDF (all on-premise)
**Infrastructure**: Docker Compose, Redis 7, Nginx, GitHub Actions CI/CD

## Engineering Rules

1. **Understand before implementing**: Read the requirement, inspect existing implementation, then act.
2. **No duplication**: Check for existing code/patterns before creating new ones.
3. **No unnecessary rewrites**: Preserve working code. Fix what's broken, not what's different from your preference.
4. **Server-side authorization**: Every endpoint and command is permission-checked. Frontend hides but never authorizes.
5. **Configuration over code**: Business rules (workflows, criteria, forms, menus, roles) must be data, not if/else chains.
6. **Test with real infrastructure**: PostgreSQL Testcontainers for backend, real backend for frontend tests.
7. **Audit trail**: All mutations log who, when, IP, before/after values.
8. **Update docs**: Keep `TRANG-THAI-TRIEN-KHAI.md` and traceability current after changes.
9. **Evidence-based verification**: A requirement is VERIFIED only when all applicable layers have runtime evidence.

## Forbidden

- Claim completion without evidence
- Bypass failing tests or delete tests to make CI green
- Weaken validation to satisfy tests
- Hard-code configurable business rules
- Call third-party AI APIs (OpenAI, Gemini, Claude API, Azure AI, AWS Bedrock)
- Commit secrets, credentials, or `.env` files
- Push directly to main without PR
- Reset other developers' work
- Replace production implementation with mocks
- Use ABP Framework
- Use `any` in TypeScript
- Use dynamic eval in rule evaluator

## Definition of Done

A feature is complete when:
- [ ] Backend API works against real PostgreSQL
- [ ] Frontend flow works against real backend
- [ ] No BlueIdea business APIs are mocked
- [ ] CRUD and lifecycle behaviors work
- [ ] Data persists after browser reload
- [ ] Validation verified (positive + negative)
- [ ] Unauthenticated access returns 401
- [ ] Unauthorized access returns 403
- [ ] Organization scope (`don_vi`) is enforced
- [ ] Workflow rules verified (if applicable)
- [ ] Loading, empty, error, success states present
- [ ] Docs updated (traceability, deployment status)

## Mandatory Final Report

Every implementation task concludes with:

```
Requirement(s):
Files changed:
Architecture impact:
Tests added/updated:
Commands executed:
Test results:
Security impact:
Known limitations:
Remaining gaps:
Requirement status:
Evidence:
```

## Detailed Rules

See `.claude/rules/` for domain-specific rules:

| Rule File | Scope |
|---|---|
| `architecture.md` | Solution structure, bounded contexts, data scoping |
| `backend.md` | C# conventions, CQRS, API conventions, naming |
| `frontend.md` | React patterns, feature isolation, component rules |
| `database.md` | PostgreSQL conventions, naming, migrations |
| `security.md` | ATTT Level 2, auth, crypto, input validation |
| `workflow-engine.md` | Dynamic workflow, snapshot rule, condition evaluator |
| `scoring-engine.md` | Dynamic scoring, council evaluation |
| `ai-service.md` | Internal AI constraint, OCR, similarity pipeline |
| `testing.md` | Test strategy, prohibited mocking patterns |
| `git-workflow.md` | Multi-developer safety, branch conventions |
| `requirement-governance.md` | Traceability, verification, evidence model |

## Specialized Agents

See `.claude/agents/` for specialized subagents:

| Agent | Purpose |
|---|---|
| `requirement-auditor` | Audit requirements against implementation (read-only) |
| `solution-architect` | Design implementation plans |
| `backend-engineer` | Implement backend changes |
| `frontend-engineer` | Implement frontend changes |
| `qa-engineer` | Independent verification with real tests |
| `security-reviewer` | Adversarial security review |
| `code-reviewer` | Implementation quality review |
| `workflow-specialist` | Dynamic workflow engine expert |
| `scoring-specialist` | Dynamic scoring engine expert |
| `ai-specialist` | OCR, similarity, internal AI expert |
| `release-auditor` | Production readiness assessment (read-only) |
