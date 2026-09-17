using Avalonia.Controls;
using GestionCommerciale.Modules.Charges.ViewModels;

namespace GestionCommerciale.Modules.Charges.Views;

public partial class ImportRmbCoeffDialog : Window
{
    public ImportRmbCoeffDialog()
    {
        InitializeComponent();
    }

    public ImportRmbCoeffDialog(ImportRmbCoeffDialogViewModel vm) : this()
    {
        DataContext = vm;
    }
}
