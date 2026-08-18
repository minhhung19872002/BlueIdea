# Architecture Rules

## Solution Structure

```
src/
├── BlueIdea.Shared/          # Result<T>, PagedResult<T>, Vietnamese text utilities
├── BlueIdea.Domain/          # Entity, Enum, Value Object, Domain Event, Domain Exception
├── BlueIdea.Workflow/        # Dynamic workflow engine
├── BlueIdea.Scoring/         # Dynamic scoring engine
├── BlueIdea.Ai/              # SimHash, MinHash/LSH, TF-IDF, embedding, similarity pipeline
├── BlueIdea.Reporting/       # Excel (ClosedXML), PDF (QuestPDF), Word (OpenXml)
├── BlueIdea.Application/     # Command/Query handlers, validators, DTOs, interfaces
├── BlueIdea.Infrastructure/  # EF Core, Argon2id, JWT, AES-GCM, file storage, seed
└── BlueIdea.Api/             # Controllers, middleware, Swagger, SignalR Hub
```

## Core Principles

- **Clean Architecture + CQRS**: Domain has no infrastructure dependency. Application orchestrates through MediatR.
- **Configuration-driven business behavior**: Workflow, scoring criteria, application components, forms, menus, and roles are all configurable via admin UI — changing business rules must not require code changes.
- **No hard-coded workflow logic**: Never write `if status == X then next = Y` for configurable behavior. Use the workflow engine.
- **Domain boundaries**: Each bounded context (`DanhMuc`, `ToChuc`, `QuyTrinh`, `TieuChi`, `HoiDong`, `SangKien`, `DanhGia`, `QuyetDinh`, `BaoCao`, `TichHop`, `Ai`) is isolated.
- **No cross-module coupling**: Modules communicate through domain events or application-layer orchestration, not direct entity references across aggregates.
- **Use existing abstractions first**: Before creating a new service or pattern, check if one already exists in the codebase.
- **Idempotency**: External-facing operations use `Idempotency-Key` header + optimistic concurrency to prevent double-submit.
- **Audit all business state changes**: Every mutation records who, when, from which IP, and values before/after.
- **Transactions for atomic state changes**: Use EF Core transactions, never partial commits for multi-step business operations.
- **No second architecture**: Do not introduce a parallel architecture for an existing concern (e.g., don't add a second ORM, a second state management library, or a second workflow engine).

## Data Scoping — Critical

- **Organization Unit pattern**: Every entity has `don_vi_id`.
- User can only view/edit data belonging to their organization unit (global query filter by `don_vi`).
- Permission check MUST be server-side — never trust client-side.
- MediatR pipeline checks permissions on every Command/Query via `HanhViPhanQuyen`.
- Data scope types: `TOAN_HE_THONG`, `DON_VI`, `DON_VI_VA_CAP_DUOI`, `CA_NHAN`, `TUY_CHINH`.
