using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOfertaIngresso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ofertas_ingresso_EventoId",
                table: "ofertas_ingresso");

            migrationBuilder.RenameColumn(
                name: "EventoId",
                table: "ofertas_ingresso",
                newName: "IdOrganizador");

            migrationBuilder.AddColumn<Guid>(
                name: "IdEvento",
                table: "ofertas_ingresso",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ofertas_ingresso_IdEvento",
                table: "ofertas_ingresso",
                column: "IdEvento");

            migrationBuilder.CreateIndex(
                name: "IX_ofertas_ingresso_IdOrganizador_IdEvento",
                table: "ofertas_ingresso",
                columns: new[] { "IdOrganizador", "IdEvento" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ofertas_ingresso_IdEvento",
                table: "ofertas_ingresso");

            migrationBuilder.DropIndex(
                name: "IX_ofertas_ingresso_IdOrganizador_IdEvento",
                table: "ofertas_ingresso");

            migrationBuilder.DropColumn(
                name: "IdEvento",
                table: "ofertas_ingresso");

            migrationBuilder.RenameColumn(
                name: "IdOrganizador",
                table: "ofertas_ingresso",
                newName: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_ofertas_ingresso_EventoId",
                table: "ofertas_ingresso",
                column: "EventoId");
        }
    }
}
