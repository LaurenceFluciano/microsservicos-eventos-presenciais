using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credenciais_acesso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IngressoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataUtilizacao = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credenciais_acesso", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_credenciais_acesso_EventoId",
                table: "credenciais_acesso",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_credenciais_acesso_IngressoId",
                table: "credenciais_acesso",
                column: "IngressoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credenciais_acesso");
        }
    }
}
