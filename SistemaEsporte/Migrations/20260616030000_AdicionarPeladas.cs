using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEsporte.Migrations
{
    public partial class AdicionarPeladas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nível do jogador
            migrationBuilder.AddColumn<int>(
                name: "Nivel",
                table: "Jogadores",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Peladas
            migrationBuilder.CreateTable(
                name: "Peladas",
                columns: table => new {
                    Id              = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Data            = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Local           = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descricao       = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LimiteJogadores = table.Column<int>(type: "int", nullable: false, defaultValue: 16),
                    LimiteGoleiros  = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    Status          = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table => { table.PrimaryKey("PK_Peladas", x => x.Id); });

            // Inscrições
            migrationBuilder.CreateTable(
                name: "InscricoesPelada",
                columns: table => new {
                    Id              = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeladaId        = table.Column<int>(type: "int", nullable: false),
                    JogadorId       = table.Column<int>(type: "int", nullable: true),
                    NomeAvulso      = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NivelAvulso     = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    EhGoleiro       = table.Column<bool>(type: "bit", nullable: false),
                    EmEspera        = table.Column<bool>(type: "bit", nullable: false),
                    DataInscricao   = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Compareceu      = table.Column<bool>(type: "bit", nullable: false),
                    TimeDistribuido = table.Column<int>(type: "int", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_InscricoesPelada", x => x.Id);
                    table.ForeignKey("FK_InscricoesPelada_Peladas_PeladaId", x => x.PeladaId,
                        "Peladas", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_InscricoesPelada_Jogadores_JogadorId", x => x.JogadorId,
                        "Jogadores", "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex("IX_InscricoesPelada_PeladaId",   "InscricoesPelada", "PeladaId");
            migrationBuilder.CreateIndex("IX_InscricoesPelada_JogadorId",  "InscricoesPelada", "JogadorId");

            // Punições
            migrationBuilder.CreateTable(
                name: "PunicoesJogador",
                columns: table => new {
                    Id         = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JogadorId  = table.Column<int>(type: "int", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFim    = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo     = table.Column<string>(type: "nvarchar(max)", nullable: false),
                },
                constraints: table => {
                    table.PrimaryKey("PK_PunicoesJogador", x => x.Id);
                    table.ForeignKey("FK_PunicoesJogador_Jogadores_JogadorId", x => x.JogadorId,
                        "Jogadores", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_PunicoesJogador_JogadorId", "PunicoesJogador", "JogadorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("PunicoesJogador");
            migrationBuilder.DropTable("InscricoesPelada");
            migrationBuilder.DropTable("Peladas");
            migrationBuilder.DropColumn("Nivel", "Jogadores");
        }
    }
}
