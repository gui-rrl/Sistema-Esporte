using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaEsporte.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarIdaVolta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IdaVolta",
                table: "Torneios",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdaVolta",
                table: "Torneios");
        }
    }
}
