using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiProject.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNoteAndPickupTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupTime",
                table: "Orders",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickupTime",
                table: "Orders");
        }
    }
}
