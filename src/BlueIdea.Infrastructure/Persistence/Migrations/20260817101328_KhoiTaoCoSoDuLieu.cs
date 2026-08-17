using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueIdea.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KhoiTaoCoSoDuLieu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "bieu_mau_thong_ke",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    loai_bao_cao = table.Column<string>(type: "text", nullable: true),
                    cau_hinh_tieu_chi = table.Column<string>(type: "jsonb", nullable: true),
                    cau_hinh_cot = table.Column<string>(type: "jsonb", nullable: false),
                    cau_hinh_bo_loc = table.Column<string>(type: "jsonb", nullable: true),
                    dinh_dang_xuat = table.Column<string>(type: "jsonb", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bieu_mau_thong_ke", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bieu_mau_xuat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    loai = table.Column<string>(type: "text", nullable: false),
                    dinh_dang = table.Column<string>(type: "text", nullable: false),
                    file_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cau_hinh_truong = table.Column<string>(type: "jsonb", nullable: false),
                    pham_vi_ap_dung = table.Column<string>(type: "jsonb", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bieu_mau_xuat", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bo_tieu_chi",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nam = table.Column<int>(type: "integer", nullable: false),
                    cap = table.Column<string>(type: "text", nullable: false),
                    thang_diem_toi_da = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    diem_dat_toi_thieu = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    cach_tinh = table.Column<string>(type: "text", nullable: false),
                    lam_tron = table.Column<int>(type: "integer", nullable: false),
                    cho_phep_cham_doc_lap = table.Column<bool>(type: "boolean", nullable: false),
                    tu_dong_tong_hop = table.Column<bool>(type: "boolean", nullable: false),
                    loai_bo_diem_cao_thap = table.Column<bool>(type: "boolean", nullable: false),
                    pham_vi_ap_dung = table.Column<string>(type: "jsonb", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bo_tieu_chi", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cau_hinh_chu_ky_so",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nha_cung_cap = table.Column<string>(type: "text", nullable: false),
                    loai_ky = table.Column<string>(type: "text", nullable: false),
                    endpoint = table.Column<string>(type: "text", nullable: true),
                    client_id = table.Column<string>(type: "text", nullable: true),
                    client_secret_ma_hoa = table.Column<string>(type: "text", nullable: true),
                    chung_thu_so = table.Column<string>(type: "text", nullable: true),
                    thuat_toan = table.Column<string>(type: "text", nullable: false),
                    tich_hop_plugin_url = table.Column<string>(type: "text", nullable: true),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false),
                    la_mac_dinh = table.Column<bool>(type: "boolean", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cau_hinh_chu_ky_so", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cau_hinh_email_sms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    loai = table.Column<string>(type: "text", nullable: false),
                    nha_cung_cap = table.Column<string>(type: "text", nullable: true),
                    host = table.Column<string>(type: "text", nullable: true),
                    port = table.Column<int>(type: "integer", nullable: true),
                    ten_dang_nhap = table.Column<string>(type: "text", nullable: true),
                    mat_khau_ma_hoa = table.Column<string>(type: "text", nullable: true),
                    su_dung_ssl = table.Column<bool>(type: "boolean", nullable: false),
                    email_gui_di = table.Column<string>(type: "text", nullable: true),
                    ten_hien_thi = table.Column<string>(type: "text", nullable: true),
                    api_endpoint = table.Column<string>(type: "text", nullable: true),
                    api_key_ma_hoa = table.Column<string>(type: "text", nullable: true),
                    brandname = table.Column<string>(type: "text", nullable: true),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false),
                    la_mac_dinh = table.Column<bool>(type: "boolean", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cau_hinh_email_sms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cau_hinh_he_thong",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nhom = table.Column<string>(type: "text", nullable: false),
                    khoa = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    gia_tri = table.Column<string>(type: "text", nullable: true),
                    gia_tri_json = table.Column<string>(type: "jsonb", nullable: true),
                    kieu_du_lieu = table.Column<string>(type: "text", nullable: false),
                    ten_hien_thi = table.Column<string>(type: "text", nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    cho_phep_sua = table.Column<bool>(type: "boolean", nullable: false),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cau_hinh_he_thong", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cau_hinh_menu",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ma = table.Column<string>(type: "text", nullable: false),
                    ten = table.Column<string>(type: "text", nullable: false),
                    icon = table.Column<string>(type: "text", nullable: true),
                    duong_dan = table.Column<string>(type: "text", nullable: true),
                    menu_cha_id = table.Column<Guid>(type: "uuid", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    quyen_ma = table.Column<string>(type: "text", nullable: true),
                    loai = table.Column<string>(type: "text", nullable: false),
                    hien_thi = table.Column<bool>(type: "boolean", nullable: false),
                    mo_tab_moi = table.Column<bool>(type: "boolean", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cau_hinh_menu", x => x.id);
                    table.ForeignKey(
                        name: "fk_cau_hinh_menu_cau_hinh_menu_menu_cha_id",
                        column: x => x.menu_cha_id,
                        principalTable: "cau_hinh_menu",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "doi_tuong",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_doi_tuong", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "don_vi",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ten_viet_tat = table.Column<string>(type: "text", nullable: true),
                    don_vi_cha_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cap = table.Column<int>(type: "integer", nullable: false),
                    loai = table.Column<string>(type: "text", nullable: false),
                    path = table.Column<string>(type: "text", nullable: false),
                    dia_chi = table.Column<string>(type: "text", nullable: true),
                    dien_thoai = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    nguoi_dai_dien = table.Column<string>(type: "text", nullable: true),
                    chuc_vu_nguoi_dai_dien = table.Column<string>(type: "text", nullable: true),
                    la_don_vi_phe_duyet = table.Column<bool>(type: "boolean", nullable: false),
                    cap_phe_duyet = table.Column<string>(type: "text", nullable: true),
                    logo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tieu_de_van_ban = table.Column<string>(type: "text", nullable: true),
                    nguoi_ky_mac_dinh = table.Column<string>(type: "text", nullable: true),
                    chuc_vu_nguoi_ky_mac_dinh = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_don_vi", x => x.id);
                    table.ForeignKey(
                        name: "fk_don_vi_don_vi_don_vi_cha_id",
                        column: x => x.don_vi_cha_id,
                        principalTable: "don_vi",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dot_de_nghi",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nam = table.Column<int>(type: "integer", nullable: false),
                    ky = table.Column<string>(type: "text", nullable: true),
                    tu_ngay = table.Column<DateOnly>(type: "date", nullable: true),
                    den_ngay = table.Column<DateOnly>(type: "date", nullable: true),
                    han_nop_ho_so = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    han_cham_diem = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cap_xet_duyet = table.Column<string>(type: "text", nullable: false),
                    quy_trinh_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bo_tieu_chi_id = table.Column<Guid>(type: "uuid", nullable: true),
                    don_vi_ap_dung_ids = table.Column<string>(type: "jsonb", nullable: false),
                    trang_thai_dot = table.Column<string>(type: "text", nullable: false),
                    tu_dong_khoa = table.Column<bool>(type: "boolean", nullable: false),
                    ghi_chu = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dot_de_nghi", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hang_doi_gui_tin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    kenh = table.Column<string>(type: "text", nullable: false),
                    nguoi_nhan = table.Column<string>(type: "text", nullable: false),
                    tieu_de = table.Column<string>(type: "text", nullable: true),
                    noi_dung = table.Column<string>(type: "text", nullable: false),
                    so_lan_thu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai_gui = table.Column<string>(type: "text", nullable: false),
                    thong_bao_loi = table.Column<string>(type: "text", nullable: true),
                    thoi_gian_gui = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hang_doi_gui_tin", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "he_thong_tich_hop",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    endpoint_base = table.Column<string>(type: "text", nullable: true),
                    loai_xac_thuc = table.Column<string>(type: "text", nullable: false),
                    client_id = table.Column<string>(type: "text", nullable: true),
                    client_secret_ma_hoa = table.Column<string>(type: "text", nullable: true),
                    scope = table.Column<string>(type: "text", nullable: true),
                    cau_hinh_mapping = table.Column<string>(type: "jsonb", nullable: true),
                    tan_suat_dong_bo = table.Column<string>(type: "text", nullable: false),
                    lan_dong_bo_cuoi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_he_thong_tich_hop", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hoi_dong",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cap = table.Column<string>(type: "text", nullable: false),
                    dot_de_nghi_id = table.Column<Guid>(type: "uuid", nullable: true),
                    don_vi_id = table.Column<Guid>(type: "uuid", nullable: true),
                    so_quyet_dinh_thanh_lap = table.Column<string>(type: "text", nullable: true),
                    ngay_quyet_dinh = table.Column<DateOnly>(type: "date", nullable: true),
                    tep_quyet_dinh_id = table.Column<Guid>(type: "uuid", nullable: true),
                    thoi_gian_hoat_dong_tu = table.Column<DateOnly>(type: "date", nullable: true),
                    thoi_gian_hoat_dong_den = table.Column<DateOnly>(type: "date", nullable: true),
                    linh_vuc_phu_trach = table.Column<string>(type: "jsonb", nullable: false),
                    so_thanh_vien_toi_thieu = table.Column<int>(type: "integer", nullable: false),
                    ty_le_thong_qua = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    trang_thai_hoat_dong = table.Column<string>(type: "text", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hoi_dong", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ket_qua_xet_duyet",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sang_kien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hoi_dong_id = table.Column<Guid>(type: "uuid", nullable: false),
                    phien_hop_id = table.Column<Guid>(type: "uuid", nullable: true),
                    so_phieu_cham = table.Column<int>(type: "integer", nullable: false),
                    diem_cao_nhat = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    diem_thap_nhat = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    diem_trung_binh = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    tong_diem_trong_so = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    so_phieu_dong_y = table.Column<int>(type: "integer", nullable: false),
                    so_phieu_khong_dong_y = table.Column<int>(type: "integer", nullable: false),
                    ket_qua = table.Column<string>(type: "text", nullable: true),
                    muc_cong_nhan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ly_do = table.Column<string>(type: "text", nullable: true),
                    nguoi_ket_luan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_ket_luan = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_cong_bo = table.Column<bool>(type: "boolean", nullable: false),
                    ngay_cong_bo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ket_qua_xet_duyet", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "kiem_tra_trung_lap",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sang_kien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ngay_chay = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    phien_ban_thuat_toan = table.Column<string>(type: "text", nullable: false),
                    pham_vi = table.Column<string>(type: "jsonb", nullable: true),
                    tong_so_doi_chieu = table.Column<int>(type: "integer", nullable: false),
                    ty_le_cao_nhat = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    muc_canh_bao = table.Column<string>(type: "text", nullable: false),
                    trang_thai_chay = table.Column<string>(type: "text", nullable: false),
                    thoi_gian_xu_ly_ms = table.Column<int>(type: "integer", nullable: false),
                    thong_bao_loi = table.Column<string>(type: "text", nullable: true),
                    da_xem_xet = table.Column<bool>(type: "boolean", nullable: false),
                    y_kien_hoi_dong = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kiem_tra_trung_lap", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lich_su_mat_khau",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nguoi_dung_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mat_khau_hash = table.Column<string>(type: "text", nullable: false),
                    mat_khau_salt = table.Column<string>(type: "text", nullable: true),
                    thoi_gian = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lich_su_mat_khau", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "linh_vuc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    linh_vuc_cha_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_linh_vuc", x => x.id);
                    table.ForeignKey(
                        name: "fk_linh_vuc_linh_vuc_linh_vuc_cha_id",
                        column: x => x.linh_vuc_cha_id,
                        principalTable: "linh_vuc",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loai_tac_gia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cho_phep_nhieu_tac_gia = table.Column<bool>(type: "boolean", nullable: false),
                    so_tac_gia_toi_da = table.Column<int>(type: "integer", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loai_tac_gia", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mau_thong_bao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    kenh = table.Column<string>(type: "text", nullable: false),
                    su_kien = table.Column<string>(type: "text", nullable: false),
                    tieu_de = table.Column<string>(type: "text", nullable: false),
                    noi_dung = table.Column<string>(type: "text", nullable: false),
                    danh_sach_bien = table.Column<string>(type: "jsonb", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mau_thong_bao", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ngay_nghi_le",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ngay = table.Column<DateOnly>(type: "date", nullable: false),
                    ten = table.Column<string>(type: "text", nullable: false),
                    lap_lai_hang_nam = table.Column<bool>(type: "boolean", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ngay_nghi_le", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nguoi_dung",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ten_dang_nhap = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mat_khau_hash = table.Column<string>(type: "text", nullable: true),
                    mat_khau_salt = table.Column<string>(type: "text", nullable: true),
                    ho_ten = table.Column<string>(type: "text", nullable: false),
                    ho_ten_khong_dau = table.Column<string>(type: "text", nullable: false),
                    ngay_sinh = table.Column<DateOnly>(type: "date", nullable: true),
                    gioi_tinh = table.Column<string>(type: "text", nullable: true),
                    so_cccd = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    dien_thoai = table.Column<string>(type: "text", nullable: true),
                    don_vi_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chuc_vu = table.Column<string>(type: "text", nullable: true),
                    anh_dai_dien_id = table.Column<Guid>(type: "uuid", nullable: true),
                    loai_tai_khoan = table.Column<string>(type: "text", nullable: false),
                    sso_subject_id = table.Column<string>(type: "text", nullable: true),
                    sso_provider = table.Column<string>(type: "text", nullable: true),
                    trang_thai_tai_khoan = table.Column<string>(type: "text", nullable: false),
                    buoc_doi_mat_khau = table.Column<bool>(type: "boolean", nullable: false),
                    so_lan_dang_nhap_sai = table.Column<int>(type: "integer", nullable: false),
                    khoa_den = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lan_dang_nhap_cuoi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ngay_doi_mat_khau_cuoi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    mfa_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    mfa_secret = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nguoi_dung", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nhat_ky_dang_nhap",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ten_dang_nhap = table.Column<string>(type: "text", nullable: false),
                    nguoi_dung_id = table.Column<Guid>(type: "uuid", nullable: true),
                    thanh_cong = table.Column<bool>(type: "boolean", nullable: false),
                    ly_do_that_bai = table.Column<string>(type: "text", nullable: true),
                    dia_chi_ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    thiet_bi = table.Column<string>(type: "text", nullable: true),
                    thoi_gian = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nhat_ky_dang_nhap", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nhat_ky_dong_bo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    he_thong_tich_hop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chieu = table.Column<string>(type: "text", nullable: false),
                    loai_du_lieu = table.Column<string>(type: "text", nullable: true),
                    tong_ban_ghi = table.Column<int>(type: "integer", nullable: false),
                    thanh_cong = table.Column<int>(type: "integer", nullable: false),
                    that_bai = table.Column<int>(type: "integer", nullable: false),
                    du_lieu_gui = table.Column<string>(type: "text", nullable: true),
                    phan_hoi = table.Column<string>(type: "text", nullable: true),
                    trang_thai_dong_bo = table.Column<string>(type: "text", nullable: false),
                    thong_bao_loi = table.Column<string>(type: "text", nullable: true),
                    thoi_gian_bat_dau = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    thoi_gian_ket_thuc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nhat_ky_dong_bo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nhat_ky_he_thong",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nguoi_dung_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ten_dang_nhap = table.Column<string>(type: "text", nullable: true),
                    hanh_dong = table.Column<string>(type: "text", nullable: false),
                    module = table.Column<string>(type: "text", nullable: false),
                    doi_tuong = table.Column<string>(type: "text", nullable: true),
                    doi_tuong_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    du_lieu_truoc = table.Column<string>(type: "text", nullable: true),
                    du_lieu_sau = table.Column<string>(type: "text", nullable: true),
                    dia_chi_ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    ket_qua = table.Column<string>(type: "text", nullable: false),
                    thoi_gian = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nhat_ky_he_thong", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nhat_ky_ky_so",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    doi_tuong = table.Column<string>(type: "text", nullable: false),
                    doi_tuong_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nguoi_ky_id = table.Column<Guid>(type: "uuid", nullable: true),
                    thoi_gian_ky = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    serial_chung_thu = table.Column<string>(type: "text", nullable: true),
                    nguoi_cap_chung_thu = table.Column<string>(type: "text", nullable: true),
                    hieu_luc_tu = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    hieu_luc_den = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tep_goc_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tep_da_ky_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trang_thai_ky = table.Column<string>(type: "text", nullable: false),
                    thong_tin_xac_thuc = table.Column<string>(type: "jsonb", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nhat_ky_ky_so", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "nhat_ky_loi",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    muc_do = table.Column<string>(type: "text", nullable: false),
                    nguon = table.Column<string>(type: "text", nullable: false),
                    thong_bao = table.Column<string>(type: "text", nullable: false),
                    stack_trace = table.Column<string>(type: "text", nullable: true),
                    du_lieu_ngu_canh = table.Column<string>(type: "jsonb", nullable: true),
                    nguoi_dung_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dia_chi_ip = table.Column<string>(type: "text", nullable: true),
                    thoi_gian = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    da_xu_ly = table.Column<bool>(type: "boolean", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nhat_ky_loi", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "phieu_danh_gia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sang_kien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hoi_dong_id = table.Column<Guid>(type: "uuid", nullable: false),
                    thanh_vien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bo_tieu_chi_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bo_tieu_chi_snapshot = table.Column<string>(type: "text", nullable: true),
                    tong_diem = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    diem_theo_nhom = table.Column<string>(type: "jsonb", nullable: false),
                    nhan_xet_chung = table.Column<string>(type: "text", nullable: true),
                    uu_diem = table.Column<string>(type: "text", nullable: true),
                    han_che = table.Column<string>(type: "text", nullable: true),
                    de_xuat_muc_cong_nhan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ket_luan = table.Column<string>(type: "text", nullable: true),
                    trang_thai_phieu = table.Column<string>(type: "text", nullable: false),
                    ngay_cham = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ngay_gui = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    chu_ky_so_id = table.Column<Guid>(type: "uuid", nullable: true),
                    so_phieu = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phieu_danh_gia", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quy_trinh",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    cap = table.Column<string>(type: "text", nullable: false),
                    phien_ban = table.Column<int>(type: "integer", nullable: false),
                    quy_trinh_goc_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pham_vi_ap_dung = table.Column<string>(type: "jsonb", nullable: false),
                    la_mac_dinh = table.Column<bool>(type: "boolean", nullable: false),
                    trang_thai_quy_trinh = table.Column<string>(type: "text", nullable: false),
                    so_do_layout = table.Column<string>(type: "jsonb", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quy_trinh", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quyen",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ma = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ten = table.Column<string>(type: "text", nullable: false),
                    nhom_chuc_nang = table.Column<string>(type: "text", nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quyen", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quyet_dinh",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    so_quyet_dinh = table.Column<string>(type: "text", nullable: false),
                    ngay_ban_hanh = table.Column<DateOnly>(type: "date", nullable: false),
                    loai = table.Column<string>(type: "text", nullable: false),
                    trich_yeu = table.Column<string>(type: "text", nullable: true),
                    nguoi_ky = table.Column<string>(type: "text", nullable: true),
                    chuc_vu_nguoi_ky = table.Column<string>(type: "text", nullable: true),
                    don_vi_ban_hanh_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dot_de_nghi_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tep_tin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    da_ky_so = table.Column<bool>(type: "boolean", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quyet_dinh", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nguoi_dung_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    het_han = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    thoi_gian_thu_hoi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    thay_the_boi_token_hash = table.Column<string>(type: "text", nullable: true),
                    dia_chi_ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sang_kien",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ma_ho_so = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten_sang_kien = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "text", nullable: false),
                    dot_de_nghi_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linh_vuc_id = table.Column<Guid>(type: "uuid", nullable: false),
                    doi_tuong_id = table.Column<Guid>(type: "uuid", nullable: true),
                    loai_tac_gia_id = table.Column<Guid>(type: "uuid", nullable: true),
                    don_vi_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quy_trinh_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quy_trinh_snapshot = table.Column<string>(type: "text", nullable: true),
                    buoc_hien_tai_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trang_thai_hien_tai_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trang_thai_tong = table.Column<string>(type: "text", nullable: false),
                    mo_ta_giai_phap = table.Column<string>(type: "text", nullable: true),
                    tinh_trang_truoc_khi_ap_dung = table.Column<string>(type: "text", nullable: true),
                    noi_dung_giai_phap = table.Column<string>(type: "text", nullable: true),
                    tinh_moi = table.Column<string>(type: "text", nullable: true),
                    kha_nang_ap_dung = table.Column<string>(type: "text", nullable: true),
                    pham_vi_ap_dung = table.Column<string>(type: "text", nullable: true),
                    hieu_qua_kinh_te = table.Column<string>(type: "text", nullable: true),
                    gia_tri_lam_loi_uoc_tinh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    hieu_qua_xa_hoi = table.Column<string>(type: "text", nullable: true),
                    thoi_gian_ap_dung_tu = table.Column<DateOnly>(type: "date", nullable: true),
                    thoi_gian_ap_dung_den = table.Column<DateOnly>(type: "date", nullable: true),
                    noi_dung_dong = table.Column<string>(type: "jsonb", nullable: false),
                    ty_le_trung_lap = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    trang_thai_kiem_tra_trung_lap = table.Column<string>(type: "text", nullable: false),
                    tong_diem = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    diem_trung_binh = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    muc_cong_nhan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ket_qua = table.Column<string>(type: "text", nullable: true),
                    quyet_dinh_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_cong_nhan = table.Column<DateOnly>(type: "date", nullable: true),
                    ngay_nop = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    han_xu_ly_hien_tai = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ngay_hoan_thanh = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dang_khoa = table.Column<bool>(type: "boolean", nullable: false),
                    ly_do_khoa = table.Column<string>(type: "text", nullable: true),
                    cong_khai = table.Column<bool>(type: "boolean", nullable: false),
                    so_luot_xem = table.Column<int>(type: "integer", nullable: false),
                    phien_ban = table.Column<int>(type: "integer", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sang_kien", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sang_kien_doan_van",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sang_kien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nguon = table.Column<string>(type: "text", nullable: false),
                    tep_tin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chi_muc = table.Column<int>(type: "integer", nullable: false),
                    noi_dung = table.Column<string>(type: "text", nullable: false),
                    noi_dung_chuan_hoa = table.Column<string>(type: "text", nullable: false),
                    so_tu = table.Column<int>(type: "integer", nullable: false),
                    sim_hash = table.Column<long>(type: "bigint", nullable: false),
                    embedding = table.Column<float[]>(type: "real[]", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sang_kien_doan_van", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sang_kien_phan_cong",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sang_kien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hoi_dong_id = table.Column<Guid>(type: "uuid", nullable: false),
                    thanh_vien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nguoi_phan_cong_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_phan_cong = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    han_hoan_thanh = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    trang_thai_phan_cong = table.Column<string>(type: "text", nullable: false),
                    ghi_chu = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sang_kien_phan_cong", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tep_tin",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ten_goc = table.Column<string>(type: "text", nullable: false),
                    ten_luu_tru = table.Column<string>(type: "text", nullable: false),
                    duong_dan = table.Column<string>(type: "text", nullable: false),
                    bucket = table.Column<string>(type: "text", nullable: false),
                    kich_thuoc = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: true),
                    phan_mo_rong = table.Column<string>(type: "text", nullable: true),
                    hash_sha256 = table.Column<string>(type: "text", nullable: true),
                    nguoi_tai_len_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tai_len = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    da_quet_virus = table.Column<bool>(type: "boolean", nullable: false),
                    noi_dung_trich_xuat = table.Column<string>(type: "text", nullable: true),
                    trang_thai_ocr = table.Column<string>(type: "text", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tep_tin", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "thong_bao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nguoi_nhan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tieu_de = table.Column<string>(type: "text", nullable: false),
                    noi_dung = table.Column<string>(type: "text", nullable: false),
                    loai_su_kien = table.Column<string>(type: "text", nullable: true),
                    doi_tuong_lien_quan = table.Column<string>(type: "text", nullable: true),
                    doi_tuong_id = table.Column<Guid>(type: "uuid", nullable: true),
                    duong_dan = table.Column<string>(type: "text", nullable: true),
                    muc_do = table.Column<string>(type: "text", nullable: false),
                    da_doc = table.Column<bool>(type: "boolean", nullable: false),
                    ngay_doc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    thoi_gian = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_thong_bao", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vai_tro",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    la_he_thong = table.Column<bool>(type: "boolean", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vai_tro", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "muc_cong_nhan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    bo_tieu_chi_id = table.Column<Guid>(type: "uuid", nullable: true),
                    diem_tu = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    diem_den = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    mau_sac = table.Column<string>(type: "text", nullable: true),
                    la_dat = table.Column<bool>(type: "boolean", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_muc_cong_nhan", x => x.id);
                    table.ForeignKey(
                        name: "fk_muc_cong_nhan_bo_tieu_chi_bo_tieu_chi_id",
                        column: x => x.bo_tieu_chi_id,
                        principalTable: "bo_tieu_chi",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nhom_tieu_chi",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    bo_tieu_chi_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trong_so = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    diem_toi_da = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nhom_tieu_chi", x => x.id);
                    table.ForeignKey(
                        name: "fk_nhom_tieu_chi_bo_tieu_chi_bo_tieu_chi_id",
                        column: x => x.bo_tieu_chi_id,
                        principalTable: "bo_tieu_chi",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cau_hinh_cap_phe_duyet",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    dot_de_nghi_id = table.Column<Guid>(type: "uuid", nullable: true),
                    linh_vuc_id = table.Column<Guid>(type: "uuid", nullable: true),
                    don_vi_phe_duyet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    thu_tu_cap = table.Column<int>(type: "integer", nullable: false),
                    ghi_chu = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cau_hinh_cap_phe_duyet", x => x.id);
                    table.ForeignKey(
                        name: "fk_cau_hinh_cap_phe_duyet_don_vi_don_vi_phe_duyet_id",
                        column: x => x.don_vi_phe_duyet_id,
                        principalTable: "don_vi",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hoi_dong_thanh_vien",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    hoi_dong_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nguoi_dung_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ho_ten_hien_thi = table.Column<string>(type: "text", nullable: false),
                    chuc_vu_cong_tac = table.Column<string>(type: "text", nullable: true),
                    don_vi_cong_tac = table.Column<string>(type: "text", nullable: true),
                    chuc_danh = table.Column<string>(type: "text", nullable: false),
                    quyen_cham_diem = table.Column<bool>(type: "boolean", nullable: false),
                    quyen_nhan_xet = table.Column<bool>(type: "boolean", nullable: false),
                    quyen_bo_phieu = table.Column<bool>(type: "boolean", nullable: false),
                    quyen_ky_bien_ban = table.Column<bool>(type: "boolean", nullable: false),
                    quyen_ket_luan = table.Column<bool>(type: "boolean", nullable: false),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hoi_dong_thanh_vien", x => x.id);
                    table.ForeignKey(
                        name: "fk_hoi_dong_thanh_vien_hoi_dong_hoi_dong_id",
                        column: x => x.hoi_dong_id,
                        principalTable: "hoi_dong",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phien_hop_hoi_dong",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    hoi_dong_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ma_phien = table.Column<string>(type: "text", nullable: false),
                    ten_phien = table.Column<string>(type: "text", nullable: false),
                    thoi_gian_bat_dau = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    thoi_gian_ket_thuc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dia_diem = table.Column<string>(type: "text", nullable: true),
                    hinh_thuc = table.Column<string>(type: "text", nullable: false),
                    chu_tri_id = table.Column<Guid>(type: "uuid", nullable: true),
                    thu_ky_id = table.Column<Guid>(type: "uuid", nullable: true),
                    noi_dung = table.Column<string>(type: "text", nullable: true),
                    ket_luan = table.Column<string>(type: "text", nullable: true),
                    trang_thai_phien = table.Column<string>(type: "text", nullable: false),
                    tep_bien_ban_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phien_hop_hoi_dong", x => x.id);
                    table.ForeignKey(
                        name: "fk_phien_hop_hoi_dong_hoi_dong_hoi_dong_id",
                        column: x => x.hoi_dong_id,
                        principalTable: "hoi_dong",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kiem_tra_trung_lap_chi_tiet",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    kiem_tra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sang_kien_doi_chieu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ty_le_tuong_dong = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ty_le_tu_vung = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ty_le_ngu_nghia = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    so_doan_trung = table.Column<int>(type: "integer", nullable: false),
                    cac_doan_trung = table.Column<string>(type: "jsonb", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kiem_tra_trung_lap_chi_tiet", x => x.id);
                    table.ForeignKey(
                        name: "fk_kiem_tra_trung_lap_chi_tiet_kiem_tra_trung_lap_kiem_tra_id",
                        column: x => x.kiem_tra_id,
                        principalTable: "kiem_tra_trung_lap",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phieu_danh_gia_chi_tiet",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    phieu_danh_gia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tieu_chi_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ten_tieu_chi_snapshot = table.Column<string>(type: "text", nullable: false),
                    diem_toi_da_snapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    diem = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    muc_diem_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nhan_xet = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phieu_danh_gia_chi_tiet", x => x.id);
                    table.ForeignKey(
                        name: "fk_phieu_danh_gia_chi_tiet_phieu_danh_gia_phieu_danh_gia_id",
                        column: x => x.phieu_danh_gia_id,
                        principalTable: "phieu_danh_gia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quy_trinh_buoc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    quy_trinh_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ma = table.Column<string>(type: "text", nullable: false),
                    ten = table.Column<string>(type: "text", nullable: false),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    loai_buoc = table.Column<string>(type: "text", nullable: false),
                    so_ngay_xu_ly = table.Column<int>(type: "integer", nullable: false),
                    tinh_theo_ngay_lam_viec = table.Column<bool>(type: "boolean", nullable: false),
                    bat_buoc_dinh_kem = table.Column<bool>(type: "boolean", nullable: false),
                    danh_sach_tep_bat_buoc = table.Column<string>(type: "jsonb", nullable: false),
                    bat_buoc_nhap_y_kien = table.Column<bool>(type: "boolean", nullable: false),
                    cho_phep_uy_quyen = table.Column<bool>(type: "boolean", nullable: false),
                    cho_phep_thu_hoi = table.Column<bool>(type: "boolean", nullable: false),
                    la_buoc_bat_dau = table.Column<bool>(type: "boolean", nullable: false),
                    la_buoc_ket_thuc = table.Column<bool>(type: "boolean", nullable: false),
                    canh_bao_truoc_han_gio = table.Column<int>(type: "integer", nullable: false),
                    mo_ta_huong_dan = table.Column<string>(type: "text", nullable: true),
                    hoi_dong_id = table.Column<Guid>(type: "uuid", nullable: true),
                    bo_tieu_chi_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quy_trinh_buoc", x => x.id);
                    table.ForeignKey(
                        name: "fk_quy_trinh_buoc_quy_trinh_quy_trinh_id",
                        column: x => x.quy_trinh_id,
                        principalTable: "quy_trinh",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quy_trinh_chuc_nang_bo_sung",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    quy_trinh_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buoc_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ma_chuc_nang = table.Column<string>(type: "text", nullable: false),
                    bat_buoc = table.Column<bool>(type: "boolean", nullable: false),
                    cau_hinh = table.Column<string>(type: "jsonb", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quy_trinh_chuc_nang_bo_sung", x => x.id);
                    table.ForeignKey(
                        name: "fk_quy_trinh_chuc_nang_bo_sung_quy_trinh_quy_trinh_id",
                        column: x => x.quy_trinh_id,
                        principalTable: "quy_trinh",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quy_trinh_lien_thong",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    quy_trinh_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buoc_id = table.Column<Guid>(type: "uuid", nullable: true),
                    he_thong_tich_hop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    su_kien = table.Column<string>(type: "text", nullable: false),
                    loai_du_lieu = table.Column<string>(type: "text", nullable: true),
                    cau_hinh_mapping = table.Column<string>(type: "jsonb", nullable: true),
                    dong_bo_hai_chieu = table.Column<bool>(type: "boolean", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quy_trinh_lien_thong", x => x.id);
                    table.ForeignKey(
                        name: "fk_quy_trinh_lien_thong_quy_trinh_quy_trinh_id",
                        column: x => x.quy_trinh_id,
                        principalTable: "quy_trinh",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quy_trinh_thanh_phan_ho_so",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    quy_trinh_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ma = table.Column<string>(type: "text", nullable: false),
                    ten = table.Column<string>(type: "text", nullable: false),
                    bat_buoc = table.Column<bool>(type: "boolean", nullable: false),
                    loai_du_lieu = table.Column<string>(type: "text", nullable: false),
                    dinh_dang_cho_phep = table.Column<string>(type: "jsonb", nullable: false),
                    dung_luong_toi_da_mb = table.Column<int>(type: "integer", nullable: false),
                    so_luong_toi_da = table.Column<int>(type: "integer", nullable: false),
                    so_ky_tu_toi_thieu = table.Column<int>(type: "integer", nullable: false),
                    so_ky_tu_toi_da = table.Column<int>(type: "integer", nullable: false),
                    dung_de_kiem_tra_trung_lap = table.Column<bool>(type: "boolean", nullable: false),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    mo_ta_huong_dan = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quy_trinh_thanh_phan_ho_so", x => x.id);
                    table.ForeignKey(
                        name: "fk_quy_trinh_thanh_phan_ho_so_quy_trinh_quy_trinh_id",
                        column: x => x.quy_trinh_id,
                        principalTable: "quy_trinh",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quyet_dinh_sang_kien",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    quyet_dinh_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sang_kien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    muc_cong_nhan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ghi_chu = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quyet_dinh_sang_kien", x => x.id);
                    table.ForeignKey(
                        name: "fk_quyet_dinh_sang_kien_quyet_dinh_quyet_dinh_id",
                        column: x => x.quyet_dinh_id,
                        principalTable: "quyet_dinh",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sang_kien_lich_su",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sang_kien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hanh_dong = table.Column<string>(type: "text", nullable: false),
                    truong_thay_doi = table.Column<string>(type: "jsonb", nullable: false),
                    gia_tri_truoc = table.Column<string>(type: "jsonb", nullable: true),
                    gia_tri_sau = table.Column<string>(type: "jsonb", nullable: true),
                    nguoi_thuc_hien_id = table.Column<Guid>(type: "uuid", nullable: true),
                    thoi_gian = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    dia_chi_ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    ghi_chu = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sang_kien_lich_su", x => x.id);
                    table.ForeignKey(
                        name: "fk_sang_kien_lich_su_sang_kien_sang_kien_id",
                        column: x => x.sang_kien_id,
                        principalTable: "sang_kien",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sang_kien_tac_gia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sang_kien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nguoi_dung_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ho_ten = table.Column<string>(type: "text", nullable: false),
                    ngay_sinh = table.Column<DateOnly>(type: "date", nullable: true),
                    gioi_tinh = table.Column<string>(type: "text", nullable: true),
                    so_cccd = table.Column<string>(type: "text", nullable: true),
                    chuc_vu = table.Column<string>(type: "text", nullable: true),
                    don_vi_cong_tac = table.Column<string>(type: "text", nullable: true),
                    trinh_do_chuyen_mon = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    dien_thoai = table.Column<string>(type: "text", nullable: true),
                    ty_le_dong_gop = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    la_tac_gia_chinh = table.Column<bool>(type: "boolean", nullable: false),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sang_kien_tac_gia", x => x.id);
                    table.ForeignKey(
                        name: "fk_sang_kien_tac_gia_sang_kien_sang_kien_id",
                        column: x => x.sang_kien_id,
                        principalTable: "sang_kien",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sang_kien_xu_ly",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sang_kien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buoc_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ten_buoc_snapshot = table.Column<string>(type: "text", nullable: false),
                    trang_thai_id = table.Column<Guid>(type: "uuid", nullable: true),
                    truong_hop_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ten_truong_hop_snapshot = table.Column<string>(type: "text", nullable: true),
                    nguoi_xu_ly_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nguoi_uy_quyen_id = table.Column<Guid>(type: "uuid", nullable: true),
                    y_kien = table.Column<string>(type: "text", nullable: true),
                    tep_dinh_kem_ids = table.Column<string>(type: "jsonb", nullable: false),
                    thoi_gian_nhan = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    han_xu_ly = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    thoi_gian_xu_ly = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    so_ngay_xu_ly = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    qua_han = table.Column<bool>(type: "boolean", nullable: false),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sang_kien_xu_ly", x => x.id);
                    table.ForeignKey(
                        name: "fk_sang_kien_xu_ly_sang_kien_sang_kien_id",
                        column: x => x.sang_kien_id,
                        principalTable: "sang_kien",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sang_kien_tep_dinh_kem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    sang_kien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tep_tin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    thanh_phan_ho_so_ma = table.Column<string>(type: "text", nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    phien_ban = table.Column<int>(type: "integer", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sang_kien_tep_dinh_kem", x => x.id);
                    table.ForeignKey(
                        name: "fk_sang_kien_tep_dinh_kem_sang_kien_sang_kien_id",
                        column: x => x.sang_kien_id,
                        principalTable: "sang_kien",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sang_kien_tep_dinh_kem_tep_tin_tep_tin_id",
                        column: x => x.tep_tin_id,
                        principalTable: "tep_tin",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nguoi_dung_vai_tro",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nguoi_dung_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vai_tro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    don_vi_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tu_ngay = table.Column<DateOnly>(type: "date", nullable: true),
                    den_ngay = table.Column<DateOnly>(type: "date", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nguoi_dung_vai_tro", x => x.id);
                    table.ForeignKey(
                        name: "fk_nguoi_dung_vai_tro_nguoi_dung_nguoi_dung_id",
                        column: x => x.nguoi_dung_id,
                        principalTable: "nguoi_dung",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_nguoi_dung_vai_tro_vai_tro_vai_tro_id",
                        column: x => x.vai_tro_id,
                        principalTable: "vai_tro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pham_vi_du_lieu",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    vai_tro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loai_pham_vi = table.Column<string>(type: "text", nullable: false),
                    don_vi_ids = table.Column<string>(type: "jsonb", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pham_vi_du_lieu", x => x.id);
                    table.ForeignKey(
                        name: "fk_pham_vi_du_lieu_vai_tro_vai_tro_id",
                        column: x => x.vai_tro_id,
                        principalTable: "vai_tro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vai_tro_quyen",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    vai_tro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quyen_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vai_tro_quyen", x => x.id);
                    table.ForeignKey(
                        name: "fk_vai_tro_quyen_quyen_quyen_id",
                        column: x => x.quyen_id,
                        principalTable: "quyen",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vai_tro_quyen_vai_tro_vai_tro_id",
                        column: x => x.vai_tro_id,
                        principalTable: "vai_tro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tieu_chi",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nhom_tieu_chi_id = table.Column<Guid>(type: "uuid", nullable: false),
                    diem_toi_da = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    diem_toi_thieu = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    trong_so = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    kieu_nhap = table.Column<string>(type: "text", nullable: false),
                    buoc_nhay = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    bat_buoc_nhan_xet = table.Column<bool>(type: "boolean", nullable: false),
                    huong_dan_cham = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ma = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ten_khong_dau = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    trang_thai = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tieu_chi", x => x.id);
                    table.ForeignKey(
                        name: "fk_tieu_chi_nhom_tieu_chi_nhom_tieu_chi_id",
                        column: x => x.nhom_tieu_chi_id,
                        principalTable: "nhom_tieu_chi",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bien_ban_hop",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    phien_hop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    so_bien_ban = table.Column<string>(type: "text", nullable: false),
                    noi_dung_json = table.Column<string>(type: "jsonb", nullable: true),
                    tep_tin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trang_thai_bien_ban = table.Column<string>(type: "text", nullable: false),
                    ngay_lap = table.Column<DateOnly>(type: "date", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bien_ban_hop", x => x.id);
                    table.ForeignKey(
                        name: "fk_bien_ban_hop_phien_hop_phien_hop_id",
                        column: x => x.phien_hop_id,
                        principalTable: "phien_hop_hoi_dong",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phien_hop_diem_danh",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    phien_hop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    thanh_vien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    co_mat = table.Column<bool>(type: "boolean", nullable: false),
                    ly_do_vang = table.Column<string>(type: "text", nullable: true),
                    thoi_gian_diem_danh = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phien_hop_diem_danh", x => x.id);
                    table.ForeignKey(
                        name: "fk_phien_hop_diem_danh_phien_hop_phien_hop_id",
                        column: x => x.phien_hop_id,
                        principalTable: "phien_hop_hoi_dong",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phien_hop_ho_so",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    phien_hop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sang_kien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    ket_luan_rieng = table.Column<string>(type: "text", nullable: true),
                    ket_qua = table.Column<string>(type: "text", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phien_hop_ho_so", x => x.id);
                    table.ForeignKey(
                        name: "fk_phien_hop_ho_so_phien_hop_phien_hop_id",
                        column: x => x.phien_hop_id,
                        principalTable: "phien_hop_hoi_dong",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phieu_bo_phieu",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    phien_hop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sang_kien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    thanh_vien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    y_kien = table.Column<string>(type: "text", nullable: false),
                    muc_de_xuat_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ghi_chu = table.Column<string>(type: "text", nullable: true),
                    la_phieu_kin = table.Column<bool>(type: "boolean", nullable: false),
                    thoi_gian = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phieu_bo_phieu", x => x.id);
                    table.ForeignKey(
                        name: "fk_phieu_bo_phieu_phien_hop_phien_hop_id",
                        column: x => x.phien_hop_id,
                        principalTable: "phien_hop_hoi_dong",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quy_trinh_buoc_tac_nhan",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    buoc_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loai_tac_nhan = table.Column<string>(type: "text", nullable: false),
                    tham_chieu_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tham_chieu_ma = table.Column<string>(type: "text", nullable: true),
                    quy_tac_xu_ly = table.Column<string>(type: "text", nullable: false),
                    ty_le_dong_thuan = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quy_trinh_buoc_tac_nhan", x => x.id);
                    table.ForeignKey(
                        name: "fk_quy_trinh_buoc_tac_nhan_quy_trinh_buoc_buoc_id",
                        column: x => x.buoc_id,
                        principalTable: "quy_trinh_buoc",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quy_trinh_trang_thai",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    quy_trinh_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buoc_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ma = table.Column<string>(type: "text", nullable: false),
                    ten = table.Column<string>(type: "text", nullable: false),
                    mau_sac = table.Column<string>(type: "text", nullable: true),
                    icon = table.Column<string>(type: "text", nullable: true),
                    la_trang_thai_ket_thuc = table.Column<bool>(type: "boolean", nullable: false),
                    hien_thi_cho_tac_gia = table.Column<bool>(type: "boolean", nullable: false),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quy_trinh_trang_thai", x => x.id);
                    table.ForeignKey(
                        name: "fk_quy_trinh_trang_thai_quy_trinh_buoc_buoc_id",
                        column: x => x.buoc_id,
                        principalTable: "quy_trinh_buoc",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_quy_trinh_trang_thai_quy_trinh_quy_trinh_id",
                        column: x => x.quy_trinh_id,
                        principalTable: "quy_trinh",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "quy_trinh_truong_hop",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    buoc_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ma = table.Column<string>(type: "text", nullable: false),
                    ten = table.Column<string>(type: "text", nullable: false),
                    buoc_tiep_theo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    trang_thai_gan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dieu_kien = table.Column<string>(type: "jsonb", nullable: true),
                    hanh_dong = table.Column<string>(type: "jsonb", nullable: false),
                    mau_thong_bao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mau_nut = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    la_mac_dinh = table.Column<bool>(type: "boolean", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quy_trinh_truong_hop", x => x.id);
                    table.ForeignKey(
                        name: "fk_quy_trinh_truong_hop_quy_trinh_buoc_buoc_id",
                        column: x => x.buoc_id,
                        principalTable: "quy_trinh_buoc",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tieu_chi_muc_diem",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    tieu_chi_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ten = table.Column<string>(type: "text", nullable: false),
                    diem = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    mo_ta = table.Column<string>(type: "text", nullable: true),
                    thu_tu = table.Column<int>(type: "integer", nullable: false),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tieu_chi_muc_diem", x => x.id);
                    table.ForeignKey(
                        name: "fk_tieu_chi_muc_diem_tieu_chi_tieu_chi_id",
                        column: x => x.tieu_chi_id,
                        principalTable: "tieu_chi",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bien_ban_chu_ky",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    bien_ban_id = table.Column<Guid>(type: "uuid", nullable: false),
                    thanh_vien_id = table.Column<Guid>(type: "uuid", nullable: false),
                    da_ky = table.Column<bool>(type: "boolean", nullable: false),
                    thoi_gian_ky = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    chu_ky_so_id = table.Column<Guid>(type: "uuid", nullable: true),
                    anh_chu_ky_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nguoi_tao_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_tao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    nguoi_sua_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_sua = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    da_xoa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    nguoi_xoa_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ngay_xoa = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bien_ban_chu_ky", x => x.id);
                    table.ForeignKey(
                        name: "fk_bien_ban_chu_ky_bien_ban_hop_bien_ban_id",
                        column: x => x.bien_ban_id,
                        principalTable: "bien_ban_hop",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bien_ban_chu_ky_bien_ban_id",
                table: "bien_ban_chu_ky",
                column: "bien_ban_id");

            migrationBuilder.CreateIndex(
                name: "ix_bien_ban_chu_ky_da_xoa",
                table: "bien_ban_chu_ky",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_bien_ban_hop_da_xoa",
                table: "bien_ban_hop",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_bien_ban_hop_phien_hop_id",
                table: "bien_ban_hop",
                column: "phien_hop_id");

            migrationBuilder.CreateIndex(
                name: "ix_bieu_mau_thong_ke_da_xoa",
                table: "bieu_mau_thong_ke",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_bieu_mau_thong_ke_ma",
                table: "bieu_mau_thong_ke",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_bieu_mau_thong_ke_ten_khong_dau",
                table: "bieu_mau_thong_ke",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_bieu_mau_xuat_da_xoa",
                table: "bieu_mau_xuat",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_bieu_mau_xuat_ma",
                table: "bieu_mau_xuat",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_bieu_mau_xuat_ten_khong_dau",
                table: "bieu_mau_xuat",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_bo_tieu_chi_da_xoa",
                table: "bo_tieu_chi",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_bo_tieu_chi_ma",
                table: "bo_tieu_chi",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_bo_tieu_chi_ten_khong_dau",
                table: "bo_tieu_chi",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_cau_hinh_cap_phe_duyet_da_xoa",
                table: "cau_hinh_cap_phe_duyet",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_cau_hinh_cap_phe_duyet_don_vi_phe_duyet_id",
                table: "cau_hinh_cap_phe_duyet",
                column: "don_vi_phe_duyet_id");

            migrationBuilder.CreateIndex(
                name: "ix_cau_hinh_chu_ky_so_da_xoa",
                table: "cau_hinh_chu_ky_so",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_cau_hinh_email_sms_da_xoa",
                table: "cau_hinh_email_sms",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_cau_hinh_he_thong_da_xoa",
                table: "cau_hinh_he_thong",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_cau_hinh_he_thong_khoa",
                table: "cau_hinh_he_thong",
                column: "khoa",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_cau_hinh_menu_da_xoa",
                table: "cau_hinh_menu",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_cau_hinh_menu_menu_cha_id",
                table: "cau_hinh_menu",
                column: "menu_cha_id");

            migrationBuilder.CreateIndex(
                name: "ix_doi_tuong_da_xoa",
                table: "doi_tuong",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_doi_tuong_ma",
                table: "doi_tuong",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_doi_tuong_ten_khong_dau",
                table: "doi_tuong",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_don_vi_da_xoa",
                table: "don_vi",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_don_vi_don_vi_cha_id",
                table: "don_vi",
                column: "don_vi_cha_id");

            migrationBuilder.CreateIndex(
                name: "ix_don_vi_ma",
                table: "don_vi",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_don_vi_path",
                table: "don_vi",
                column: "path");

            migrationBuilder.CreateIndex(
                name: "ix_don_vi_ten_khong_dau",
                table: "don_vi",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_dot_de_nghi_da_xoa",
                table: "dot_de_nghi",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_dot_de_nghi_ma",
                table: "dot_de_nghi",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_dot_de_nghi_ten_khong_dau",
                table: "dot_de_nghi",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_hang_doi_gui_tin_da_xoa",
                table: "hang_doi_gui_tin",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_he_thong_tich_hop_da_xoa",
                table: "he_thong_tich_hop",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_he_thong_tich_hop_ma",
                table: "he_thong_tich_hop",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_he_thong_tich_hop_ten_khong_dau",
                table: "he_thong_tich_hop",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_hoi_dong_da_xoa",
                table: "hoi_dong",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_hoi_dong_ma",
                table: "hoi_dong",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_hoi_dong_ten_khong_dau",
                table: "hoi_dong",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_hoi_dong_thanh_vien_da_xoa",
                table: "hoi_dong_thanh_vien",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_hoi_dong_thanh_vien_hoi_dong_id",
                table: "hoi_dong_thanh_vien",
                column: "hoi_dong_id");

            migrationBuilder.CreateIndex(
                name: "ix_ket_qua_xet_duyet_da_xoa",
                table: "ket_qua_xet_duyet",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_kiem_tra_trung_lap_da_xoa",
                table: "kiem_tra_trung_lap",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_kiem_tra_trung_lap_chi_tiet_da_xoa",
                table: "kiem_tra_trung_lap_chi_tiet",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_kiem_tra_trung_lap_chi_tiet_kiem_tra_id",
                table: "kiem_tra_trung_lap_chi_tiet",
                column: "kiem_tra_id");

            migrationBuilder.CreateIndex(
                name: "ix_lich_su_mat_khau_da_xoa",
                table: "lich_su_mat_khau",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_linh_vuc_da_xoa",
                table: "linh_vuc",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_linh_vuc_linh_vuc_cha_id",
                table: "linh_vuc",
                column: "linh_vuc_cha_id");

            migrationBuilder.CreateIndex(
                name: "ix_linh_vuc_ma",
                table: "linh_vuc",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_linh_vuc_ten_khong_dau",
                table: "linh_vuc",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_loai_tac_gia_da_xoa",
                table: "loai_tac_gia",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_loai_tac_gia_ma",
                table: "loai_tac_gia",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_loai_tac_gia_ten_khong_dau",
                table: "loai_tac_gia",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_mau_thong_bao_da_xoa",
                table: "mau_thong_bao",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_mau_thong_bao_ma",
                table: "mau_thong_bao",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_mau_thong_bao_ten_khong_dau",
                table: "mau_thong_bao",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_muc_cong_nhan_bo_tieu_chi_id",
                table: "muc_cong_nhan",
                column: "bo_tieu_chi_id");

            migrationBuilder.CreateIndex(
                name: "ix_muc_cong_nhan_da_xoa",
                table: "muc_cong_nhan",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_muc_cong_nhan_ma",
                table: "muc_cong_nhan",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_muc_cong_nhan_ten_khong_dau",
                table: "muc_cong_nhan",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_ngay_nghi_le_da_xoa",
                table: "ngay_nghi_le",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_ngay_nghi_le_ngay",
                table: "ngay_nghi_le",
                column: "ngay");

            migrationBuilder.CreateIndex(
                name: "ix_nguoi_dung_da_xoa",
                table: "nguoi_dung",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_nguoi_dung_don_vi_id",
                table: "nguoi_dung",
                column: "don_vi_id");

            migrationBuilder.CreateIndex(
                name: "ix_nguoi_dung_email",
                table: "nguoi_dung",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_nguoi_dung_ho_ten_khong_dau",
                table: "nguoi_dung",
                column: "ho_ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_nguoi_dung_ten_dang_nhap",
                table: "nguoi_dung",
                column: "ten_dang_nhap",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_nguoi_dung_vai_tro_da_xoa",
                table: "nguoi_dung_vai_tro",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_nguoi_dung_vai_tro_nguoi_dung_id",
                table: "nguoi_dung_vai_tro",
                column: "nguoi_dung_id");

            migrationBuilder.CreateIndex(
                name: "ix_nguoi_dung_vai_tro_vai_tro_id",
                table: "nguoi_dung_vai_tro",
                column: "vai_tro_id");

            migrationBuilder.CreateIndex(
                name: "ix_nhat_ky_dang_nhap_da_xoa",
                table: "nhat_ky_dang_nhap",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_nhat_ky_dang_nhap_ten_dang_nhap",
                table: "nhat_ky_dang_nhap",
                column: "ten_dang_nhap");

            migrationBuilder.CreateIndex(
                name: "ix_nhat_ky_dang_nhap_thoi_gian",
                table: "nhat_ky_dang_nhap",
                column: "thoi_gian");

            migrationBuilder.CreateIndex(
                name: "ix_nhat_ky_dong_bo_da_xoa",
                table: "nhat_ky_dong_bo",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_nhat_ky_he_thong_da_xoa",
                table: "nhat_ky_he_thong",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_nhat_ky_he_thong_doi_tuong_id",
                table: "nhat_ky_he_thong",
                column: "doi_tuong_id");

            migrationBuilder.CreateIndex(
                name: "ix_nhat_ky_he_thong_module_hanh_dong",
                table: "nhat_ky_he_thong",
                columns: new[] { "module", "hanh_dong" });

            migrationBuilder.CreateIndex(
                name: "ix_nhat_ky_he_thong_nguoi_dung_id",
                table: "nhat_ky_he_thong",
                column: "nguoi_dung_id");

            migrationBuilder.CreateIndex(
                name: "ix_nhat_ky_he_thong_thoi_gian",
                table: "nhat_ky_he_thong",
                column: "thoi_gian");

            migrationBuilder.CreateIndex(
                name: "ix_nhat_ky_ky_so_da_xoa",
                table: "nhat_ky_ky_so",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_nhat_ky_loi_da_xoa",
                table: "nhat_ky_loi",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_nhom_tieu_chi_bo_tieu_chi_id",
                table: "nhom_tieu_chi",
                column: "bo_tieu_chi_id");

            migrationBuilder.CreateIndex(
                name: "ix_nhom_tieu_chi_da_xoa",
                table: "nhom_tieu_chi",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_nhom_tieu_chi_ma",
                table: "nhom_tieu_chi",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_nhom_tieu_chi_ten_khong_dau",
                table: "nhom_tieu_chi",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_pham_vi_du_lieu_da_xoa",
                table: "pham_vi_du_lieu",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_pham_vi_du_lieu_vai_tro_id",
                table: "pham_vi_du_lieu",
                column: "vai_tro_id");

            migrationBuilder.CreateIndex(
                name: "ix_phien_hop_diem_danh_da_xoa",
                table: "phien_hop_diem_danh",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_phien_hop_diem_danh_phien_hop_id",
                table: "phien_hop_diem_danh",
                column: "phien_hop_id");

            migrationBuilder.CreateIndex(
                name: "ix_phien_hop_ho_so_da_xoa",
                table: "phien_hop_ho_so",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_phien_hop_ho_so_phien_hop_id",
                table: "phien_hop_ho_so",
                column: "phien_hop_id");

            migrationBuilder.CreateIndex(
                name: "ix_phien_hop_hoi_dong_da_xoa",
                table: "phien_hop_hoi_dong",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_phien_hop_hoi_dong_hoi_dong_id",
                table: "phien_hop_hoi_dong",
                column: "hoi_dong_id");

            migrationBuilder.CreateIndex(
                name: "ix_phieu_bo_phieu_da_xoa",
                table: "phieu_bo_phieu",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_phieu_bo_phieu_phien_hop_id",
                table: "phieu_bo_phieu",
                column: "phien_hop_id");

            migrationBuilder.CreateIndex(
                name: "ix_phieu_danh_gia_da_xoa",
                table: "phieu_danh_gia",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_phieu_danh_gia_sang_kien_id_hoi_dong_id_thanh_vien_id",
                table: "phieu_danh_gia",
                columns: new[] { "sang_kien_id", "hoi_dong_id", "thanh_vien_id" },
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_phieu_danh_gia_chi_tiet_da_xoa",
                table: "phieu_danh_gia_chi_tiet",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_phieu_danh_gia_chi_tiet_phieu_danh_gia_id",
                table: "phieu_danh_gia_chi_tiet",
                column: "phieu_danh_gia_id");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_da_xoa",
                table: "quy_trinh",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_ma",
                table: "quy_trinh",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_ten_khong_dau",
                table: "quy_trinh",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_buoc_da_xoa",
                table: "quy_trinh_buoc",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_buoc_quy_trinh_id",
                table: "quy_trinh_buoc",
                column: "quy_trinh_id");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_buoc_tac_nhan_buoc_id",
                table: "quy_trinh_buoc_tac_nhan",
                column: "buoc_id");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_buoc_tac_nhan_da_xoa",
                table: "quy_trinh_buoc_tac_nhan",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_chuc_nang_bo_sung_da_xoa",
                table: "quy_trinh_chuc_nang_bo_sung",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_chuc_nang_bo_sung_quy_trinh_id",
                table: "quy_trinh_chuc_nang_bo_sung",
                column: "quy_trinh_id");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_lien_thong_da_xoa",
                table: "quy_trinh_lien_thong",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_lien_thong_quy_trinh_id",
                table: "quy_trinh_lien_thong",
                column: "quy_trinh_id");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_thanh_phan_ho_so_da_xoa",
                table: "quy_trinh_thanh_phan_ho_so",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_thanh_phan_ho_so_quy_trinh_id",
                table: "quy_trinh_thanh_phan_ho_so",
                column: "quy_trinh_id");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_trang_thai_buoc_id",
                table: "quy_trinh_trang_thai",
                column: "buoc_id");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_trang_thai_da_xoa",
                table: "quy_trinh_trang_thai",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_trang_thai_quy_trinh_id",
                table: "quy_trinh_trang_thai",
                column: "quy_trinh_id");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_truong_hop_buoc_id",
                table: "quy_trinh_truong_hop",
                column: "buoc_id");

            migrationBuilder.CreateIndex(
                name: "ix_quy_trinh_truong_hop_da_xoa",
                table: "quy_trinh_truong_hop",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_quyen_da_xoa",
                table: "quyen",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_quyen_ma",
                table: "quyen",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_quyet_dinh_da_xoa",
                table: "quyet_dinh",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_quyet_dinh_sang_kien_da_xoa",
                table: "quyet_dinh_sang_kien",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_quyet_dinh_sang_kien_quyet_dinh_id",
                table: "quyet_dinh_sang_kien",
                column: "quyet_dinh_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_da_xoa",
                table: "refresh_token",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_nguoi_dung_id",
                table: "refresh_token",
                column: "nguoi_dung_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_token_hash",
                table: "refresh_token",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_buoc_hien_tai_id",
                table: "sang_kien",
                column: "buoc_hien_tai_id");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_cong_khai_ket_qua",
                table: "sang_kien",
                columns: new[] { "cong_khai", "ket_qua" });

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_da_xoa",
                table: "sang_kien",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_don_vi_id",
                table: "sang_kien",
                column: "don_vi_id");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_dot_de_nghi_id",
                table: "sang_kien",
                column: "dot_de_nghi_id");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_han_xu_ly_hien_tai",
                table: "sang_kien",
                column: "han_xu_ly_hien_tai");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_linh_vuc_id",
                table: "sang_kien",
                column: "linh_vuc_id");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_ma_ho_so",
                table: "sang_kien",
                column: "ma_ho_so",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_ten_khong_dau",
                table: "sang_kien",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_trang_thai_tong",
                table: "sang_kien",
                column: "trang_thai_tong");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_doan_van_da_xoa",
                table: "sang_kien_doan_van",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_doan_van_sang_kien_id",
                table: "sang_kien_doan_van",
                column: "sang_kien_id");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_doan_van_sim_hash",
                table: "sang_kien_doan_van",
                column: "sim_hash");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_lich_su_da_xoa",
                table: "sang_kien_lich_su",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_lich_su_sang_kien_id",
                table: "sang_kien_lich_su",
                column: "sang_kien_id");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_phan_cong_da_xoa",
                table: "sang_kien_phan_cong",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_phan_cong_sang_kien_id_thanh_vien_id",
                table: "sang_kien_phan_cong",
                columns: new[] { "sang_kien_id", "thanh_vien_id" },
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_tac_gia_da_xoa",
                table: "sang_kien_tac_gia",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_tac_gia_sang_kien_id",
                table: "sang_kien_tac_gia",
                column: "sang_kien_id");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_tep_dinh_kem_da_xoa",
                table: "sang_kien_tep_dinh_kem",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_tep_dinh_kem_sang_kien_id",
                table: "sang_kien_tep_dinh_kem",
                column: "sang_kien_id");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_tep_dinh_kem_tep_tin_id",
                table: "sang_kien_tep_dinh_kem",
                column: "tep_tin_id");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_xu_ly_da_xoa",
                table: "sang_kien_xu_ly",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_xu_ly_nguoi_xu_ly_id_thoi_gian_xu_ly",
                table: "sang_kien_xu_ly",
                columns: new[] { "nguoi_xu_ly_id", "thoi_gian_xu_ly" });

            migrationBuilder.CreateIndex(
                name: "ix_sang_kien_xu_ly_sang_kien_id",
                table: "sang_kien_xu_ly",
                column: "sang_kien_id");

            migrationBuilder.CreateIndex(
                name: "ix_tep_tin_da_xoa",
                table: "tep_tin",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_thong_bao_da_xoa",
                table: "thong_bao",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_thong_bao_nguoi_nhan_id_da_doc",
                table: "thong_bao",
                columns: new[] { "nguoi_nhan_id", "da_doc" });

            migrationBuilder.CreateIndex(
                name: "ix_thong_bao_thoi_gian",
                table: "thong_bao",
                column: "thoi_gian");

            migrationBuilder.CreateIndex(
                name: "ix_tieu_chi_da_xoa",
                table: "tieu_chi",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_tieu_chi_ma",
                table: "tieu_chi",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_tieu_chi_nhom_tieu_chi_id",
                table: "tieu_chi",
                column: "nhom_tieu_chi_id");

            migrationBuilder.CreateIndex(
                name: "ix_tieu_chi_ten_khong_dau",
                table: "tieu_chi",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_tieu_chi_muc_diem_da_xoa",
                table: "tieu_chi_muc_diem",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_tieu_chi_muc_diem_tieu_chi_id",
                table: "tieu_chi_muc_diem",
                column: "tieu_chi_id");

            migrationBuilder.CreateIndex(
                name: "ix_vai_tro_da_xoa",
                table: "vai_tro",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_vai_tro_ma",
                table: "vai_tro",
                column: "ma",
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_vai_tro_ten_khong_dau",
                table: "vai_tro",
                column: "ten_khong_dau");

            migrationBuilder.CreateIndex(
                name: "ix_vai_tro_quyen_da_xoa",
                table: "vai_tro_quyen",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_vai_tro_quyen_quyen_id",
                table: "vai_tro_quyen",
                column: "quyen_id");

            migrationBuilder.CreateIndex(
                name: "ix_vai_tro_quyen_vai_tro_id",
                table: "vai_tro_quyen",
                column: "vai_tro_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bien_ban_chu_ky");

            migrationBuilder.DropTable(
                name: "bieu_mau_thong_ke");

            migrationBuilder.DropTable(
                name: "bieu_mau_xuat");

            migrationBuilder.DropTable(
                name: "cau_hinh_cap_phe_duyet");

            migrationBuilder.DropTable(
                name: "cau_hinh_chu_ky_so");

            migrationBuilder.DropTable(
                name: "cau_hinh_email_sms");

            migrationBuilder.DropTable(
                name: "cau_hinh_he_thong");

            migrationBuilder.DropTable(
                name: "cau_hinh_menu");

            migrationBuilder.DropTable(
                name: "doi_tuong");

            migrationBuilder.DropTable(
                name: "dot_de_nghi");

            migrationBuilder.DropTable(
                name: "hang_doi_gui_tin");

            migrationBuilder.DropTable(
                name: "he_thong_tich_hop");

            migrationBuilder.DropTable(
                name: "hoi_dong_thanh_vien");

            migrationBuilder.DropTable(
                name: "ket_qua_xet_duyet");

            migrationBuilder.DropTable(
                name: "kiem_tra_trung_lap_chi_tiet");

            migrationBuilder.DropTable(
                name: "lich_su_mat_khau");

            migrationBuilder.DropTable(
                name: "linh_vuc");

            migrationBuilder.DropTable(
                name: "loai_tac_gia");

            migrationBuilder.DropTable(
                name: "mau_thong_bao");

            migrationBuilder.DropTable(
                name: "muc_cong_nhan");

            migrationBuilder.DropTable(
                name: "ngay_nghi_le");

            migrationBuilder.DropTable(
                name: "nguoi_dung_vai_tro");

            migrationBuilder.DropTable(
                name: "nhat_ky_dang_nhap");

            migrationBuilder.DropTable(
                name: "nhat_ky_dong_bo");

            migrationBuilder.DropTable(
                name: "nhat_ky_he_thong");

            migrationBuilder.DropTable(
                name: "nhat_ky_ky_so");

            migrationBuilder.DropTable(
                name: "nhat_ky_loi");

            migrationBuilder.DropTable(
                name: "pham_vi_du_lieu");

            migrationBuilder.DropTable(
                name: "phien_hop_diem_danh");

            migrationBuilder.DropTable(
                name: "phien_hop_ho_so");

            migrationBuilder.DropTable(
                name: "phieu_bo_phieu");

            migrationBuilder.DropTable(
                name: "phieu_danh_gia_chi_tiet");

            migrationBuilder.DropTable(
                name: "quy_trinh_buoc_tac_nhan");

            migrationBuilder.DropTable(
                name: "quy_trinh_chuc_nang_bo_sung");

            migrationBuilder.DropTable(
                name: "quy_trinh_lien_thong");

            migrationBuilder.DropTable(
                name: "quy_trinh_thanh_phan_ho_so");

            migrationBuilder.DropTable(
                name: "quy_trinh_trang_thai");

            migrationBuilder.DropTable(
                name: "quy_trinh_truong_hop");

            migrationBuilder.DropTable(
                name: "quyet_dinh_sang_kien");

            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropTable(
                name: "sang_kien_doan_van");

            migrationBuilder.DropTable(
                name: "sang_kien_lich_su");

            migrationBuilder.DropTable(
                name: "sang_kien_phan_cong");

            migrationBuilder.DropTable(
                name: "sang_kien_tac_gia");

            migrationBuilder.DropTable(
                name: "sang_kien_tep_dinh_kem");

            migrationBuilder.DropTable(
                name: "sang_kien_xu_ly");

            migrationBuilder.DropTable(
                name: "thong_bao");

            migrationBuilder.DropTable(
                name: "tieu_chi_muc_diem");

            migrationBuilder.DropTable(
                name: "vai_tro_quyen");

            migrationBuilder.DropTable(
                name: "bien_ban_hop");

            migrationBuilder.DropTable(
                name: "don_vi");

            migrationBuilder.DropTable(
                name: "kiem_tra_trung_lap");

            migrationBuilder.DropTable(
                name: "nguoi_dung");

            migrationBuilder.DropTable(
                name: "phieu_danh_gia");

            migrationBuilder.DropTable(
                name: "quy_trinh_buoc");

            migrationBuilder.DropTable(
                name: "quyet_dinh");

            migrationBuilder.DropTable(
                name: "tep_tin");

            migrationBuilder.DropTable(
                name: "sang_kien");

            migrationBuilder.DropTable(
                name: "tieu_chi");

            migrationBuilder.DropTable(
                name: "quyen");

            migrationBuilder.DropTable(
                name: "vai_tro");

            migrationBuilder.DropTable(
                name: "phien_hop_hoi_dong");

            migrationBuilder.DropTable(
                name: "quy_trinh");

            migrationBuilder.DropTable(
                name: "nhom_tieu_chi");

            migrationBuilder.DropTable(
                name: "hoi_dong");

            migrationBuilder.DropTable(
                name: "bo_tieu_chi");
        }
    }
}
