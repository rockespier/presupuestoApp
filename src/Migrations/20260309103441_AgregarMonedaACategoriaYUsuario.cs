using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresupuestoFamiliarApp.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMonedaACategoriaYUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MonedaPreferida",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MonedaCategoria",
                table: "CategoriasGastos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "CategoriasGastos",
                keyColumn: "Id",
                keyValue: 1,
                column: "MonedaCategoria",
                value: 0);

            migrationBuilder.UpdateData(
                table: "CategoriasGastos",
                keyColumn: "Id",
                keyValue: 2,
                column: "MonedaCategoria",
                value: 0);

            migrationBuilder.UpdateData(
                table: "CategoriasGastos",
                keyColumn: "Id",
                keyValue: 3,
                column: "MonedaCategoria",
                value: 0);

            migrationBuilder.UpdateData(
                table: "CategoriasGastos",
                keyColumn: "Id",
                keyValue: 4,
                column: "MonedaCategoria",
                value: 0);

            migrationBuilder.UpdateData(
                table: "CategoriasGastos",
                keyColumn: "Id",
                keyValue: 5,
                column: "MonedaCategoria",
                value: 0);

            migrationBuilder.UpdateData(
                table: "CategoriasGastos",
                keyColumn: "Id",
                keyValue: 6,
                column: "MonedaCategoria",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "MonedaPreferida", "PasswordHash" },
                values: new object[] { 0, "$2a$11$C9nbaZCmDYuRVqAPZPX0ZOiTFauPypdAVBRuC0wkKDFLvf/ZP1MMm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonedaPreferida",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "MonedaCategoria",
                table: "CategoriasGastos");

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$kRrQ9UcIJQsPOVe67nsbHez2YGjKqY/aUEw14FDsrfcDd0uIFloSm");
        }
    }
}
