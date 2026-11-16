using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEventoSlugNumero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Eventos_Slug",
                table: "Eventos");

            migrationBuilder.AddColumn<int>(
                name: "SlugNumero",
                table: "Eventos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_Slug_SlugNumero",
                table: "Eventos",
                columns: new[] { "Slug", "SlugNumero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Eventos_Slug_SlugNumero",
                table: "Eventos");

            migrationBuilder.DropColumn(
                name: "SlugNumero",
                table: "Eventos");

            migrationBuilder.CreateIndex(
                name: "IX_Eventos_Slug",
                table: "Eventos",
                column: "Slug",
                unique: true);
        }
    }
}
