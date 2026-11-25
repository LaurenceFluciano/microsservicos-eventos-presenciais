using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class Alter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DataPagamento",
                table: "pagamentos",
                newName: "DataConfirmacao");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiraEm",
                table: "pedidos",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CobrancaGeradaEm",
                table: "pagamentos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdExterno",
                table: "pagamentos",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UrlCheckout",
                table: "pagamentos",
                type: "TEXT",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "pagamento_tentativas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PagamentoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    TentadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagamento_tentativas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pagamento_tentativas_pagamentos_PagamentoId",
                        column: x => x.PagamentoId,
                        principalTable: "pagamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pagamentos_IdExterno",
                table: "pagamentos",
                column: "IdExterno",
                unique: true,
                filter: "\"IdExterno\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_pagamento_tentativas_PagamentoId_TentadoEm",
                table: "pagamento_tentativas",
                columns: new[] { "PagamentoId", "TentadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pagamento_tentativas");

            migrationBuilder.DropIndex(
                name: "IX_pagamentos_IdExterno",
                table: "pagamentos");

            migrationBuilder.DropColumn(
                name: "CobrancaGeradaEm",
                table: "pagamentos");

            migrationBuilder.DropColumn(
                name: "IdExterno",
                table: "pagamentos");

            migrationBuilder.DropColumn(
                name: "UrlCheckout",
                table: "pagamentos");

            migrationBuilder.RenameColumn(
                name: "DataConfirmacao",
                table: "pagamentos",
                newName: "DataPagamento");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiraEm",
                table: "pedidos",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");
        }
    }
}
