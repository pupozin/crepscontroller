using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrepeControladorApi.Migrations
{
    public partial class MesaAtivaFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ativa",
                table: "Mesas",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ativa",
                table: "Mesas");
        }
    }
}
