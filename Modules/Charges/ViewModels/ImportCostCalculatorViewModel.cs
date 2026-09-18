using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Window = Avalonia.Controls.Window;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Charges.Models;
using GestionCommerciale.Modules.Charges.Views;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Models.Pdf;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public partial class ImportCostCalculatorViewModel : BaseViewModel
{
    private readonly ILocaleService _locale;
    private readonly IAppSettingsService _settings;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IPdfService _pdf;
    private readonly IDialogService _dialog;

    public ImportCostCalculatorViewModel(
        ILocaleService locale,
        IAppSettingsService settings,
        IDbContextFactory<AppDbContext> dbFactory,
        IPdfService pdf,
        IDialogService dialog)
    {
        _locale = locale;
        _settings = settings;
        _dbFactory = dbFactory;
        _pdf = pdf;
        _dialog = dialog;
        _locale.CultureApplied += (_, _) => RefreshUi();
        Expenses.CollectionChanged += ExpensesOnCollectionChanged;
        TableRows.CollectionChanged += TableRowsOnCollectionChanged;
        RefreshUi();
        AddExpense();
        AddTableRow();
        _ = LoadDeviseAsync();
        _ = LoadProductsAsync();
    }

    public ObservableCollection<ImportCostExpenseRow> Expenses { get; } = [];
    public ObservableCollection<ImportCostTableRow> TableRows { get; } = [];
    public ObservableCollection<ImportProductPick> ProductCatalog { get; } = [];

    public AutoCompleteFilterPredicate<object?> ProductItemFilter { get; } = static (search, item) =>
    {
        if (item is not ImportProductPick p)
            return false;
        if (string.IsNullOrWhiteSpace(search))
            return true;
        return p.Designation.Contains(search, StringComparison.OrdinalIgnoreCase);
    };

    [ObservableProperty] private decimal _grosPrice;
    [ObservableProperty] private decimal _unitPriceRmb;
    [ObservableProperty] private decimal? _marketPrice;
    [ObservableProperty] private string _devise = "dh";
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
    [ObservableProperty] private string _btnAddAllProducts = string.Empty;
    [ObservableProperty] private string _wmProduct = string.Empty;
    [ObservableProperty] private string _tipProductImage = string.Empty;
    [ObservableProperty] private string _msgNoProduct = string.Empty;
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
    [ObservableProperty] private string _lblTotal = string.Empty;
    [ObservableProperty] private string _btnPdf = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;
    [ObservableProperty] private string _btnLoad = string.Empty;
    [ObservableProperty] private int? _importCalculId;
    [ObservableProperty] private string _libelle = string.Empty;
    [ObservableProperty] private decimal _totalMNet;
    [ObservableProperty] private decimal _totalCa;
    [ObservableProperty] private decimal _totalTMarge;
    [ObservableProperty] private string _totalMNetLabel = string.Empty;
    [ObservableProperty] private string _totalCaLabel = string.Empty;
    [ObservableProperty] private string _totalTMargeLabel = string.Empty;

    partial void OnGrosPriceChanged(decimal value) => Recalc();
    partial void OnUnitPriceRmbChanged(decimal value) => Recalc();
    partial void OnMarketPriceChanged(decimal? value) => Recalc();
    partial void OnDeviseChanged(string value)
    {
        UpdateResultLabels();
        UpdateTotalLabels();
    }

    private async Task LoadDeviseAsync()
    {
        var cfg = await _settings.GetAsync();
        var fromSettings = CurrencyHelper.FromSettings(cfg);
        Devise = CurrencyHelper.NormalizeImportDevise(
            string.IsNullOrWhiteSpace(fromSettings) ? "dh" : fromSettings);
    }

    private async Task LoadProductsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var list = await db.Produits.AsNoTracking()
            .Where(p => p.Actif)
            .OrderBy(p => p.Designation)
            .Select(p => new ImportProductPick
            {
                Id = p.Id,
                Designation = p.Designation,
                ImageData = p.ImageData
            })
            .ToListAsync();

        ProductCatalog.Clear();
        foreach (var p in list)
            ProductCatalog.Add(p);
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
        BtnAddAllProducts = _locale.T("Calc_AddAllProducts");
        WmProduct = _locale.T("Calc_WmProduct");
        TipProductImage = _locale.T("Calc_TipProductImage");
        MsgNoProduct = _locale.T("Calc_MsgNoProduct");
        ColReference = _locale.T("Calc_ColDesignation");
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
        LblTotal = _locale.T("Calc_Total");
        BtnPdf = _locale.T("Btn_Pdf");
        BtnSave = _locale.T("Btn_Save");
        BtnLoad = _locale.T("Calc_Load");
        UpdateTitleLabel();
        UpdateResultLabels();
    }

    private void UpdateTitleLabel()
    {
        Title = string.IsNullOrWhiteSpace(Libelle)
            ? _locale.T("Nav_ImportCalc")
            : $"{_locale.T("Nav_ImportCalc")} — {Libelle}";
    }

    private void TableRowsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            RecalcTableTotals();
            return;
        }

        if (e.OldItems != null)
        {
            foreach (ImportCostTableRow row in e.OldItems)
                row.PropertyChanged -= TableRowChanged;
        }
        if (e.NewItems != null)
        {
            foreach (ImportCostTableRow row in e.NewItems)
                row.PropertyChanged += TableRowChanged;
        }
        RecalcTableTotals();
    }

    private void TableRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is ImportCostTableRow row &&
            e.PropertyName is nameof(ImportCostTableRow.Pm)
                or nameof(ImportCostTableRow.Pc)
                or nameof(ImportCostTableRow.Rmb)
                or nameof(ImportCostTableRow.CntPs)
                or nameof(ImportCostTableRow.CntColis))
        {
            row.RecalcDerived();
        }

        if (e.PropertyName is nameof(ImportCostTableRow.MNet)
            or nameof(ImportCostTableRow.Ca)
            or nameof(ImportCostTableRow.TMarge)
            or null)
            RecalcTableTotals();
    }

    private void RecalcTableTotals()
    {
        TotalMNet = TableRows.Sum(r => r.MNet ?? 0);
        TotalCa = TableRows.Sum(r => r.Ca ?? 0);
        TotalTMarge = TableRows.Sum(r => r.TMarge ?? 0);
        UpdateTotalLabels();
    }

    private void UpdateTotalLabels()
    {
        TotalMNetLabel = CurrencyHelper.Format(TotalMNet, Devise);
        TotalCaLabel = CurrencyHelper.Format(TotalCa, Devise);
        TotalTMargeLabel = CurrencyHelper.Format(TotalTMarge, Devise);
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
        {
            row.Dispose();
            TableRows.Remove(row);
        }
        if (TableRows.Count == 0)
            AddTableRow();
    }

    [RelayCommand]
    private async Task AddAllProductsAsync()
    {
        if (ProductCatalog.Count == 0)
            await LoadProductsAsync();

        foreach (var row in TableRows.ToList())
        {
            row.PropertyChanged -= TableRowChanged;
            row.Dispose();
        }
        TableRows.Clear();

        foreach (var p in ProductCatalog)
        {
            var row = new ImportCostTableRow();
            row.ApplyProduct(p.Id, p.Designation, p.ImageData);
            TableRows.Add(row);
        }

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

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var rows = GetSavableRows();
        if (rows.Count == 0)
        {
            await _dialog.ShowErrorAsync(Title, _locale.T("Calc_SaveEmpty"), cancellationToken);
            return;
        }

        var incomplete = rows.FirstOrDefault(r => !IsRowComplete(r));
        if (incomplete is not null)
        {
            await _dialog.ShowErrorAsync(Title, _locale.T("Calc_SaveIncomplete"), cancellationToken);
            return;
        }

        var libelle = Libelle.Trim();
        if (ImportCalculId is null or 0)
        {
            var prompted = await _dialog.ShowPromptAsync(
                Title,
                _locale.T("Calc_SavePrompt"),
                $"Import {DateTime.Today:yyyy-MM-dd}",
                cancellationToken);
            if (string.IsNullOrWhiteSpace(prompted))
                return;
            libelle = prompted.Trim();
        }

        IsBusy = true;
        try
        {
            RecalcTableTotals();
            foreach (var row in rows)
                row.RecalcDerived();

            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            ImportCalcul entity;
            if (ImportCalculId is int id and > 0)
            {
                entity = await db.ImportCalculs
                    .Include(x => x.Lignes)
                    .ThenInclude(l => l.RmbFrais)
                    .FirstAsync(x => x.Id == id, cancellationToken);
                db.ImportCalculLigneRmbFrais.RemoveRange(entity.Lignes.SelectMany(l => l.RmbFrais));
                db.ImportCalculLignes.RemoveRange(entity.Lignes);
                entity.Lignes.Clear();
            }
            else
            {
                entity = new ImportCalcul();
                db.ImportCalculs.Add(entity);
            }

            entity.Libelle = libelle;
            entity.Date = DateTime.Today;
            entity.Devise = CurrencyHelper.NormalizeImportDevise(Devise);
            entity.TotalMNet = TotalMNet;
            entity.TotalCa = TotalCa;
            entity.TotalTMarge = TotalTMarge;

            var ordre = 0;
            foreach (var row in rows)
            {
                var ligne = MapRowToEntity(row, ordre++);
                entity.Lignes.Add(ligne);
            }

            await db.SaveChangesAsync(cancellationToken);
            ImportCalculId = entity.Id;
            Libelle = entity.Libelle;
            UpdateTitleLabel();
            await _dialog.ShowInfoAsync(Title, _locale.T("Calc_Saved"), cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de l'enregistrement du calcul import", ex, "ImportCostCalculatorViewModel.SaveAsync");
            await _dialog.ShowErrorAsync(Title, ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadSavedAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var list = await db.ImportCalculs.AsNoTracking()
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .Select(x => new ImportCalculPickItem
            {
                Id = x.Id,
                Libelle = x.Libelle,
                Date = x.Date,
                LineCount = x.Lignes.Count
            })
            .ToListAsync(cancellationToken);

        var owner = GetOwnerWindow();
        int? pickedId = null;
        var dialog = new ImportCalculPickDialog();
        var pickVm = new ImportCalculPickDialogViewModel(
            _locale.T("Calc_LoadTitle"),
            _locale.T("Calc_LoadEmpty"),
            _locale.T("Btn_Cancel"),
            _locale.T("Calc_Load"),
            _locale.T("Btn_Delete"),
            list,
            id =>
            {
                pickedId = id;
                dialog.Close();
            },
            item => DeleteImportCalculAsync(item, cancellationToken));
        dialog.DataContext = pickVm;
        if (owner != null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        if (pickedId is not int id)
            return;

        await ShowImportCalculPreviewAsync(id, cancellationToken);
    }

    private async Task<bool> DeleteImportCalculAsync(ImportCalculPickItem item, CancellationToken cancellationToken)
    {
        if (!await _dialog.ConfirmAsync(
                _locale.T("Calc_LoadTitle"),
                _locale.Tf("Calc_ConfirmDelete", item.Libelle),
                cancellationToken))
            return false;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var entity = await db.ImportCalculs.FindAsync([item.Id], cancellationToken);
            if (entity is null)
            {
                await _dialog.ShowErrorAsync(_locale.T("Calc_LoadTitle"), _locale.T("Calc_LoadNotFound"), cancellationToken);
                return false;
            }

            db.ImportCalculs.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);

            if (ImportCalculId == item.Id)
            {
                ImportCalculId = null;
                Libelle = string.Empty;
                UpdateTitleLabel();
                ClearTable();
            }

            await _dialog.ShowInfoAsync(_locale.T("Calc_LoadTitle"), _locale.T("Calc_Deleted"), cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de la suppression du calcul import", ex, "ImportCostCalculatorViewModel.DeleteImportCalculAsync");
            await _dialog.ShowErrorAsync(_locale.T("Calc_LoadTitle"), ex.Message, cancellationToken);
            return false;
        }
    }

    private async Task ShowImportCalculPreviewAsync(int id, CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ImportCalculs.AsNoTracking()
            .Include(x => x.Lignes.OrderBy(l => l.Ordre))
            .ThenInclude(l => l.Produit)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            await _dialog.ShowErrorAsync(Title, _locale.T("Calc_LoadNotFound"), cancellationToken);
            return;
        }

        var owner = GetOwnerWindow();
        var previewDialog = new ImportCalculPreviewDialog();
        var editRequested = false;
        var previewVm = ImportCalculPreviewDialogViewModel.Create(
            entity,
            _locale,
            _pdf,
            _dialog,
            previewDialog.Close,
            _ =>
            {
                editRequested = true;
            });
        previewDialog.DataContext = previewVm;

        if (owner != null)
            await previewDialog.ShowDialog(owner);
        else
            previewDialog.Show();

        if (editRequested)
            await LoadImportCalculAsync(id, cancellationToken);
    }

    public async Task LoadImportCalculAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ImportCalculs.AsNoTracking()
            .Include(x => x.Lignes.OrderBy(l => l.Ordre))
            .ThenInclude(l => l.RmbFrais.OrderBy(f => f.Ordre))
            .Include(x => x.Lignes)
            .ThenInclude(l => l.Produit)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            await _dialog.ShowErrorAsync(Title, _locale.T("Calc_LoadNotFound"), cancellationToken);
            return;
        }

        ClearTable();
        ImportCalculId = entity.Id;
        Libelle = entity.Libelle;
        Devise = CurrencyHelper.NormalizeImportDevise(entity.Devise);
        UpdateTitleLabel();

        foreach (var ligne in entity.Lignes.OrderBy(l => l.Ordre))
        {
            var row = new ImportCostTableRow();
            var image = ligne.Produit?.ImageData;
            row.ApplyProduct(ligne.ProduitId, ligne.Designation, image);
            row.Pm = ligne.Pm;
            row.Pc = ligne.Pc;
            row.Rmb = ligne.Rmb;
            row.CntPs = ligne.CntPs;
            row.CntColis = ligne.CntColis;
            row.RmbGrosPrice = ligne.RmbGrosPrice;
            row.RmbExpenses.Clear();
            foreach (var frais in ligne.RmbFrais.OrderBy(f => f.Ordre))
                row.RmbExpenses.Add(new ImportRmbExpenseMemory { Libelle = frais.Libelle, Montant = frais.Montant });
            row.RecalcDerived();
            TableRows.Add(row);
        }

        if (TableRows.Count == 0)
            AddTableRow();

        RecalcTableTotals();
    }

    private static ImportCalculLigne MapRowToEntity(ImportCostTableRow row, int ordre)
    {
        row.RecalcDerived();
        var ligne = new ImportCalculLigne
        {
            Ordre = ordre,
            ProduitId = row.ProduitId!.Value,
            Designation = row.Designation.Trim(),
            Pm = row.Pm!.Value,
            Pc = row.Pc!.Value,
            Rmb = row.Rmb!.Value,
            CntPs = row.CntPs!.Value,
            CntColis = row.CntColis!.Value,
            RmbGrosPrice = row.RmbGrosPrice ?? 0,
            LaDouane = row.LaDouane ?? 0,
            M = row.M ?? 0,
            Tm = row.Tm ?? 0,
            MNet = row.MNet ?? 0,
            Ca = row.Ca ?? 0,
            TMarge = row.TMarge ?? 0
        };

        var expenseOrdre = 0;
        foreach (var e in row.RmbExpenses)
        {
            ligne.RmbFrais.Add(new ImportCalculLigneRmbFrais
            {
                Ordre = expenseOrdre++,
                Libelle = e.Libelle.Trim(),
                Montant = e.Montant
            });
        }

        return ligne;
    }

    private List<ImportCostTableRow> GetSavableRows() =>
        TableRows.Where(r => !IsRowEmpty(r)).ToList();

    private static bool IsRowEmpty(ImportCostTableRow row) =>
        string.IsNullOrWhiteSpace(row.Designation)
        && !row.ProduitId.HasValue
        && !row.Pm.HasValue
        && !row.Pc.HasValue
        && !row.Rmb.HasValue
        && !row.CntPs.HasValue
        && !row.CntColis.HasValue;

    private static bool IsRowComplete(ImportCostTableRow row) =>
        row.ProduitId.HasValue
        && !string.IsNullOrWhiteSpace(row.Designation)
        && row.Pm.HasValue
        && row.Pc.HasValue
        && row.Rmb.HasValue
        && row.CntPs.HasValue
        && row.CntColis.HasValue;

    private void ClearTable()
    {
        foreach (var row in TableRows.ToList())
        {
            row.PropertyChanged -= TableRowChanged;
            row.Dispose();
        }
        TableRows.Clear();
        AddTableRow();
        RecalcTableTotals();
    }

    private static Window? GetOwnerWindow() =>
        Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

    [RelayCommand]
    private async Task ExportPdfAsync(CancellationToken cancellationToken)
    {
        try
        {
            IsBusy = true;
            var model = BuildImportTablePdfModel();
            if (model.Rows.Count == 0)
            {
                await _dialog.ShowInfoAsync(_locale.T("Export_Pdf"), _locale.T("Calc_PdfEmpty"), cancellationToken);
                return;
            }

            var bytes = await _pdf.BuildReportPdfAsync(model, cancellationToken);
            var fileName = $"import-calc-{DateTime.Today:yyyy-MM-dd}.pdf";
            var ok = await _dialog.SavePickedFileBytesAsync(
                _locale.T("Export_PdfPicker"), fileName, new[] { "*.pdf" }, bytes, cancellationToken);
            if (ok)
                await _dialog.ShowInfoAsync(_locale.T("Export_Pdf"), _locale.T("Export_Done"), cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de l'export PDF du tableau import", ex, "ImportCostCalculatorViewModel.ExportPdfAsync");
            await _dialog.ShowErrorAsync(_locale.T("Export_Pdf"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private ReportPdfModel BuildImportTablePdfModel()
    {
        var right = PdfTextAlignment.End;
        var source = TableRows
            .Where(r => !string.IsNullOrWhiteSpace(r.Designation)
                        || r.Pm.HasValue || r.Pc.HasValue || r.Rmb.HasValue
                        || r.CntPs.HasValue || r.CntColis.HasValue)
            .ToList();

        var rows = source
            .Select(r => new ReportPdfRow
            {
                Cells =
                [
                    r.Designation,
                    Fmt(r.Pm),
                    Fmt(r.Pc),
                    Fmt(r.Rmb),
                    Fmt(r.LaDouane),
                    Fmt(r.M),
                    Fmt0(r.CntPs),
                    Fmt(r.Tm),
                    Fmt0(r.CntColis),
                    Fmt(r.MNet),
                    Fmt(r.Ca),
                    Fmt(r.TMarge)
                ],
                CellImages = r.ProductImageData is { Length: > 0 }
                    ? new byte[]?[] { r.ProductImageData }
                    : null
            })
            .ToList();

        if (rows.Count > 0)
        {
            rows.Add(new ReportPdfRow
            {
                Cells =
                [
                    LblTotal,
                    "", "", "", "", "", "", "", "",
                    FmtTotal(TotalMNet),
                    FmtTotal(TotalCa),
                    FmtTotal(TotalTMarge)
                ]
            });
        }

        return new ReportPdfModel
        {
            Title = Title,
            PeriodLabel = $"{DateTime.Today:dd/MM/yyyy}  —  {Devise}",
            Columns =
            [
                new(ColReference, 4.5f),
                new(ColPm, 0.7f, right),
                new(ColPc, 0.7f, right),
                new(ColRmb, 0.7f, right),
                new(ColLaDouane, 0.8f, right),
                new(ColM, 0.7f, right),
                new(ColCntPs, 0.7f, right),
                new(ColTm, 0.8f, right),
                new(ColCntColis, 0.8f, right),
                new(ColMNet, 1f, right),
                new(ColCa, 1f, right),
                new(ColTMarge, 1f, right)
            ],
            Rows = rows,
            SummaryLines =
            [
                new(ColMNet, FmtTotal(TotalMNet), true),
                new(ColCa, FmtTotal(TotalCa), true),
                new(ColTMarge, FmtTotal(TotalTMarge), true)
            ],
            Landscape = true
        };
    }

    private static string Fmt(decimal? value) => value?.ToString("N2") ?? "";
    private static string Fmt0(decimal? value) => value?.ToString("N0") ?? "";
    private string FmtTotal(decimal value) => CurrencyHelper.Format(value, Devise);

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
