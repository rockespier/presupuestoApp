using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PresupuestoFamiliarApp.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriasGastos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subcategoria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PresupuestoMensual = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasGastos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cuentas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SaldoActual = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuentas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transacciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    CategoriaGastoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transacciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transacciones_CategoriasGastos_CategoriaGastoId",
                        column: x => x.CategoriaGastoId,
                        principalTable: "CategoriasGastos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Transacciones_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CategoriasGastos",
                columns: new[] { "Id", "Nombre", "PresupuestoMensual", "Subcategoria" },
                values: new object[,]
                {
                    { 1, "Comida", 500m, null },
                    { 2, "Vivienda", 1000m, null },
                    { 3, "Transporte", 150m, null },
                    { 4, "Gastos personales", 200m, "Peluquería, móvil, deporte" },
                    { 5, "Gastos de mascota", 100m, null },
                    { 6, "Servicios de casa", 200m, "Luz, agua, gas, internet" }
                });

            migrationBuilder.InsertData(
                table: "Cuentas",
                columns: new[] { "Id", "Nombre", "SaldoActual" },
                values: new object[,]
                {
                    { 1, "Cuenta Roberto", 0m },
                    { 2, "Cuenta Ivette", 0m },
                    { 3, "Efectivo", 0m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_CategoriaGastoId",
                table: "Transacciones",
                column: "CategoriaGastoId");

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_CuentaId",
                table: "Transacciones",
                column: "CuentaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transacciones");

            migrationBuilder.DropTable(
                name: "CategoriasGastos");

            migrationBuilder.DropTable(
                name: "Cuentas");
        }
    }
}
