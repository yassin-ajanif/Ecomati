using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Charges.Models;

public class ImportCalculLigne : BaseEntity
{
    public int ImportCalculId { get; set; }
    public ImportCalcul? ImportCalcul { get; set; }
    public int Ordre { get; set; }
    public int ProduitId { get; set; }
    public Produit? Produit { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Pm { get; set; }
    public decimal Pc { get; set; }
    public decimal Rmb { get; set; }
    public decimal CntPs { get; set; }
    public decimal CntColis { get; set; }
    public decimal RmbGrosPrice { get; set; }
    public decimal LaDouane { get; set; }
    public decimal M { get; set; }
    public decimal Tm { get; set; }
    public decimal MNet { get; set; }
    public decimal Ca { get; set; }
    public decimal TMarge { get; set; }
    public List<ImportCalculLigneRmbFrais> RmbFrais { get; set; } = [];
}
