using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresupuestoFamiliarApp.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTarjetaCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsCredito",
                table: "Cuentas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 1,
                column: "EsCredito",
                value: false);

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 2,
                column: "EsCredito",
                value: false);

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 3,
                column: "EsCredito",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsCredito",
                table: "Cuentas");
        }
    }
}
