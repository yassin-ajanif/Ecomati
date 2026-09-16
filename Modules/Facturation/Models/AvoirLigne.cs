using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Facturation.Models;

public class AvoirLigne : BaseEntity
{
    public int AvoirId { get; set; }
    public Avoir? Avoir { get; set; }
    public int? ProduitId { get; set; }
    public int? ServiceId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    public decimal PrixUnitaireHT { get; set; }
    /// <summary>Unit / packaging label (e.g. carton, pièce).</summary>
    public string Conditionnement { get; set; } = string.Empty;
}
