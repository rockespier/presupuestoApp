using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresupuestoFamiliarApp.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEspaciosTrabajo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EspacioId",
                table: "Cuentas",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "EspacioId",
                table: "CategoriasGastos",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Espacios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Espacios", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "CategoriasGastos",
                keyColumn: "Id",
                keyValue: 1,
                column: "EspacioId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "CategoriasGastos",
                keyColumn: "Id",
                keyValue: 2,
                column: "EspacioId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "CategoriasGastos",
                keyColumn: "Id",
                keyValue: 3,
                column: "EspacioId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "CategoriasGastos",
                keyColumn: "Id",
                keyValue: 4,
                column: "EspacioId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "CategoriasGastos",
                keyColumn: "Id",
                keyValue: 5,
                column: "EspacioId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "CategoriasGastos",
                keyColumn: "Id",
                keyValue: 6,
                column: "EspacioId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 1,
                column: "EspacioId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 2,
                column: "EspacioId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Cuentas",
                keyColumn: "Id",
                keyValue: 3,
                column: "EspacioId",
                value: 1);

            migrationBuilder.InsertData(
                table: "Espacios",
                columns: new[] { "Id", "Nombre" },
                values: new object[] { 1, "Mi Casa" });

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_EspacioId",
                table: "Cuentas",
                column: "EspacioId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasGastos_EspacioId",
                table: "CategoriasGastos",
                column: "EspacioId");

            migrationBuilder.AddForeignKey(
                name: "FK_CategoriasGastos_Espacios_EspacioId",
                table: "CategoriasGastos",
                column: "EspacioId",
                principalTable: "Espacios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cuentas_Espacios_EspacioId",
                table: "Cuentas",
                column: "EspacioId",
                principalTable: "Espacios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CategoriasGastos_Espacios_EspacioId",
                table: "CategoriasGastos");

            migrationBuilder.DropForeignKey(
                name: "FK_Cuentas_Espacios_EspacioId",
                table: "Cuentas");

            migrationBuilder.DropTable(
                name: "Espacios");

            migrationBuilder.DropIndex(
                name: "IX_Cuentas_EspacioId",
                table: "Cuentas");

            migrationBuilder.DropIndex(
                name: "IX_CategoriasGastos_EspacioId",
                table: "CategoriasGastos");

            migrationBuilder.DropColumn(
                name: "EspacioId",
                table: "Cuentas");

            migrationBuilder.DropColumn(
                name: "EspacioId",
                table: "CategoriasGastos");
        }
    }
}
