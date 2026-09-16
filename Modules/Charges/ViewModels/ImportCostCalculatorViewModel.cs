using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        _ = LoadDeviseAsync();
    }

    public ObservableCollection<ImportCostExpenseRow> Expenses { get; } = [];

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
