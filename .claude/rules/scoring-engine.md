# Scoring Engine Rules

Scoring criteria and evaluation must remain configuration-driven.

## Architecture

- `BlueIdea.Scoring/` contains `BoTinhDiem` (scoring calculator) and `MoHinhChamDiem` (models).
- `BlueIdea.Application/TieuChi/` manages criteria configuration.
- `BlueIdea.Application/DanhGia/` manages evaluation process.

## Criteria Configuration

- **Criteria groups**: 2-level tree (group → criteria).
- **Weight validation**: Total weight across groups must equal 100%. Real-time validation on UI, server blocks save if invalid.
- **Score range validation**: Recognition levels defined by score ranges. Server blocks overlapping ranges (`KHOANG_DIEM_CHONG_LAN`).
- **4 input types** for scoring (slider, number, select, text).
- **Criteria are versioned**: Snapshot attached to evaluation period (`dot_de_nghi`).

## Scoring Calculation

- 3 calculation methods supported.
- **High/low score exclusion**: When enabled with N evaluators, exclude highest and lowest scores, average the rest.
- **Recognition levels**: Score ranges map to recognition outcomes (e.g., 80-100 = "Xuất sắc").
- Scores computed in `BoTinhDiem` with comprehensive unit tests.

## Council Evaluation

- Council members score independently.
- **Conflict of interest**: Member who is also an application author is excluded from scoring that application.
- Score visibility: Individual scores only visible after the evaluation form is submitted.
- **Score matrix**: Rows = applications, Columns = council members. Scores only appear after submission.
- Secretary can reopen submitted evaluation forms.

## Prohibited

- Do NOT hard-code scoring schemes or recognition levels.
- Do NOT allow criteria edits while an evaluation period is active with in-progress evaluations.
- Do NOT reveal individual scores before form submission.
