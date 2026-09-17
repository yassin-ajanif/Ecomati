using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public partial class ImportRmbCoeffDialogViewModel : ObservableObject
{
    private readonly ILocaleService _locale;
    private readonly Action<bool> _close;

    public ImportRmbCoeffDialogViewModel(
        ILocaleService locale,
        string devise,
        decimal initialGros,
        IEnumerable<(string Libelle, decimal Montant)> initialExpenses,
        Action<bool> close)
    {
        _locale = locale;
        _close = close;
        Devise = devise;
        GrosPrice = initialGros;
        foreach (var e in initialExpenses)
            Expenses.Add(new ImportCostExpenseRow { Libelle = e.Libelle, Montant = e.Montant });
        if (Expenses.Count == 0)
            AddExpense();
        Expenses.CollectionChanged += ExpensesOnCollectionChanged;
        foreach (var row in Expenses)
            row.PropertyChanged += ExpenseChanged;
        RefreshUi();
        Recalc();
    }

    public ObservableCollection<ImportCostExpenseRow> Expenses { get; } = [];

    [ObservableProperty] private decimal _grosPrice;
    [ObservableProperty] private decimal _sumExpenses;
    [ObservableProperty] private decimal _coefficient;
    [ObservableProperty] private string _devise = "MAD";
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _lblFormula = string.Empty;
    [ObservableProperty] private string _lblGros = string.Empty;
    [ObservableProperty] private string _lblExpenses = string.Empty;
    [ObservableProperty] private string _btnAddExpense = string.Empty;
    [ObservableProperty] private string _lblExpenseLabel = string.Empty;
    [ObservableProperty] private string _lblExpenseAmount = string.Empty;
    [ObservableProperty] private string _btnRemove = string.Empty;
    [ObservableProperty] private string _lblSumExpenses = string.Empty;
    [ObservableProperty] private string _lblCoefficient = string.Empty;
    [ObservableProperty] private string _btnOk = string.Empty;
    [ObservableProperty] private string _btnCancel = string.Empty;
    [ObservableProperty] private string _sumExpensesLabel = string.Empty;
    [ObservableProperty] private string _coefficientLabel = string.Empty;

    public bool Confirmed { get; private set; }

    partial void OnGrosPriceChanged(decimal value) => Recalc();

    private void RefreshUi()
    {
        Title = _locale.T("Calc_RmbDialogTitle");
        LblFormula = _locale.T("Calc_RmbDialogFormula");
        LblGros = _locale.T("Calc_Gros");
        LblExpenses = _locale.T("Calc_Expenses");
        BtnAddExpense = _locale.T("Calc_AddExpense");
        LblExpenseLabel = _locale.T("Calc_ExpenseLabel");
        LblExpenseAmount = _locale.T("Calc_ExpenseAmount");
        BtnRemove = _locale.T("Btn_RemoveLine");
        LblSumExpenses = _locale.T("Calc_SumExpenses");
        LblCoefficient = _locale.T("Calc_Coefficient");
        BtnOk = _locale.T("Btn_Ok");
        BtnCancel = _locale.T("Btn_Cancel");
    }

    private void ExpensesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (ImportCostExpenseRow row in e.NewItems)
                row.PropertyChanged += ExpenseChanged;
        if (e.OldItems != null)
            foreach (ImportCostExpenseRow row in e.OldItems)
                row.PropertyChanged -= ExpenseChanged;
        Recalc();
    }

    private void ExpenseChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ImportCostExpenseRow.Montant))
            Recalc();
    }

    [RelayCommand]
    private void AddExpense() => Expenses.Add(new ImportCostExpenseRow());

    [RelayCommand]
    private void RemoveExpense(ImportCostExpenseRow? row)
    {
        if (row != null)
            Expenses.Remove(row);
        if (Expenses.Count == 0)
            AddExpense();
    }

    [RelayCommand]
    private void Ok()
    {
        Confirmed = true;
        _close(true);
    }

    [RelayCommand]
    private void Cancel() => _close(false);

    private void Recalc()
    {
        var fraisOnly = Expenses.Sum(x => x.Montant);
        SumExpenses = GrosPrice + fraisOnly;
        Coefficient = GrosPrice > 0 ? SumExpenses / GrosPrice : 0;
        SumExpensesLabel = CurrencyHelper.Format(SumExpenses, Devise);
        CoefficientLabel = Coefficient.ToString("N2");
    }
}
