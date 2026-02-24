using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PresupuestoFamiliarApp.Migrations
{
    /// <inheritdoc />
    public partial class RelacionUsuariosEspacios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Espacios_EspacioId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_EspacioId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EspacioId",
                table: "Usuarios");

            migrationBuilder.CreateTable(
                name: "EspacioUsuario",
                columns: table => new
                {
                    EspaciosId = table.Column<int>(type: "int", nullable: false),
                    UsuariosId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EspacioUsuario", x => new { x.EspaciosId, x.UsuariosId });
                    table.ForeignKey(
                        name: "FK_EspacioUsuario_Espacios_EspaciosId",
                        column: x => x.EspaciosId,
                        principalTable: "Espacios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EspacioUsuario_Usuarios_UsuariosId",
                        column: x => x.UsuariosId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$kRijcgc6W/24JVO8tlkuK.kFn.Nflb1/HpUiTCPFlunkpOBobkVdC");

            migrationBuilder.CreateIndex(
                name: "IX_EspacioUsuario_UsuariosId",
                table: "EspacioUsuario",
                column: "UsuariosId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EspacioUsuario");

            migrationBuilder.AddColumn<int>(
                name: "EspacioId",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EspacioId", "PasswordHash" },
                values: new object[] { 1, "$2a$11$hzA5MKXBItr7jt3rMP4FOODAoHCYw5QaGCE44esAfn56N0FS5TUBS" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EspacioId",
                table: "Usuarios",
                column: "EspacioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Espacios_EspacioId",
                table: "Usuarios",
                column: "EspacioId",
                principalTable: "Espacios",
                principalColumn: "Id");
        }
    }
}
