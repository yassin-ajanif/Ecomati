using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Charges.Views;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public partial class ImportCostCalculatorViewModel : BaseViewModel
{
    private readonly ILocaleService _locale;
    private readonly IAppSettingsService _settings;

    public ImportCostCalculatorViewModel(ILocaleService locale, IAppSettingsService settings)
    {
        _locale = locale;
        _settings = settings;
        _locale.CultureApplied += (_, _) => RefreshUi();
        Expenses.CollectionChanged += ExpensesOnCollectionChanged;
        RefreshUi();
        AddExpense();
        AddTableRow();
        _ = LoadDeviseAsync();
    }

    public ObservableCollection<ImportCostExpenseRow> Expenses { get; } = [];
    public ObservableCollection<ImportCostTableRow> TableRows { get; } = [];

    [ObservableProperty] private decimal _grosPrice;
    [ObservableProperty] private decimal _unitPriceRmb;
    [ObservableProperty] private decimal? _marketPrice;
    [ObservableProperty] private string _devise = "MAD";
    public string RmbCurrency => "RMB";

    [ObservableProperty] private decimal _sumExpenses;
    [ObservableProperty] private decimal _coefficient;
    [ObservableProperty] private decimal _productPrice;
    [ObservableProperty] private decimal _margin;
    [ObservableProperty] private bool _hasMargin;

    [ObservableProperty] private string _lblHelp = string.Empty;
    [ObservableProperty] private string _lblFormula = string.Empty;
    [ObservableProperty] private string _lblFormulaProduct = string.Empty;
    [ObservableProperty] private string _lblGros = string.Empty;
    [ObservableProperty] private string _lblRmb = string.Empty;
    [ObservableProperty] private string _lblCoefficient = string.Empty;
    [ObservableProperty] private string _lblExpenses = string.Empty;
    [ObservableProperty] private string _btnAddExpense = string.Empty;
    [ObservableProperty] private string _lblExpenseLabel = string.Empty;
    [ObservableProperty] private string _lblExpenseAmount = string.Empty;
    [ObservableProperty] private string _btnRemove = string.Empty;
    [ObservableProperty] private string _lblSumExpenses = string.Empty;
    [ObservableProperty] private string _lblMarketPrice = string.Empty;
    [ObservableProperty] private string _lblProductPrice = string.Empty;
    [ObservableProperty] private string _lblMargin = string.Empty;
    [ObservableProperty] private string _sumExpensesLabel = string.Empty;
    [ObservableProperty] private string _coefficientLabel = string.Empty;
    [ObservableProperty] private string _productPriceLabel = string.Empty;
    [ObservableProperty] private string _marginLabel = string.Empty;
    [ObservableProperty] private string _lblTableTitle = string.Empty;
    [ObservableProperty] private string _btnAddRow = string.Empty;
    [ObservableProperty] private string _colReference = string.Empty;
    [ObservableProperty] private string _colPm = string.Empty;
    [ObservableProperty] private string _colPc = string.Empty;
    [ObservableProperty] private string _colRmb = string.Empty;
    [ObservableProperty] private string _colLaDouane = string.Empty;
    [ObservableProperty] private string _colM = string.Empty;
    [ObservableProperty] private string _colCntPs = string.Empty;
    [ObservableProperty] private string _colTm = string.Empty;
    [ObservableProperty] private string _colCntColis = string.Empty;
    [ObservableProperty] private string _colMNet = string.Empty;
    [ObservableProperty] private string _colCa = string.Empty;
    [ObservableProperty] private string _colTMarge = string.Empty;
    [ObservableProperty] private string _tipCalcRmb = string.Empty;

    partial void OnGrosPriceChanged(decimal value) => Recalc();
    partial void OnUnitPriceRmbChanged(decimal value) => Recalc();
    partial void OnMarketPriceChanged(decimal? value) => Recalc();
    partial void OnDeviseChanged(string value) => UpdateResultLabels();

    private async Task LoadDeviseAsync()
    {
        var cfg = await _settings.GetAsync();
        var fromSettings = CurrencyHelper.FromSettings(cfg);
        Devise = string.IsNullOrWhiteSpace(fromSettings) ? "MAD" : fromSettings;
    }

    private void RefreshUi()
    {
        Title = _locale.T("Nav_ImportCalc");
        LblHelp = _locale.T("Calc_Help");
        LblFormula = _locale.T("Calc_Formula");
        LblFormulaProduct = _locale.T("Calc_FormulaProduct");
        LblGros = _locale.T("Calc_Gros");
        LblRmb = _locale.T("Calc_Rmb");
        LblCoefficient = _locale.T("Calc_Coefficient");
        LblExpenses = _locale.T("Calc_Expenses");
        BtnAddExpense = _locale.T("Calc_AddExpense");
        LblExpenseLabel = _locale.T("Calc_ExpenseLabel");
        LblExpenseAmount = _locale.T("Calc_ExpenseAmount");
        BtnRemove = _locale.T("Btn_RemoveLine");
        LblSumExpenses = _locale.T("Calc_SumExpenses");
        LblMarketPrice = _locale.T("Calc_MarketPrice");
        LblProductPrice = _locale.T("Calc_ProductPrice");
        LblMargin = _locale.T("Calc_Margin");
        LblTableTitle = _locale.T("Calc_TableTitle");
        BtnAddRow = _locale.T("Calc_AddRow");
        ColReference = _locale.T("Calc_ColReference");
        ColPm = _locale.T("Calc_ColPm");
        ColPc = _locale.T("Calc_ColPc");
        ColRmb = _locale.T("Calc_ColRmb");
        ColLaDouane = _locale.T("Calc_ColLaDouane");
        ColM = _locale.T("Calc_ColM");
        ColCntPs = _locale.T("Calc_ColCntPs");
        ColTm = _locale.T("Calc_ColTm");
        ColCntColis = _locale.T("Calc_ColCntColis");
        ColMNet = _locale.T("Calc_ColMNet");
        ColCa = _locale.T("Calc_ColCa");
        ColTMarge = _locale.T("Calc_ColTMarge");
        TipCalcRmb = _locale.T("Calc_CalcRmb");
        UpdateResultLabels();
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
    private void AddTableRow() => TableRows.Add(new ImportCostTableRow());

    [RelayCommand]
    private void RemoveTableRow(ImportCostTableRow? row)
    {
        if (row != null)
            TableRows.Remove(row);
        if (TableRows.Count == 0)
            AddTableRow();
    }

    [RelayCommand]
    private async Task CalculateRmbAsync(ImportCostTableRow? row)
    {
        if (row is null)
            return;

        var owner = Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        var initialGros = row.HasRmbMemory ? (row.RmbGrosPrice ?? 0) : GrosPrice;
        var initialExpenses = row.HasRmbMemory
            ? row.RmbExpenses.Select(e => (e.Libelle, e.Montant))
            : Expenses.Select(e => (e.Libelle, e.Montant));

        var dialog = new ImportRmbCoeffDialog();
        var vm = new ImportRmbCoeffDialogViewModel(
            _locale,
            Devise,
            initialGros,
            initialExpenses,
            _ => dialog.Close());
        dialog.DataContext = vm;

        if (owner != null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        if (!vm.Confirmed)
            return;

        row.Rmb = vm.Coefficient;
        row.RmbGrosPrice = vm.GrosPrice;
        row.RmbExpenses.Clear();
        foreach (var e in vm.Expenses)
            row.RmbExpenses.Add(new ImportRmbExpenseMemory { Libelle = e.Libelle, Montant = e.Montant });
    }

    private void Recalc()
    {
        SumExpenses = GrosPrice + Expenses.Sum(x => x.Montant);
        Coefficient = GrosPrice > 0 ? SumExpenses / GrosPrice : 0;
        ProductPrice = Coefficient * UnitPriceRmb;
        HasMargin = MarketPrice.HasValue;
        Margin = MarketPrice is decimal mp ? mp - ProductPrice : 0;
        UpdateResultLabels();
    }

    private void UpdateResultLabels()
    {
        SumExpensesLabel = CurrencyHelper.Format(SumExpenses, Devise);
        CoefficientLabel = Coefficient.ToString("N2");
        ProductPriceLabel = CurrencyHelper.Format(ProductPrice, Devise);
        MarginLabel = CurrencyHelper.Format(Margin, Devise);
    }
}
