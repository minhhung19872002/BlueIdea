using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlueIdea.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ThemMoHinhNhungChoDoanVan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mo_hinh_nhung",
                table: "sang_kien_doan_van",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mo_hinh_nhung",
                table: "sang_kien_doan_van");
        }
    }
}
