using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEsporte.Migrations
{
    public partial class AdicionarJogadorPelada : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JogadoresPelada",
                columns: table => new {
                    Id       = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome     = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nivel    = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                },
                constraints: table => { table.PrimaryKey("PK_JogadoresPelada", x => x.Id); });

            migrationBuilder.AddColumn<int>(
                name: "JogadorPeladaId",
                table: "InscricoesPelada",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InscricoesPelada_JogadorPeladaId",
                table: "InscricoesPelada",
                column: "JogadorPeladaId");

            migrationBuilder.AddForeignKey(
                name: "FK_InscricoesPelada_JogadoresPelada_JogadorPeladaId",
                table: "InscricoesPelada",
                column: "JogadorPeladaId",
                principalTable: "JogadoresPelada",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_InscricoesPelada_JogadoresPelada_JogadorPeladaId", "InscricoesPelada");
            migrationBuilder.DropIndex("IX_InscricoesPelada_JogadorPeladaId", "InscricoesPelada");
            migrationBuilder.DropColumn("JogadorPeladaId", "InscricoesPelada");
            migrationBuilder.DropTable("JogadoresPelada");
        }
    }
}
