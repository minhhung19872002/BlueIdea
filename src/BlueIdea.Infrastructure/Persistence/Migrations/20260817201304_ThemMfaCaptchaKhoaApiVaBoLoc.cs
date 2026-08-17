using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueIdea.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ThemMfaCaptchaKhoaApiVaBoLoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "mfa_buoc_da_dung",
                table: "nguoi_dung",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mfa_ma_khoi_phuc",
                table: "nguoi_dung",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "mfa_ngay_bat",
                table: "nguoi_dung",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bo_loc_yeu_thich",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    nguoi_dung_id = table.Column<Guid>(type: "uuid", nullable: false),
                    man_hinh = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ten = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tham_so = table.Column<string>(type: "jsonb", nullable: false),
                    mac_dinh = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_bo_loc_yeu_thich", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "khoa_api_ngoai",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ten = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tien_to = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    khoa_bam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    danh_sach_ip = table.Column<string>(type: "jsonb", nullable: false),
                    dang_hoat_dong = table.Column<bool>(type: "boolean", nullable: false),
                    ngay_het_han = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lan_goi_cuoi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    so_lan_goi = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("pk_khoa_api_ngoai", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ma_xac_thuc_tam",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    loai = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    khoa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ma_bam = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    het_han = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    so_lan_thu_sai = table.Column<int>(type: "integer", nullable: false),
                    da_dung = table.Column<bool>(type: "boolean", nullable: false),
                    dia_chi_ip = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_ma_xac_thuc_tam", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bo_loc_yeu_thich_da_xoa",
                table: "bo_loc_yeu_thich",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_bo_loc_yeu_thich_nguoi_dung_id_man_hinh",
                table: "bo_loc_yeu_thich",
                columns: new[] { "nguoi_dung_id", "man_hinh" });

            migrationBuilder.CreateIndex(
                name: "ix_bo_loc_yeu_thich_nguoi_dung_id_man_hinh_ten",
                table: "bo_loc_yeu_thich",
                columns: new[] { "nguoi_dung_id", "man_hinh", "ten" },
                unique: true,
                filter: "da_xoa = false");

            migrationBuilder.CreateIndex(
                name: "ix_khoa_api_ngoai_da_xoa",
                table: "khoa_api_ngoai",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_khoa_api_ngoai_tien_to",
                table: "khoa_api_ngoai",
                column: "tien_to");

            migrationBuilder.CreateIndex(
                name: "ix_ma_xac_thuc_tam_da_xoa",
                table: "ma_xac_thuc_tam",
                column: "da_xoa");

            migrationBuilder.CreateIndex(
                name: "ix_ma_xac_thuc_tam_het_han",
                table: "ma_xac_thuc_tam",
                column: "het_han");

            migrationBuilder.CreateIndex(
                name: "ix_ma_xac_thuc_tam_loai_khoa",
                table: "ma_xac_thuc_tam",
                columns: new[] { "loai", "khoa" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bo_loc_yeu_thich");

            migrationBuilder.DropTable(
                name: "khoa_api_ngoai");

            migrationBuilder.DropTable(
                name: "ma_xac_thuc_tam");

            migrationBuilder.DropColumn(
                name: "mfa_buoc_da_dung",
                table: "nguoi_dung");

            migrationBuilder.DropColumn(
                name: "mfa_ma_khoi_phuc",
                table: "nguoi_dung");

            migrationBuilder.DropColumn(
                name: "mfa_ngay_bat",
                table: "nguoi_dung");
        }
    }
}
