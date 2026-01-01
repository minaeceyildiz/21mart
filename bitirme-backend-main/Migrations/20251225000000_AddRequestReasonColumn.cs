using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiProject.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestReasonColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // request_reason kolonunu ekle (öğrencinin yazdığı özel metin için)
            migrationBuilder.AddColumn<string>(
                name: "request_reason",
                table: "appointments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Geri al: request_reason kolonunu sil
            migrationBuilder.DropColumn(
                name: "request_reason",
                table: "appointments");
        }
    }
}

