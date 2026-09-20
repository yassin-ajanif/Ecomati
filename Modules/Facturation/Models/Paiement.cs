using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Facturation.Models;

public class Paiement : BaseEntity
{
    public int FactureId { get; set; }
    public Facture? Facture { get; set; }
    /// <summary>Set when this row is a slice of a handed amount stored on <see cref="ReglementGroupe"/>.</summary>
    public int? ReglementGroupeId { get; set; }
    public decimal Montant { get; set; }
    public DateTime Date { get; set; }
    public ModePaiement Mode { get; set; }
    public string Reference { get; set; } = string.Empty;
}
