using CommunityToolkit.Mvvm.ComponentModel;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public partial class ImportCostExpenseRow : ObservableObject
{
    [ObservableProperty] private string _libelle = string.Empty;
    [ObservableProperty] private decimal _montant;
}
