using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Shared.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddImportCalculModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportCalculs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Libelle = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Devise = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: false),
                    TotalMNet = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalCa = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalTMarge = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportCalculs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportCalculLignes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImportCalculId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                    ProduitId = table.Column<int>(type: "INTEGER", nullable: false),
                    Designation = table.Column<string>(type: "TEXT", nullable: false),
                    Pm = table.Column<decimal>(type: "TEXT", nullable: false),
                    Pc = table.Column<decimal>(type: "TEXT", nullable: false),
                    Rmb = table.Column<decimal>(type: "TEXT", nullable: false),
                    CntPs = table.Column<decimal>(type: "TEXT", nullable: false),
                    CntColis = table.Column<decimal>(type: "TEXT", nullable: false),
                    RmbGrosPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    LaDouane = table.Column<decimal>(type: "TEXT", nullable: false),
                    M = table.Column<decimal>(type: "TEXT", nullable: false),
                    Tm = table.Column<decimal>(type: "TEXT", nullable: false),
                    MNet = table.Column<decimal>(type: "TEXT", nullable: false),
                    Ca = table.Column<decimal>(type: "TEXT", nullable: false),
                    TMarge = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportCalculLignes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportCalculLignes_ImportCalculs_ImportCalculId",
                        column: x => x.ImportCalculId,
                        principalTable: "ImportCalculs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportCalculLignes_Produits_ProduitId",
                        column: x => x.ProduitId,
                        principalTable: "Produits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportCalculLigneRmbFrais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImportCalculLigneId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordre = table.Column<int>(type: "INTEGER", nullable: false),
                    Libelle = table.Column<string>(type: "TEXT", nullable: false),
                    Montant = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportCalculLigneRmbFrais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportCalculLigneRmbFrais_ImportCalculLignes_ImportCalculLigneId",
                        column: x => x.ImportCalculLigneId,
                        principalTable: "ImportCalculLignes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportCalculLigneRmbFrais_ImportCalculLigneId",
                table: "ImportCalculLigneRmbFrais",
                column: "ImportCalculLigneId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportCalculLignes_ImportCalculId",
                table: "ImportCalculLignes",
                column: "ImportCalculId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportCalculLignes_ProduitId",
                table: "ImportCalculLignes",
                column: "ProduitId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportCalculs_Date",
                table: "ImportCalculs",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportCalculLigneRmbFrais");

            migrationBuilder.DropTable(
                name: "ImportCalculLignes");

            migrationBuilder.DropTable(
                name: "ImportCalculs");
        }
    }
}
