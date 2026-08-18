# Workflow Engine Rules

Dynamic workflow is a critical project area. The workflow engine must remain configuration-driven.

## Architecture

- `BlueIdea.Workflow/` contains the engine: `IBoMayQuyTrinh`, `BoDanhGiaDieuKien`, `TinhHanXuLy`, `KiemTraQuyTrinh`.
- `BlueIdea.Application/XuLy/` contains `DichVuWorkflow` and `ThucThiBuocCommand`.

## Snapshot Rule (ADR 0002)

**Critical**: Applications run on the workflow **snapshot** captured at submission time, NOT the current workflow definition.

- When an application is submitted, the full workflow config (steps, actors, transitions, statuses, components, features) is serialized to `sang_kien.quy_trinh_snapshot`.
- `IBoMayQuyTrinh` **always** reads from this snapshot. Never from `quy_trinh` table directly for running applications.
- Workflows with in-progress applications cannot be edited (HTTP 409 `QUY_TRINH_DANG_SU_DUNG`). Admin must use "Create new version" instead.

## Before Modifying Workflow Behavior

1. Read `docs/ADR/0002-quy-trinh-snapshot.md`.
2. Understand the snapshot lifecycle.
3. Verify changes don't break running applications.
4. Run `BoMayQuyTrinhTests` and `KiemTraQuyTrinhTests`.

## Rule Evaluator

- Custom-built, NO dynamic eval. Supports: `= != > >= < <= IN CONTAINS BETWEEN`, `AND/OR/NOT` nesting.
- Implemented in `BoDanhGiaDieuKien` with safe value conversion.

## Deadline Calculation

- `TinhHanXuLy` calculates deadlines using **working days** (excludes Sat/Sun + `ngay_nghi_le` table).

## Workflow Validation

- `KiemTraQuyTrinh` validates: start step exists, all steps have actors, no infinite loops without exit conditions, scoring steps have council + criteria config.
- 7 validation rules, all with unit tests.

## Workflow Concepts

| Concept | Table/Config | Notes |
|---|---|---|
| Workflow definition | `quy_trinh` | Versioned, clonable |
| Steps | `quy_trinh_buoc` | 10 step types including TIEP_NHAN, CHAM_DIEM, BO_PHIEU |
| Transitions (cases) | `quy_trinh_truong_hop` | Conditional branching with jsonb rules |
| Actors | `quy_trinh_buoc_tac_nhan` | 7 actor types, processing rules: MOT_NGUOI/TAT_CA/DA_SO |
| Statuses | Per-step and global | Configurable status labels |
| Components | `thanh_phan_ho_so` | Required/optional documents per step |
| Features | Step-level toggles | 9 configurable features per step |

## Prohibited

- Do NOT hard-code `if status == X then next = Y` for configurable behavior.
- Do NOT read current workflow definition for running applications — always use snapshot.
- Do NOT allow editing workflows that have in-progress applications.
