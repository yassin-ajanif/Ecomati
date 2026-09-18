using Avalonia.Controls;
using GestionCommerciale.Modules.Charges.ViewModels;

namespace GestionCommerciale.Modules.Charges.Views;

public partial class ImportCalculPreviewDialog : Window
{
    public ImportCalculPreviewDialog()
    {
        InitializeComponent();
    }

    public ImportCalculPreviewDialog(ImportCalculPreviewDialogViewModel vm) : this()
    {
        DataContext = vm;
    }
}
