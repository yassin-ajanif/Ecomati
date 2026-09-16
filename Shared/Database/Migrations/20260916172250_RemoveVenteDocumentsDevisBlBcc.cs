using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVenteDocumentsDevisBlBcc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE MouvementsStock
                SET OrigineType = 'Facture',
                    OrigineId = (
                        SELECT FactureId FROM BonsLivraison
                        WHERE BonsLivraison.Id = MouvementsStock.OrigineId
                    )
                WHERE OrigineType = 'BL'
                  AND OrigineId IS NOT NULL
                  AND EXISTS (
                      SELECT 1 FROM BonsLivraison
                      WHERE BonsLivraison.Id = MouvementsStock.OrigineId
                        AND BonsLivraison.FactureId IS NOT NULL
                  );
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_FactureLignes_BonsLivraison_BonLivraisonId",
                table: "FactureLignes");

            migrationBuilder.DropTable(
                name: "BonCommandeClientLignes");

            migrationBuilder.DropTable(
                name: "BonLivraisonLignes");

            migrationBuilder.DropTable(
                name: "DevisConditions");

            migrationBuilder.DropTable(
                name: "DevisLignes");

            migrationBuilder.DropTable(
                name: "BonsLivraison");

            migrationBuilder.DropTable(
                name: "Devis");

            migrationBuilder.DropTable(
                name: "BonsCommandeClient");

            migrationBuilder.DropIndex(
                name: "IX_FactureLignes_BonLivraisonId",
                table: "FactureLignes");

            migrationBuilder.DropColumn(
                name: "BonCommandeReference",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "DevisId",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "BonLivraisonId",
                table: "FactureLignes");

            migrationBuilder.DropColumn(
                name: "DevisValiditeJoursDefaut",
                table: "AppSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BonCommandeReference",
                table: "Factures",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DevisId",
                table: "Factures",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BonLivraisonId",
                table: "FactureLignes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DevisValiditeJoursDefaut",
                table: "AppSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BonsCommandeClient",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FactureId = table.Column<int>(type: "INTEGER", nullable: true),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DevisId = table.Column<int>(type: "INTEGER", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    Numero = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonsCommandeClient", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonsCommandeClient_Factures_FactureId",
                        column: x => x.FactureId,
                        principalTable: "Factures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Devis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateValidite = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    Numero = table.Column<string>(type: "TEXT", nullable: false),
                    RemiseGlobale = table.Column<decimal>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BonCommandeClientLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BonCommandeClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Conditionnement = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Designation = table.Column<string>(type: "TEXT", nullable: false),
                    PrixUnitaireHT = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProduitId = table.Column<int>(type: "INTEGER", nullable: true),
                    QuantiteCommandee = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: true),
                    TauxTVA = table.Column<decimal>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonCommandeClientLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonCommandeClientLignes_BonsCommandeClient_BonCommandeClientId",
                        column: x => x.BonCommandeClientId,
                        principalTable: "BonsCommandeClient",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BonCommandeClientLignes_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BonsLivraison",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FactureId = table.Column<int>(type: "INTEGER", nullable: true),
                    BonCommandeClientId = table.Column<int>(type: "INTEGER", nullable: true),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DevisId = table.Column<int>(type: "INTEGER", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    Numero = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonsLivraison", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonsLivraison_BonsCommandeClient_BonCommandeClientId",
                        column: x => x.BonCommandeClientId,
                        principalTable: "BonsCommandeClient",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BonsLivraison_Factures_FactureId",
                        column: x => x.FactureId,
                        principalTable: "Factures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DevisConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DevisId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                    Titre = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Valeur = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevisConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevisConditions_Devis_DevisId",
                        column: x => x.DevisId,
                        principalTable: "Devis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DevisLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DevisId = table.Column<int>(type: "INTEGER", nullable: false),
                    Conditionnement = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Designation = table.Column<string>(type: "TEXT", nullable: false),
                    PrixUnitaireHT = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProduitId = table.Column<int>(type: "INTEGER", nullable: true),
                    Quantite = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: true),
                    TauxTVA = table.Column<decimal>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevisLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevisLignes_Devis_DevisId",
                        column: x => x.DevisId,
                        principalTable: "Devis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DevisLignes_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BonLivraisonLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BLId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Designation = table.Column<string>(type: "TEXT", nullable: false),
                    PrixUnitaireHT = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProduitId = table.Column<int>(type: "INTEGER", nullable: true),
                    QuantiteCommandee = table.Column<decimal>(type: "TEXT", nullable: false),
                    QuantiteLivree = table.Column<decimal>(type: "TEXT", nullable: false),
                    Remise = table.Column<decimal>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<int>(type: "INTEGER", nullable: true),
                    TauxTVA = table.Column<decimal>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonLivraisonLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonLivraisonLignes_BonsLivraison_BLId",
                        column: x => x.BLId,
                        principalTable: "BonsLivraison",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BonLivraisonLignes_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FactureLignes_BonLivraisonId",
                table: "FactureLignes",
                column: "BonLivraisonId");

            migrationBuilder.CreateIndex(
                name: "IX_BonCommandeClientLignes_BonCommandeClientId",
                table: "BonCommandeClientLignes",
                column: "BonCommandeClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BonCommandeClientLignes_ServiceId",
                table: "BonCommandeClientLignes",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BonLivraisonLignes_BLId",
                table: "BonLivraisonLignes",
                column: "BLId");

            migrationBuilder.CreateIndex(
                name: "IX_BonLivraisonLignes_ServiceId",
                table: "BonLivraisonLignes",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsCommandeClient_FactureId",
                table: "BonsCommandeClient",
                column: "FactureId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsLivraison_BonCommandeClientId",
                table: "BonsLivraison",
                column: "BonCommandeClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsLivraison_FactureId",
                table: "BonsLivraison",
                column: "FactureId");

            migrationBuilder.CreateIndex(
                name: "IX_DevisConditions_DevisId",
                table: "DevisConditions",
                column: "DevisId");

            migrationBuilder.CreateIndex(
                name: "IX_DevisLignes_DevisId",
                table: "DevisLignes",
                column: "DevisId");

            migrationBuilder.CreateIndex(
                name: "IX_DevisLignes_ServiceId",
                table: "DevisLignes",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_FactureLignes_BonsLivraison_BonLivraisonId",
                table: "FactureLignes",
                column: "BonLivraisonId",
                principalTable: "BonsLivraison",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
