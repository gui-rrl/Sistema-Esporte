using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEsporte.Migrations
{
    /// <inheritdoc />
    public partial class MigracaoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Times",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pontuacao = table.Column<int>(type: "int", nullable: false),
                    EscudoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalVitorias = table.Column<int>(type: "int", nullable: false),
                    TotalDerrotas = table.Column<int>(type: "int", nullable: false),
                    TotalEmpates = table.Column<int>(type: "int", nullable: false),
                    TotalGolsMarcados = table.Column<int>(type: "int", nullable: false),
                    TotalGolsSofridos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Times", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Torneios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFim = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Formato = table.Column<int>(type: "int", nullable: false),
                    CodigoConvite = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    MaxTimes = table.Column<int>(type: "int", nullable: false),
                    RodadaAtual = table.Column<int>(type: "int", nullable: false),
                    TotalRodadas = table.Column<int>(type: "int", nullable: false),
                    NumeroGrupos = table.Column<int>(type: "int", nullable: false),
                    TimesPorGrupo = table.Column<int>(type: "int", nullable: false),
                    ClassificadosPorGrupo = table.Column<int>(type: "int", nullable: false),
                    VencedorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Torneios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Partidas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Time1Id = table.Column<int>(type: "int", nullable: false),
                    Time2Id = table.Column<int>(type: "int", nullable: false),
                    GolsTime1 = table.Column<int>(type: "int", nullable: false),
                    GolsTime2 = table.Column<int>(type: "int", nullable: false),
                    VencedorId = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partidas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Partidas_Times_Time1Id",
                        column: x => x.Time1Id,
                        principalTable: "Times",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Partidas_Times_Time2Id",
                        column: x => x.Time2Id,
                        principalTable: "Times",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomeUsuario = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HashSenha = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Papel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    EmailConfirmado = table.Column<bool>(type: "bit", nullable: false),
                    TokenConfirmacaoEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiracaoTokenConfirmacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TokenRedefinicaoSenha = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiracaoTokenRedefinicao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Times_TimeId",
                        column: x => x.TimeId,
                        principalTable: "Times",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PartidasTorneio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TorneioId = table.Column<int>(type: "int", nullable: false),
                    Time1Id = table.Column<int>(type: "int", nullable: true),
                    Time2Id = table.Column<int>(type: "int", nullable: true),
                    VencedorId = table.Column<int>(type: "int", nullable: true),
                    GolsTime1 = table.Column<int>(type: "int", nullable: true),
                    GolsTime2 = table.Column<int>(type: "int", nullable: true),
                    GolsTime1Volta = table.Column<int>(type: "int", nullable: true),
                    GolsTime2Volta = table.Column<int>(type: "int", nullable: true),
                    Fase = table.Column<int>(type: "int", nullable: false),
                    Rodada = table.Column<int>(type: "int", nullable: false),
                    Concluida = table.Column<bool>(type: "bit", nullable: false),
                    EhBye = table.Column<bool>(type: "bit", nullable: false),
                    ProximaPartidaId = table.Column<int>(type: "int", nullable: true),
                    PosicaoProximaPartida = table.Column<int>(type: "int", nullable: true),
                    DataPartida = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartidasTorneio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartidasTorneio_Torneios_TorneioId",
                        column: x => x.TorneioId,
                        principalTable: "Torneios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TorneioTimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TorneioId = table.Column<int>(type: "int", nullable: false),
                    TimeId = table.Column<int>(type: "int", nullable: true),
                    NomeConvidado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pontos = table.Column<int>(type: "int", nullable: false),
                    Vitorias = table.Column<int>(type: "int", nullable: false),
                    Empates = table.Column<int>(type: "int", nullable: false),
                    Derrotas = table.Column<int>(type: "int", nullable: false),
                    GolsMarcados = table.Column<int>(type: "int", nullable: false),
                    GolsSofridos = table.Column<int>(type: "int", nullable: false),
                    SaldoGols = table.Column<int>(type: "int", nullable: false),
                    PartidasJogadas = table.Column<int>(type: "int", nullable: false),
                    Grupo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TorneioTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TorneioTimes_Times_TimeId",
                        column: x => x.TimeId,
                        principalTable: "Times",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TorneioTimes_Torneios_TorneioId",
                        column: x => x.TorneioId,
                        principalTable: "Torneios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Partidas_Time1Id",
                table: "Partidas",
                column: "Time1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Partidas_Time2Id",
                table: "Partidas",
                column: "Time2Id");

            migrationBuilder.CreateIndex(
                name: "IX_PartidasTorneio_TorneioId",
                table: "PartidasTorneio",
                column: "TorneioId");

            migrationBuilder.CreateIndex(
                name: "IX_Torneios_CodigoConvite",
                table: "Torneios",
                column: "CodigoConvite",
                unique: true,
                filter: "[CodigoConvite] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TorneioTimes_TimeId",
                table: "TorneioTimes",
                column: "TimeId");

            migrationBuilder.CreateIndex(
                name: "IX_TorneioTimes_TorneioId",
                table: "TorneioTimes",
                column: "TorneioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NomeUsuario",
                table: "Usuarios",
                column: "NomeUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TimeId",
                table: "Usuarios",
                column: "TimeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Partidas");

            migrationBuilder.DropTable(
                name: "PartidasTorneio");

            migrationBuilder.DropTable(
                name: "TorneioTimes");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Torneios");

            migrationBuilder.DropTable(
                name: "Times");
        }
    }
}
