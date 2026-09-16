using CommunityToolkit.Mvvm.ComponentModel;
using GestionCommerciale.Modules.Services.Models;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Helpers;

namespace GestionCommerciale.Modules.Facturation.ViewModels;

public partial class FactureLineRow : ObservableObject
{
    [ObservableProperty] private int? _produitId;
    [ObservableProperty] private int? _serviceId;
    [ObservableProperty] private string _designation = string.Empty;
    [ObservableProperty] private string _conditionnement = string.Empty;
    [ObservableProperty] private decimal _quantite = 1;
    [ObservableProperty] private decimal _prixUnitaireHt;

    public bool IsService => ServiceId is > 0;

    public decimal Montant => Quantite * PrixUnitaireHt;

    partial void OnQuantiteChanged(decimal value) => NotifyMontants();
    partial void OnPrixUnitaireHtChanged(decimal value) => NotifyMontants();

    public void ApplyCatalogProduct(Produit p)
    {
        ProduitId = p.Id;
        ServiceId = null;
        Designation = p.Designation;
        Conditionnement = p.Unite;
        PrixUnitaireHt = Math.Round(p.PrixVenteHT * (1 + p.TauxTVA / 100m), 2);
        NotifyMontants();
    }

    public void ApplyCatalogService(Service s)
    {
        ServiceId = s.Id;
        ProduitId = null;
        Designation = s.Designation;
        Conditionnement = s.Unite;
        PrixUnitaireHt = Math.Round(s.PrixVenteHT * (1 + s.TauxTVA / 100m), 2);
        NotifyMontants();
    }

    public void ApplyCatalogItem(DocumentCatalogItem item)
    {
        if (item.Kind == DocumentCatalogKind.Service)
        {
            ServiceId = item.Id;
            ProduitId = null;
        }
        else
        {
            ProduitId = item.Id;
            ServiceId = null;
        }

        Designation = item.Designation;
        Conditionnement = item.Unite;
        PrixUnitaireHt = item.PrixVenteTtc;
        NotifyMontants();
    }

    private void NotifyMontants() => OnPropertyChanged(nameof(Montant));
}
