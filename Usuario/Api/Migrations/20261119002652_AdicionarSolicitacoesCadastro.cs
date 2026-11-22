using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Usuario.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarSolicitacoesCadastro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "solicitacoes_cadastro",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false, collation: "NOCASE"),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SenhaHash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UtilizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitacoes_cadastro", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_cadastro_Email",
                table: "solicitacoes_cadastro",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_cadastro_TokenHash",
                table: "solicitacoes_cadastro",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "solicitacoes_cadastro");
        }
    }
}
