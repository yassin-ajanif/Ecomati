using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class DropProduitTauxTva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE Produits
                SET PrixAchatHT = CAST(
                        CAST(PrixAchatHT AS REAL)
                        * (1.0 + CAST(COALESCE(TauxTVA, 0) AS REAL) / 100.0)
                        AS TEXT),
                    PrixVenteHT = CAST(
                        CAST(PrixVenteHT AS REAL)
                        * (1.0 + CAST(COALESCE(TauxTVA, 0) AS REAL) / 100.0)
                        AS TEXT);
                """);

            migrationBuilder.DropColumn(
                name: "TauxTVA",
                table: "Produits");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TauxTVA",
                table: "Produits",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
