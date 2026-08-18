# Database Rules

## PostgreSQL 16

Extensions: `uuid-ossp`, `pg_trgm`, `unaccent`, `pgvector`, `pgcrypto`.

## Naming Convention

- Tables and columns: **Vietnamese without diacritics, snake_case**: `sang_kien`, `dot_de_nghi`, `nguoi_dung`
- Primary key: `id uuid PRIMARY KEY DEFAULT gen_random_uuid()`
- Audit columns on every table: `nguoi_tao_id`, `ngay_tao`, `nguoi_sua_id`, `ngay_sua`, `da_xoa boolean`
- Catalog tables additionally have: `ma varchar(50) UNIQUE`, `ten varchar(500)`, `mo_ta text`, `thu_tu int`, `trang_thai smallint`

## Soft Delete

System-wide soft delete (`da_xoa boolean DEFAULT false`), enforced via EF Core global query filter. Data is never physically deleted.

## Timestamps

All timestamps use `timestamptz`, stored in UTC, displayed in `Asia/Ho_Chi_Minh`.

## Vietnamese Text Search

Columns needing Vietnamese search: add computed column `*_khong_dau` + GIN `pg_trgm` index for unaccented search.

## JSON Fields

Use `jsonb` with GIN index when querying is needed.

## Sensitive Data

Encrypted at application layer with AES-256-GCM: CCCD numbers, integration secrets, SMTP/SMS passwords.

## Migrations

EF Core Code-First. API auto-runs migration and seeds sample data on first startup.

## Prohibited

- Do NOT use EF Core InMemory for acceptance testing — use PostgreSQL Testcontainers.
- Do NOT expose PostgreSQL/MinIO/Redis to Internet (bind `127.0.0.1` only).
