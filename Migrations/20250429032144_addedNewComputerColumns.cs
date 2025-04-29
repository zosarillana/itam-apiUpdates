using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITAM.Migrations
{
    /// <inheritdoc />
    public partial class addedNewComputerColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cd_rom",
                table: "computers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cpu",
                table: "computers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cpu_fan",
                table: "computers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fa_code",
                table: "computers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "psu",
                table: "computers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fa_code",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cd_rom",
                table: "computers");

            migrationBuilder.DropColumn(
                name: "cpu",
                table: "computers");

            migrationBuilder.DropColumn(
                name: "cpu_fan",
                table: "computers");

            migrationBuilder.DropColumn(
                name: "fa_code",
                table: "computers");

            migrationBuilder.DropColumn(
                name: "psu",
                table: "computers");

            migrationBuilder.DropColumn(
                name: "fa_code",
                table: "Assets");
        }
    }
}
