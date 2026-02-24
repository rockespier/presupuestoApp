using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresupuestoFamiliarApp.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMovimientosFijos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovimientosFijos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiaDelMes = table.Column<int>(type: "int", nullable: false),
                    CuentaId = table.Column<int>(type: "int", nullable: false),
                    CategoriaGastoId = table.Column<int>(type: "int", nullable: true),
                    EspacioId = table.Column<int>(type: "int", nullable: false),
                    UltimaGeneracion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosFijos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosFijos_CategoriasGastos_CategoriaGastoId",
                        column: x => x.CategoriaGastoId,
                        principalTable: "CategoriasGastos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MovimientosFijos_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosFijos_CategoriaGastoId",
                table: "MovimientosFijos",
                column: "CategoriaGastoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosFijos_CuentaId",
                table: "MovimientosFijos",
                column: "CuentaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosFijos");
        }
    }
}
