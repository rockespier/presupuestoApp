using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresupuestoFamiliarApp.Migrations
{
    /// <inheritdoc />
    public partial class ImplementarSeguridadUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$i6xmGH7IWFwQ3vXkK0OGluG8PF7DFLToDHNGksFhPCGEZMOx22jmq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$Fa5KVjWc4XOsNt8qxGaUvOIUSrh/jfpmH6IEJOFRMYfpA1MGg/XKC");
        }
    }
}
