using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEsporte.Migrations
{
    public partial class AdicionarJogadores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Jogadores",
                columns: table => new
                {
                    Id            = table.Column<int>(type: "int", nullable: false)
                                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome          = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Posicao       = table.Column<int>(type: "int", nullable: false),
                    TimeId        = table.Column<int>(type: "int", nullable: false),
                    GolsMarcados  = table.Column<int>(type: "int", nullable: false),
                    GolsSofridos  = table.Column<int>(type: "int", nullable: false),
                    Vitorias      = table.Column<int>(type: "int", nullable: false),
                    Empates       = table.Column<int>(type: "int", nullable: false),
                    Derrotas      = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jogadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jogadores_Times_TimeId",
                        column: x => x.TimeId,
                        principalTable: "Times",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jogadores_TimeId",
                table: "Jogadores",
                column: "TimeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Jogadores");
        }
    }
}
