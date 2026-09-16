using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyAvoirSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalTtc",
                table: "Avoirs",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE AvoirLignes
                SET PrixUnitaireHT = CAST(
                    CAST(PrixUnitaireHT AS REAL)
                    * (1.0 - CAST(COALESCE(Remise, 0) AS REAL) / 100.0)
                    * (1.0 + CAST(COALESCE(TauxTVA, 0) AS REAL) / 100.0)
                    AS TEXT);

                UPDATE Avoirs
                SET TotalTtc = CAST(COALESCE((
                    SELECT SUM(CAST(Quantite AS REAL) * CAST(PrixUnitaireHT AS REAL))
                    FROM AvoirLignes
                    WHERE AvoirId = Avoirs.Id
                ), 0) AS TEXT);
                """);

            migrationBuilder.DropColumn(
                name: "Motif",
                table: "Avoirs");

            migrationBuilder.DropColumn(
                name: "Remise",
                table: "AvoirLignes");

            migrationBuilder.DropColumn(
                name: "TauxTVA",
                table: "AvoirLignes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalTtc",
                table: "Avoirs");

            migrationBuilder.AddColumn<string>(
                name: "Motif",
                table: "Avoirs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Remise",
                table: "AvoirLignes",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxTVA",
                table: "AvoirLignes",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
