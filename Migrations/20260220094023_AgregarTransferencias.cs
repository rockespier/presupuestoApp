using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresupuestoFamiliarApp.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTransferencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsTransferencia",
                table: "Transacciones",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsTransferencia",
                table: "Transacciones");
        }
    }
}
