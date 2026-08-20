-- =============================================================================
-- Chuyen cac bang nhat ky sang PHAN VUNG THEO THANG (PostgreSQL declarative
-- partitioning).  TD-004.
--
-- KHI NAO CHAY: khi mot trong ba bang duoi day vuot khoang 5 trieu dong hoac
-- vai GB.  Duoi nguong do phan vung khong loi gi ma them mot lop phuc tap —
-- do 20/08/2026 bang lon nhat moi 3.505 dong / 1,2 MB.
--
-- CHAY NHU THE NAO:
--   1. Dung API (khong duoc co ket noi ghi trong luc chuyen).
--   2. Sao luu:  deploy/sao-luu-blueidea.sh
--   3. psql -U blueidea -d blueidea -f deploy/phan-vung-nhat-ky.sql
--   4. Bat lai API.  Viec nen `tao-phan-vung-thang` se tu tao phan vung cho
--      cac thang ke tiep.
--
-- DIEU PHAI BIET TRUOC KHI CHAY:
--
--   * Khoa chinh doi tu (id) thanh (id, <cot thoi gian>).  PostgreSQL bat buoc
--     nhu vay: moi rang buoc duy nhat tren bang phan vung phai chua cot phan
--     vung.  Hau qua: CSDL khong con bao dam id duy nhat TREN TOAN BANG, chi
--     duy nhat trong tung phan vung.  Voi uuid v4 thi xac suat trung la khong
--     dang ke, nhung day la mot bao dam bi mat that su chu khong phai hinh thuc.
--
--   * EF Core van khai id la khoa don, nen UPDATE/DELETE sinh ra menh de
--     `WHERE id = ...` khong kem cot thoi gian => PostgreSQL phai quet MOI phan
--     vung.  Cac bang nay hau nhu chi INSERT va SELECT nen anh huong nho, nhung
--     dung mo rong cach nay sang bang co nhieu UPDATE.
--
--   * Co mot phan vung DEFAULT lam luoi an toan: du viec nen bao tri khong chay,
--     ban ghi moi van vao duoc bang thay vi bi tu choi.  Neu thay phan vung
--     DEFAULT phinh to nghia la viec nen da chet — kiem tra ngay.
--
-- CHAY LAI DUOC: bang da phan vung roi thi bo qua, khong lam gi.
-- =============================================================================

\set ON_ERROR_STOP on

BEGIN;

CREATE OR REPLACE FUNCTION blueidea_chuyen_sang_phan_vung(
    ten_bang text,
    cot_thoi_gian text
) RETURNS text AS $$
DECLARE
    moc_dau date;
    moc_cuoi date;
    moc date;
    ten_phan_vung text;
    so_dong_truoc bigint;
    so_dong_sau bigint;
BEGIN
    -- Da phan vung roi thi thoi.
    IF EXISTS (
        SELECT 1 FROM pg_partitioned_table pt
        JOIN pg_class c ON c.oid = pt.partrelid
        WHERE c.relname = ten_bang
    ) THEN
        RETURN format('%s: da phan vung tu truoc, bo qua', ten_bang);
    END IF;

    EXECUTE format('SELECT count(*) FROM %I', ten_bang) INTO so_dong_truoc;

    -- Doi ten bang cu, dung lam nguon chep du lieu.
    EXECUTE format('ALTER TABLE %I RENAME TO %I', ten_bang, ten_bang || '_truoc_phan_vung');

    -- Bang cha: cung cot, cung mac dinh, chua co chi muc.
    EXECUTE format(
        'CREATE TABLE %I (LIKE %I INCLUDING DEFAULTS INCLUDING CONSTRAINTS) PARTITION BY RANGE (%I)',
        ten_bang, ten_bang || '_truoc_phan_vung', cot_thoi_gian);

    -- Khoa chinh BAT BUOC chua cot phan vung.
    EXECUTE format('ALTER TABLE %I ADD PRIMARY KEY (id, %I)', ten_bang, cot_thoi_gian);

    -- Pham vi thang can phu: tu ban ghi cu nhat den 3 thang toi.
    EXECUTE format(
        'SELECT date_trunc(''month'', COALESCE(min(%I), now()))::date,
                date_trunc(''month'', now() + interval ''3 months'')::date
         FROM %I', cot_thoi_gian, ten_bang || '_truoc_phan_vung')
    INTO moc_dau, moc_cuoi;

    moc := moc_dau;
    WHILE moc <= moc_cuoi LOOP
        ten_phan_vung := format('%s_p%s', ten_bang, to_char(moc, 'YYYYMM'));

        EXECUTE format(
            'CREATE TABLE %I PARTITION OF %I FOR VALUES FROM (%L) TO (%L)',
            ten_phan_vung, ten_bang, moc, moc + interval '1 month');

        moc := (moc + interval '1 month')::date;
    END LOOP;

    -- Luoi an toan: ban ghi ngoai moi khoang van co cho, khong bi tu choi.
    EXECUTE format(
        'CREATE TABLE %I PARTITION OF %I DEFAULT',
        ten_bang || '_p_mac_dinh', ten_bang);

    -- Chep du lieu sang.
    EXECUTE format('INSERT INTO %I SELECT * FROM %I', ten_bang, ten_bang || '_truoc_phan_vung');

    EXECUTE format('SELECT count(*) FROM %I', ten_bang) INTO so_dong_sau;

    IF so_dong_sau <> so_dong_truoc THEN
        RAISE EXCEPTION 'Chuyen % that bai: truoc % dong, sau % dong',
            ten_bang, so_dong_truoc, so_dong_sau;
    END IF;

    EXECUTE format('DROP TABLE %I', ten_bang || '_truoc_phan_vung');

    -- Chi muc tren bang cha tu lan sang moi phan vung.
    EXECUTE format('CREATE INDEX ON %I (%I DESC)', ten_bang, cot_thoi_gian);

    RETURN format('%s: da phan vung, %s dong giu nguyen', ten_bang, so_dong_sau);
END;
$$ LANGUAGE plpgsql;

-- Ba bang duoc thiet ke de phan vung theo thang.
SELECT blueidea_chuyen_sang_phan_vung('nhat_ky_he_thong', 'thoi_gian');
SELECT blueidea_chuyen_sang_phan_vung('nhat_ky_dang_nhap', 'thoi_gian');
SELECT blueidea_chuyen_sang_phan_vung('thong_bao', 'thoi_gian');

COMMIT;

-- Kiem chung: liet ke phan vung da tao.
SELECT c.relname AS bang_cha,
       count(i.inhrelid) AS so_phan_vung
FROM pg_class c
JOIN pg_partitioned_table pt ON pt.partrelid = c.oid
LEFT JOIN pg_inherits i ON i.inhparent = c.oid
WHERE c.relname IN ('nhat_ky_he_thong', 'nhat_ky_dang_nhap', 'thong_bao')
GROUP BY c.relname;
