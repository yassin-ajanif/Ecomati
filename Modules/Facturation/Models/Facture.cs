using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Facturation.Models;

public class Facture : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalTtc { get; set; }
    public List<FactureLigne> Lignes { get; set; } = [];
    public List<Paiement> Paiements { get; set; } = [];
}
