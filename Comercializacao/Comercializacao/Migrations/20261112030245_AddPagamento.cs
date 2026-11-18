using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comercializacao.Migrations
{
    /// <inheritdoc />
    public partial class AddPagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pagamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DataPagamento = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pagamentos_pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pagamentos_PedidoId",
                table: "pagamentos",
                column: "PedidoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pagamentos");
        }
    }
}
