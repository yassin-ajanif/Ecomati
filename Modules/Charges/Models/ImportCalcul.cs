using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Charges.Models;

public class ImportCalcul : BaseEntity
{
    public string Libelle { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public string Devise { get; set; } = "DH";
    public string Note { get; set; } = string.Empty;
    public decimal TotalMNet { get; set; }
    public decimal TotalCa { get; set; }
    public decimal TotalTMarge { get; set; }
    public List<ImportCalculLigne> Lignes { get; set; } = [];
}
