using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Facturation.Models;

public class Avoir : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public int? FactureId { get; set; }
    public Facture? Facture { get; set; }
    public int ClientId { get; set; }
    public DateTime Date { get; set; }
    public bool RetourMarchandise { get; set; }
    public decimal TotalTtc { get; set; }
    public List<AvoirLigne> Lignes { get; set; } = [];
}
