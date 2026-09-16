using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyFactureSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE FactureLignes
                SET PrixUnitaireHT = CAST(
                    CAST(PrixUnitaireHT AS REAL)
                    * (1.0 - CAST(COALESCE(Remise, 0) AS REAL) / 100.0)
                    * (1.0 + CAST(COALESCE(TauxTVA, 0) AS REAL) / 100.0)
                    * (1.0 - CAST(COALESCE((
                        SELECT RemiseGlobale FROM Factures WHERE Id = FactureLignes.FactureId
                    ), 0) AS REAL) / 100.0)
                    AS TEXT);

                UPDATE Factures
                SET TotalTtc = CAST(COALESCE((
                    SELECT SUM(CAST(Quantite AS REAL) * CAST(PrixUnitaireHT AS REAL))
                    FROM FactureLignes
                    WHERE FactureId = Factures.Id
                ), 0) AS TEXT);
                """);

            migrationBuilder.DropColumn(
                name: "DateEcheance",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "EstPayee",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "RemiseGlobale",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "Remise",
                table: "FactureLignes");

            migrationBuilder.DropColumn(
                name: "TauxTVA",
                table: "FactureLignes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateEcheance",
                table: "Factures",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "EstPayee",
                table: "Factures",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Factures",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "RemiseGlobale",
                table: "Factures",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Remise",
                table: "FactureLignes",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxTVA",
                table: "FactureLignes",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
