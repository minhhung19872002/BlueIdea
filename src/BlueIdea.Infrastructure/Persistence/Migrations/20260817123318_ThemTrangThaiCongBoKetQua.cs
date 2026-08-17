using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueIdea.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ThemTrangThaiCongBoKetQua : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "da_cong_bo_ket_qua",
                table: "sang_kien",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ngay_cong_bo_ket_qua",
                table: "sang_kien",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "da_cong_bo_ket_qua",
                table: "sang_kien");

            migrationBuilder.DropColumn(
                name: "ngay_cong_bo_ket_qua",
                table: "sang_kien");
        }
    }
}
