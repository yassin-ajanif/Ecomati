using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddReglementsGroupes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReglementGroupeId",
                table: "Paiements",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReglementsGroupes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TiersId = table.Column<int>(type: "INTEGER", nullable: false),
                    Sens = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Montant = table.Column<decimal>(type: "TEXT", nullable: false),
                    Mode = table.Column<int>(type: "INTEGER", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglementsGroupes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Paiements_ReglementGroupeId",
                table: "Paiements",
                column: "ReglementGroupeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReglementsGroupes_TiersId",
                table: "ReglementsGroupes",
                column: "TiersId");

            migrationBuilder.CreateIndex(
                name: "IX_ReglementsGroupes_TiersId_Sens",
                table: "ReglementsGroupes",
                columns: new[] { "TiersId", "Sens" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReglementsGroupes");

            migrationBuilder.DropIndex(
                name: "IX_Paiements_ReglementGroupeId",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "ReglementGroupeId",
                table: "Paiements");
        }
    }
}
