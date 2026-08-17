-- =============================================================================
-- Khoi tao extension bat buoc cho BlueIdea (chay mot lan khi tao container).
-- Doi chieu Muc 2 dac ta: uuid-ossp, pg_trgm, unaccent, pgvector, pgcrypto.
-- =============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS unaccent;
CREATE EXTENSION IF NOT EXISTS vector;

-- Cau hinh mui gio nghiep vu.
ALTER DATABASE blueidea SET timezone TO 'Asia/Ho_Chi_Minh';

-- -----------------------------------------------------------------------------
-- Tach vai tro CSDL theo Muc 6 dac ta:
--   app_rw     : tai khoan ung dung, chi DML (khong DDL tren production)
--   app_ro     : tai khoan chi doc, phuc vu bao cao / BI
--   migration  : tai khoan chay migration (co DDL)
-- Mat khau mac dinh chi dung cho moi truong phat trien - PHAI doi khi trien khai.
-- -----------------------------------------------------------------------------
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_ro') THEN
        CREATE ROLE app_ro LOGIN PASSWORD 'doi_mat_khau_nay';
    END IF;
END
$$;

GRANT CONNECT ON DATABASE blueidea TO app_ro;
GRANT USAGE ON SCHEMA public TO app_ro;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO app_ro;
