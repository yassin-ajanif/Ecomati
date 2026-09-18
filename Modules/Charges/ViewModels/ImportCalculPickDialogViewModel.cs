using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public partial class ImportCalculPickDialogViewModel : ObservableObject
{
    private readonly Action<int?> _close;
    private readonly Func<ImportCalculPickItem, Task<bool>> _deleteAsync;

    public ImportCalculPickDialogViewModel(
        string title,
        string lblEmpty,
        string btnCancel,
        string btnLoad,
        string btnDelete,
        IEnumerable<ImportCalculPickItem> items,
        Action<int?> close,
        Func<ImportCalculPickItem, Task<bool>> deleteAsync)
    {
        Title = title;
        LblEmpty = lblEmpty;
        BtnCancel = btnCancel;
        BtnLoad = btnLoad;
        BtnDelete = btnDelete;
        _close = close;
        _deleteAsync = deleteAsync;
        foreach (var item in items)
            Items.Add(item);
        if (Items.Count > 0)
            SelectedItem = Items[0];
    }

    public string Title { get; }
    public string LblEmpty { get; }
    public string BtnCancel { get; }
    public string BtnLoad { get; }
    public string BtnDelete { get; }

    public ObservableCollection<ImportCalculPickItem> Items { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private ImportCalculPickItem? _selectedItem;

    private bool CanActOnSelection => SelectedItem is not null;

    [RelayCommand]
    private void Cancel() => _close(null);

    [RelayCommand(CanExecute = nameof(CanActOnSelection))]
    private void Confirm()
    {
        if (SelectedItem is null)
            return;
        _close(SelectedItem.Id);
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelection))]
    private async Task DeleteAsync()
    {
        if (SelectedItem is not ImportCalculPickItem item)
            return;

        if (!await _deleteAsync(item))
            return;

        var index = Items.IndexOf(item);
        Items.Remove(item);
        SelectedItem = Items.Count == 0
            ? null
            : Items[Math.Min(index, Items.Count - 1)];
    }
}
