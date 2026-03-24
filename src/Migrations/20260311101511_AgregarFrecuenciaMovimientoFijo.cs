using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresupuestoFamiliarApp.Migrations
{
    /// <inheritdoc />
    public partial class AgregarFrecuenciaMovimientoFijo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FrecuenciaRepeticion",
                table: "MovimientosFijos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$lGmNXDsL0GHkEtaFW1YEBuOb3V8o2rdyK2jB8Wi3FuW6iZSB2g8Em");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FrecuenciaRepeticion",
                table: "MovimientosFijos");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$VnpKDMn4aqGwIDf0c/hUp.WBa8sbAEZ5X8YDKg/R8YDYub2Xa.GSa");
        }
    }
}
