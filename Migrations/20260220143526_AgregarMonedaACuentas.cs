using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresupuestoFamiliarApp.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMonedaACuentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MonedaCuenta",
                table: "Cuentas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 1,
                column: "MonedaCuenta",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 2,
                column: "MonedaCuenta",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 3,
                column: "MonedaCuenta",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonedaCuenta",
                table: "Cuentas");
        }
    }
}
