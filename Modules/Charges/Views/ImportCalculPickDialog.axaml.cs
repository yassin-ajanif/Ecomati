using Avalonia.Controls;
using GestionCommerciale.Modules.Charges.ViewModels;

namespace GestionCommerciale.Modules.Charges.Views;

public partial class ImportCalculPickDialog : Window
{
    public ImportCalculPickDialog()
    {
        InitializeComponent();
    }

    public ImportCalculPickDialog(ImportCalculPickDialogViewModel vm) : this()
    {
        DataContext = vm;
    }
}
