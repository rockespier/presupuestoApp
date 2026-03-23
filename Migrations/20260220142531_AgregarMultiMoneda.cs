using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresupuestoFamiliarApp.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMultiMoneda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MonedaTransaccion",
                table: "Transacciones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoOriginal",
                table: "Transacciones",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TasaCambioUsada",
                table: "Transacciones",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MonedaPrincipal",
                table: "Espacios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TiposCambio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonedaOrigen = table.Column<int>(type: "int", nullable: false),
                    MonedaDestino = table.Column<int>(type: "int", nullable: false),
                    Tasa = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposCambio", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Espacios",
                keyColumn: "Id",
                keyValue: 1,
                column: "MonedaPrincipal",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TiposCambio");

            migrationBuilder.DropColumn(
                name: "MonedaTransaccion",
                table: "Transacciones");

            migrationBuilder.DropColumn(
                name: "MontoOriginal",
                table: "Transacciones");

            migrationBuilder.DropColumn(
                name: "TasaCambioUsada",
                table: "Transacciones");

            migrationBuilder.DropColumn(
                name: "MonedaPrincipal",
                table: "Espacios");
        }
    }
}
