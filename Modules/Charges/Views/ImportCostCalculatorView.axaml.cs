using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GestionCommerciale.Modules.Charges.Views;

public partial class ImportCostCalculatorView : UserControl
{
    public ImportCostCalculatorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
