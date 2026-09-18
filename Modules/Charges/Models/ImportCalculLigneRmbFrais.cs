using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Charges.Models;

public class ImportCalculLigneRmbFrais : BaseEntity
{
    public int ImportCalculLigneId { get; set; }
    public ImportCalculLigne? Ligne { get; set; }
    public int Ordre { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public decimal Montant { get; set; }
}
