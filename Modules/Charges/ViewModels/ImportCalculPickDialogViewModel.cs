using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public partial class ImportCalculPickDialogViewModel : ObservableObject
{
    private readonly Action<int?> _close;

    public ImportCalculPickDialogViewModel(
        string title,
        string lblEmpty,
        string btnCancel,
        string btnLoad,
        IEnumerable<ImportCalculPickItem> items,
        Action<int?> close)
    {
        Title = title;
        LblEmpty = lblEmpty;
        BtnCancel = btnCancel;
        BtnLoad = btnLoad;
        _close = close;
        foreach (var item in items)
            Items.Add(item);
        if (Items.Count > 0)
            SelectedItem = Items[0];
    }

    public string Title { get; }
    public string LblEmpty { get; }
    public string BtnCancel { get; }
    public string BtnLoad { get; }

    public ObservableCollection<ImportCalculPickItem> Items { get; } = [];

    [ObservableProperty] private ImportCalculPickItem? _selectedItem;

    [RelayCommand]
    private void Cancel() => _close(null);

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedItem is null)
            return;
        _close(SelectedItem.Id);
    }
}
