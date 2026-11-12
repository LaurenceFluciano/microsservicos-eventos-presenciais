using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Comercializacao.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ofertas_ingresso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Preco = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    QuantidadeTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ofertas_ingresso", x => x.Id);
                    table.CheckConstraint("CK_Oferta_Preco", "Preco >= 0");
                    table.CheckConstraint("CK_Oferta_Quantidade", "QuantidadeTotal > 0");
                });

            migrationBuilder.CreateTable(
                name: "pedidos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompradorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfertaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantidade = table.Column<int>(type: "INTEGER", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiraEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedidos", x => x.Id);
                    table.CheckConstraint("CK_Pedido_Quantidade", "Quantidade > 0");
                    table.CheckConstraint("CK_Pedido_ValorTotal", "ValorTotal >= 0");
                    table.CheckConstraint("CK_Pedido_ValorUnitario", "ValorUnitario >= 0");
                    table.ForeignKey(
                        name: "FK_pedidos_ofertas_ingresso_OfertaId",
                        column: x => x.OfertaId,
                        principalTable: "ofertas_ingresso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ingressos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PedidoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodigoIngresso = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EmitidoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingressos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ingressos_pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ingressos_CodigoIngresso",
                table: "ingressos",
                column: "CodigoIngresso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ingressos_PedidoId",
                table: "ingressos",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_ofertas_ingresso_EventoId",
                table: "ofertas_ingresso",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_CompradorId",
                table: "pedidos",
                column: "CompradorId");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_OfertaId",
                table: "pedidos",
                column: "OfertaId");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_Status",
                table: "pedidos",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ingressos");

            migrationBuilder.DropTable(
                name: "pedidos");

            migrationBuilder.DropTable(
                name: "ofertas_ingresso");
        }
    }
}
