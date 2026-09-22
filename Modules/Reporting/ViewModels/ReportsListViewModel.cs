using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Reporting.Services;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Tiers.Models;
using TiersEntity = GestionCommerciale.Modules.Tiers.Models.Tiers;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Models.Pdf;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Reporting.ViewModels;

public partial class ReportsListViewModel : BaseViewModel
{
    private readonly IReportService _reportService;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly IPdfService _pdf;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ReportsListViewModel(
        IReportService reportService,
        IDialogService dialog,
        ICurrentUserSession session,
        ILocaleService locale,
        IPdfService pdf,
        IDbContextFactory<AppDbContext> dbFactory)
    {
        _reportService = reportService;
        _dialog = dialog;
        _session = session;
        _locale = locale;
        _pdf = pdf;
        _dbFactory = dbFactory;
        _locale.CultureApplied += (_, _) => RefreshLabels();
        Pagination = new PaginationHelper(ApplyCurrentPage);
        DatePresets = new DatePresetChipsModel(_locale, (from, to) =>
        {
            DateFrom = new DateTimeOffset(from);
            DateTo = new DateTimeOffset(to);
            LoadReportCommand.Execute(null);
        });
        DatePresets.SyncSelection(DateFrom.Date, DateTo.Date);
        ShowProfitCharges = true;
        RefreshLabels();
        Title = _locale.T("Reports_Title");
    }

    public PaginationHelper Pagination { get; }
    public DatePresetChipsModel DatePresets { get; }

    [ObservableProperty] private string _lblTitle = string.Empty;
    [ObservableProperty] private string _lblDateFrom = string.Empty;
    [ObservableProperty] private string _lblDateTo = string.Empty;
    [ObservableProperty] private string _lblApply = string.Empty;
    [ObservableProperty] private string _lblLoading = string.Empty;

    [ObservableProperty] private string _btnSaleByProduct = string.Empty;
    [ObservableProperty] private string _btnSaleByCustomer = string.Empty;
    [ObservableProperty] private string _btnRefunds = string.Empty;
    [ObservableProperty] private string _btnDailySales = string.Empty;
    [ObservableProperty] private string _btnLowStock = string.Empty;
    [ObservableProperty] private string _btnStockMovements = string.Empty;
    [ObservableProperty] private string _btnProfitCharges = string.Empty;
    [ObservableProperty] private string _btnZakat = string.Empty;
    [ObservableProperty] private string _btnPdf = string.Empty;
    [ObservableProperty] private string _wmSaleByCustomerClient = string.Empty;
    [ObservableProperty] private string _lblSaleByCustomerClient = string.Empty;
    [ObservableProperty] private string _btnClearSaleByCustomerClient = string.Empty;
    [ObservableProperty] private string _colCustomerQty = string.Empty;
    [ObservableProperty] private string _colCustomerUnitPrice = string.Empty;
    [ObservableProperty] private string _colCustomerMarginPct = string.Empty;
    [ObservableProperty] private TiersEntity? _selectedSaleByCustomerClient;

    [ObservableProperty] private int _selectedReportIndex;
    [ObservableProperty] private DateTimeOffset _dateFrom = new(DateTime.Today);
    [ObservableProperty] private DateTimeOffset _dateTo = new(DateTime.Today);

    // visible columns for each report — used in view
    [ObservableProperty] private bool _showSaleByProduct;
    [ObservableProperty] private bool _showSaleByCustomer;
    [ObservableProperty] private bool _showRefunds;
    [ObservableProperty] private bool _showDailySales;
    [ObservableProperty] private bool _showLowStock;
    [ObservableProperty] private bool _showStockMovements;
    [ObservableProperty] private bool _showProfitCharges;
    [ObservableProperty] private bool _showZakat;

    [ObservableProperty] private bool _showEmpty;
    [ObservableProperty] private bool _showDateFilter = true;
    [ObservableProperty] private string _emptyMessage = string.Empty;
    [ObservableProperty] private string _lblSaleByCustomerTotalHt = string.Empty;
    [ObservableProperty] private string _lblSaleByCustomerTotalTtc = string.Empty;
    [ObservableProperty] private string _lblSaleByCustomerLabelHt = string.Empty;
    [ObservableProperty] private string _lblSaleByCustomerLabelTtc = string.Empty;
    [ObservableProperty] private string _lblSaleByCustomerLabelProfit = string.Empty;
    [ObservableProperty] private string _lblSaleByCustomerTotalProfit = string.Empty;
    [ObservableProperty] private string _lblDailySalesTotalTtc = string.Empty;
    [ObservableProperty] private string _lblDailySalesTotalProfit = string.Empty;
    [ObservableProperty] private string _lblStockValHtLabel = string.Empty;
    [ObservableProperty] private string _lblStockValTtcLabel = string.Empty;
    [ObservableProperty] private string _lblStockValHt = string.Empty;
    [ObservableProperty] private string _lblStockValTtc = string.Empty;
    [ObservableProperty] private string _lblProfitChargesTotalMargin = string.Empty;
    [ObservableProperty] private string _lblProfitChargesTotalVente = string.Empty;
    [ObservableProperty] private string _lblProfitChargesTotalAvoirsClient = string.Empty;
    [ObservableProperty] private string _lblProfitChargesTotalPurchases = string.Empty;
    [ObservableProperty] private string _lblProfitChargesTotalAvoirsFournisseur = string.Empty;
    [ObservableProperty] private string _lblProfitChargesTotalCharges = string.Empty;
    [ObservableProperty] private string _lblProfitChargesNetResult = string.Empty;
    [ObservableProperty] private bool _isNetPositive = true;
    [ObservableProperty] private string _lblProfitChargesMarginLabel = string.Empty;
    [ObservableProperty] private string _lblProfitChargesVenteLabel = string.Empty;
    [ObservableProperty] private string _lblProfitChargesAvoirsClientLabel = string.Empty;
    [ObservableProperty] private string _lblProfitChargesPurchasesLabel = string.Empty;
    [ObservableProperty] private string _lblProfitChargesAvoirsFournisseurLabel = string.Empty;
    [ObservableProperty] private string _lblProfitChargesChargesLabel = string.Empty;
    [ObservableProperty] private string _lblProfitChargesNetLabel = string.Empty;
    [ObservableProperty] private string _colProfitType = string.Empty;
    [ObservableProperty] private string _colProfitRef = string.Empty;
    [ObservableProperty] private string _colProfitDate = string.Empty;
    [ObservableProperty] private string _colProfitHt = string.Empty;
    [ObservableProperty] private string _colProfitAmount = string.Empty;
    [ObservableProperty] private string _colLowStockRef = string.Empty;
    [ObservableProperty] private string _colLowStockName = string.Empty;
    [ObservableProperty] private string _colLowStockActual = string.Empty;
    [ObservableProperty] private string _colLowStockMin = string.Empty;
    [ObservableProperty] private string _colZakatClient = string.Empty;
    [ObservableProperty] private string _colZakatBalance = string.Empty;
    [ObservableProperty] private string _lblZakatTotalBalancesLabel = string.Empty;
    [ObservableProperty] private string _lblZakatStockHtLabel = string.Empty;
    [ObservableProperty] private string _lblZakatBaseLabel = string.Empty;
    [ObservableProperty] private string _lblZakatAmountLabel = string.Empty;
    [ObservableProperty] private string _lblZakatTotalBalances = string.Empty;
    [ObservableProperty] private string _lblZakatStockHt = string.Empty;
    [ObservableProperty] private string _lblZakatBase = string.Empty;
    [ObservableProperty] private string _lblZakatAmount = string.Empty;
    [ObservableProperty] private bool _showPagination;
    [ObservableProperty] private bool _isProfitFilterMarginActive;
    [ObservableProperty] private bool _isProfitFilterAvoirsClientActive;
    [ObservableProperty] private bool _isProfitFilterPurchasesActive;
    [ObservableProperty] private bool _isProfitFilterAvoirsFournisseurActive;
    [ObservableProperty] private bool _isProfitFilterChargesActive;
    [ObservableProperty] private bool _isProfitFilterAllActive = true;

    private List<ReportSaleByProductRow> _allSalesByProduct = [];
    private List<ReportSaleByCustomerRow> _allSalesByCustomer = [];
    private List<ReportSaleByCustomerRow> _filteredSalesByCustomer = [];
    private bool _clientsLoaded;
    private List<ReportRefundRow> _allRefunds = [];
    private List<ReportDailySaleRow> _allDailySales = [];
    private List<ReportLowStockRow> _allLowStock = [];
    private List<ReportStockMovementRow> _allStockMovements = [];
    private List<ReportProfitChargeRow> _allProfitCharges = [];
    private List<ReportProfitChargeRow> _filteredProfitCharges = [];
    private List<ReportZakatClientRow> _allZakatClients = [];
    private ReportProfitChargesResult? _lastProfitCharges;
    private ReportProfitChargeKind? _profitFilterKind;

    public ObservableCollection<ReportSaleByProductRow> SalesByProduct { get; } = [];
    public ObservableCollection<ReportSaleByCustomerRow> SalesByCustomer { get; } = [];
    public ObservableCollection<TiersEntity> SaleByCustomerClients { get; } = [];
    public AutoCompleteFilterPredicate<object?> PartyAutocompleteFilter => PartyAutoComplete.ItemFilter;
    public ObservableCollection<ReportRefundRow> Refunds { get; } = [];
    public ObservableCollection<ReportDailySaleRow> DailySales { get; } = [];
    public ObservableCollection<ReportLowStockRow> LowStockProducts { get; } = [];
    public ObservableCollection<ReportStockMovementRow> StockMovements { get; } = [];
    public ObservableCollection<ReportProfitChargeRow> ProfitCharges { get; } = [];
    public ObservableCollection<ReportZakatClientRow> ZakatClients { get; } = [];

    private void RefreshLabels()
    {
        Title = _locale.T("Reports_Title");
        LblTitle = _locale.T("Reports_Title");
        LblDateFrom = _locale.T("Reports_From");
        LblDateTo = _locale.T("Reports_To");
        LblApply = _locale.T("Reports_Apply");
        LblLoading = _locale.T("Report_Loading");
        BtnSaleByProduct = _locale.T("Reports_BtnSaleByProduct");
        BtnSaleByCustomer = _locale.T("Reports_BtnSaleByCustomer");
        BtnRefunds = _locale.T("Reports_BtnRefunds");
        BtnDailySales = _locale.T("Reports_BtnDailySales");
        BtnLowStock = _locale.T("Reports_BtnLowStock");
        ColLowStockRef = _locale.T("Lbl_ColRef");
        ColLowStockName = _locale.T("Lbl_ColDesignation");
        ColLowStockActual = _locale.T("Reports_ColStockActuel");
        ColLowStockMin = _locale.T("Lbl_StockMinField");
        BtnStockMovements = _locale.T("Reports_BtnStockMovements");
        BtnProfitCharges = _locale.T("Reports_BtnProfitCharges");
        BtnZakat = _locale.T("Reports_BtnZakat");
        BtnPdf = _locale.T("Btn_Pdf");
        WmSaleByCustomerClient = _locale.T("Wm_SearchClient");
        LblSaleByCustomerClient = _locale.T("Lbl_Client");
        BtnClearSaleByCustomerClient = _locale.T("Reports_BtnClearClient");
        ColCustomerQty = _locale.T("Lbl_Quantity");
        ColCustomerUnitPrice = _locale.T("Reports_ColUnitPrice");
        ColCustomerMarginPct = _locale.T("Reports_ColMarginPct");
        EmptyMessage = _locale.T("Reports_Empty");
        LblSaleByCustomerLabelHt = _locale.T("Reports_LblTotalHt");
        LblSaleByCustomerLabelTtc = _locale.T("Reports_LblTotalTtc");
        LblSaleByCustomerLabelProfit = _locale.T("Reports_LblTotalProfit");
        LblStockValHtLabel = _locale.T("Reports_LblStockValHt");
        LblStockValTtcLabel = _locale.T("Reports_LblStockValTtc");
        LblProfitChargesMarginLabel = _locale.T("Reports_LblTotalSalesMargin");
        LblProfitChargesVenteLabel = _locale.T("Reports_LblTotalSales");
        LblProfitChargesAvoirsClientLabel = _locale.T("Reports_LblTotalAvoirsClient");
        LblProfitChargesPurchasesLabel = _locale.T("Reports_LblTotalPurchases");
        LblProfitChargesAvoirsFournisseurLabel = _locale.T("Reports_LblTotalAvoirsFournisseur");
        LblProfitChargesChargesLabel = _locale.T("Reports_LblTotalCharges");
        LblProfitChargesNetLabel = _locale.T("Reports_LblNetResult");
        ColProfitType = _locale.T("Reports_ColType");
        ColProfitRef = _locale.T("Reports_ColRefLibelle");
        ColProfitDate = _locale.T("DevisList_ColDate");
        ColProfitHt = _locale.T("Reports_LblTotalTtc");
        ColProfitAmount = _locale.T("Reports_ColMarginCharge");
        ColZakatClient = _locale.T("Lbl_Client");
        ColZakatBalance = _locale.T("ClientLedger_ColBalance");
        LblZakatTotalBalancesLabel = _locale.T("Reports_LblZakatTotalBalances");
        LblZakatStockHtLabel = _locale.T("Reports_LblStockValHt");
        LblZakatBaseLabel = _locale.T("Reports_LblZakatBase");
        LblZakatAmountLabel = _locale.T("Reports_LblZakatAmount");
    }

    partial void OnSelectedReportIndexChanged(int value)
    {
        ShowProfitCharges = value == 0;
        ShowSaleByProduct = value == 1;
        ShowSaleByCustomer = value == 2;
        ShowRefunds = value == 3;
        ShowDailySales = value == 4;
        ShowLowStock = value == 5;
        ShowStockMovements = value == 6;
        ShowZakat = value == 7;
        ShowDateFilter = value != 5;
        EmptyMessage = value == 5
            ? _locale.T("Report_EmptyStock")
            : _locale.T("Reports_Empty");
        LoadReportCommand.Execute(null);
    }

    partial void OnDateFromChanged(DateTimeOffset value) =>
        DatePresets.SyncSelection(value.Date, DateTo.Date);

    partial void OnDateToChanged(DateTimeOffset value) =>
        DatePresets.SyncSelection(DateFrom.Date, value.Date);

    [RelayCommand]
    private void GoProfitCharges()
    {
        if (SelectedReportIndex != 0)
            SelectedReportIndex = 0;
        else
            LoadReportCommand.Execute(null);
    }
    [RelayCommand] private void GoSaleByProduct() => SelectedReportIndex = 1;
    [RelayCommand] private void GoSaleByCustomer() => SelectedReportIndex = 2;
    [RelayCommand] private void GoRefunds() => SelectedReportIndex = 3;
    [RelayCommand] private void GoDailySales() => SelectedReportIndex = 4;
    [RelayCommand] private void GoLowStock() => SelectedReportIndex = 5;
    [RelayCommand] private void GoStockMovements() => SelectedReportIndex = 6;
    [RelayCommand] private void GoZakat() => SelectedReportIndex = 7;

    [RelayCommand]
    private void ToggleCustomerExpand(ReportSaleByCustomerRow? row)
    {
        if (row != null)
            row.IsExpanded = !row.IsExpanded;
    }

    [RelayCommand]
    private void ToggleCustomerDayExpand(ReportSaleByCustomerDayRow? row)
    {
        if (row != null)
            row.IsExpanded = !row.IsExpanded;
    }

    [RelayCommand]
    private void ToggleDailyExpand(ReportDailySaleRow? row)
    {
        if (row != null)
            row.IsExpanded = !row.IsExpanded;
    }

    [RelayCommand]
    private void ToggleDailyInvoiceExpand(ReportDailySaleDetailRow? row)
    {
        if (row != null)
            row.IsExpanded = !row.IsExpanded;
    }

    [RelayCommand]
    private async Task LoadReportAsync(CancellationToken cancellationToken)
    {
        if (!_session.CanAccessReporting)
        {
            await _dialog.ShowErrorAsync(_locale.T("Report_Title"), _locale.T("Report_ErrDenied"), cancellationToken);
            return;
        }

        IsBusy = true;
        ShowEmpty = false;
        try
        {
            await Task.Yield();

            var from = DateFrom.Date;
            var to = DateTo.Date;

            switch (SelectedReportIndex)
            {
                case 0:
                    await LoadProfitChargesAsync(from, to, cancellationToken);
                    break;
                case 1:
                    await LoadSalesByProductAsync(from, to, cancellationToken);
                    break;
                case 2:
                    await LoadSalesByCustomerAsync(from, to, cancellationToken);
                    break;
                case 3:
                    await LoadRefundsAsync(from, to, cancellationToken);
                    break;
                case 4:
                    await LoadDailySalesAsync(from, to, cancellationToken);
                    break;
                case 5:
                    await LoadLowStockAsync(cancellationToken);
                    break;
                case 6:
                    await LoadStockMovementsAsync(from, to, cancellationToken);
                    break;
                case 7:
                    await LoadZakatAsync(from, to, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec du chargement du rapport", ex, "ReportsListViewModel.LoadReportAsync");
            await _dialog.ShowErrorAsync(_locale.T("Report_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSalesByProductAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        _allSalesByProduct = await Task.Run(() => _reportService.GetSalesByProductAsync(from, to, ct), ct);
        FinishPagedLoad(_allSalesByProduct.Count);
    }

    private async Task LoadSalesByCustomerAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        await EnsureClientsLoadedAsync(ct);
        _allSalesByCustomer = await Task.Run(() => _reportService.GetSalesByCustomerAsync(from, to, ct), ct);
        ApplySaleByCustomerFilter();
    }

    private async Task EnsureClientsLoadedAsync(CancellationToken ct)
    {
        if (_clientsLoaded) return;
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => t.Actif && (t.Type == TypeTiers.Client || t.Type == TypeTiers.LesDeux))
            .OrderBy(t => t.Nom)
            .ToListAsync(ct);
        SaleByCustomerClients.Clear();
        foreach (var c in clients)
            SaleByCustomerClients.Add(c);
        _clientsLoaded = true;
    }

    partial void OnSelectedSaleByCustomerClientChanged(TiersEntity? value)
    {
        if (SelectedReportIndex == 2)
            ApplySaleByCustomerFilter();
    }

    [RelayCommand]
    private void ClearSaleByCustomerClient()
    {
        SelectedSaleByCustomerClient = null;
    }

    private void ApplySaleByCustomerFilter()
    {
        _filteredSalesByCustomer = SelectedSaleByCustomerClient is null
            ? _allSalesByCustomer
            : _allSalesByCustomer.Where(r => r.ClientId == SelectedSaleByCustomerClient.Id).ToList();

        var source = _filteredSalesByCustomer;
        var dev = source.Count > 0 ? source[0].Devise : (_allSalesByCustomer.Count > 0 ? _allSalesByCustomer[0].Devise : "MAD");
        LblSaleByCustomerTotalHt = $"{source.Sum(r => r.TotalHt):N2} {dev}";
        LblSaleByCustomerTotalTtc = $"{source.Sum(r => r.TotalTtc):N2} {dev}";
        LblSaleByCustomerTotalProfit = $"{source.Sum(r => r.Profit):N2} {dev}";
        FinishPagedLoad(source.Count);
    }

    private async Task LoadRefundsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        _allRefunds = await Task.Run(() => _reportService.GetRefundsAsync(from, to, ct), ct);
        FinishPagedLoad(_allRefunds.Count);
    }

    private async Task LoadDailySalesAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        _allDailySales = await Task.Run(() => _reportService.GetDailySalesAsync(from, to, ct), ct);
        var dev = _allDailySales.Count > 0 ? _allDailySales[0].Devise : "MAD";
        LblDailySalesTotalTtc = $"{_allDailySales.Sum(r => r.TotalTtc):N2} {dev}";
        LblDailySalesTotalProfit = $"{_allDailySales.Sum(r => r.Profit):N2} {dev}";
        FinishPagedLoad(_allDailySales.Count);
    }

    private async Task LoadLowStockAsync(CancellationToken ct)
    {
        _allLowStock = await Task.Run(() => _reportService.GetLowStockProductsAsync(ct), ct);
        FinishPagedLoad(_allLowStock.Count);
    }

    private async Task LoadStockMovementsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        _allStockMovements = await Task.Run(() => _reportService.GetStockMovementsAsync(from, to, ct), ct);
        var valuation = await Task.Run(() => _reportService.GetStockValuationAsync(ct), ct);
        LblStockValHt = $"{valuation.ht:N2} {valuation.devise}";
        LblStockValTtc = $"{valuation.ttc:N2} {valuation.devise}";
        FinishPagedLoad(_allStockMovements.Count);
    }

    private async Task LoadZakatAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var result = await Task.Run(() => _reportService.GetZakatAsync(from, to, ct), ct);
        _allZakatClients = result.Clients;
        var dev = result.Devise;
        LblZakatTotalBalances = $"{result.TotalBalances:N2} {dev}";
        LblZakatStockHt = $"{result.StockHt:N2} {dev}";
        LblZakatBase = $"{result.ZakatBase:N2} {dev}";
        LblZakatAmount = $"{result.ZakatAmount:N2} {dev}";
        FinishPagedLoad(_allZakatClients.Count);
    }

    private async Task LoadProfitChargesAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var result = await Task.Run(() => _reportService.GetProfitChargesAsync(from, to, ct), ct);
        _lastProfitCharges = result;
        _allProfitCharges = result.Rows;
        var dev = result.Devise;
        LblProfitChargesTotalMargin = $"+{result.TotalSalesMargin:N2} {dev}";
        LblProfitChargesTotalVente = $"+{result.TotalVente:N2} {dev}";
        LblProfitChargesTotalAvoirsClient = $"-{result.TotalAvoirsClient:N2} {dev}";
        LblProfitChargesTotalPurchases = $"-{result.TotalPurchases:N2} {dev}";
        LblProfitChargesTotalAvoirsFournisseur = $"+{result.TotalAvoirsFournisseur:N2} {dev}";
        LblProfitChargesTotalCharges = $"-{result.TotalCharges:N2} {dev}";
        var netSign = result.NetResult >= 0 ? "+" : "";
        LblProfitChargesNetResult = $"{netSign}{result.NetResult:N2} {dev}";
        IsNetPositive = result.NetResult >= 0;
        ApplyProfitFilter(_profitFilterKind);
    }

    [RelayCommand]
    private async Task ExportPdfAsync(CancellationToken cancellationToken)
    {
        if (!_session.CanAccessReporting)
        {
            await _dialog.ShowErrorAsync(_locale.T("Report_Title"), _locale.T("Report_ErrDenied"), cancellationToken);
            return;
        }

        try
        {
            IsBusy = true;
            var model = BuildCurrentReportPdfModel();
            if (model.Rows.Count == 0 && model.SummaryLines.Count == 0)
            {
                await _dialog.ShowInfoAsync(_locale.T("Export_Pdf"), _locale.T("Reports_Empty"), cancellationToken);
                return;
            }

            var bytes = await _pdf.BuildReportPdfAsync(model, cancellationToken);
            var fileName = BuildReportFileName(model.Title);
            var ok = await _dialog.SavePickedFileBytesAsync(
                _locale.T("Export_PdfPicker"), fileName, new[] { "*.pdf" }, bytes, cancellationToken);
            if (ok)
                await _dialog.ShowInfoAsync(_locale.T("Export_Pdf"), _locale.T("Export_Done"), cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de l'export PDF du rapport", ex, "ReportsListViewModel.ExportPdfAsync");
            await _dialog.ShowErrorAsync(_locale.T("Export_Pdf"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private ReportPdfModel BuildCurrentReportPdfModel()
    {
        var period = ShowDateFilter
            ? $"{_locale.T("Reports_From")} {DateFrom:dd/MM/yyyy}  —  {_locale.T("Reports_To")} {DateTo:dd/MM/yyyy}"
            : null;
        var right = PdfTextAlignment.End;

        return SelectedReportIndex switch
        {
            0 => BuildProfitChargesPdf(period, right),
            1 => BuildSalesByProductPdf(period, right),
            2 => BuildSalesByCustomerPdf(period, right),
            3 => BuildRefundsPdf(period, right),
            4 => BuildDailySalesPdf(period, right),
            5 => BuildLowStockPdf(right),
            6 => BuildStockMovementsPdf(period, right),
            7 => BuildZakatPdf(period, right),
            _ => new ReportPdfModel
            {
                Title = _locale.T("Reports_Title"),
                PeriodLabel = period,
                Columns = [],
                Rows = []
            }
        };
    }

    private static ReportPdfRow PdfRow(params string[] cells) =>
        new() { Cells = cells };

    private static ReportPdfRow PdfDetailRow(params string[] cells) =>
        new() { Cells = cells, IsDetail = true };

    private static ReportPdfRow PdfDayHeaderRow(
        DateTime date,
        string dayName,
        params string[] otherCells)
    {
        var cells = new List<string> { string.Empty };
        cells.AddRange(otherCells);
        return new ReportPdfRow
        {
            Cells = cells,
            IsDayHeader = true,
            DayHeaderOn = date,
            DayHeaderDayName = dayName
        };
    }

    private static ReportPdfRow PdfSpacerRow(int columnCount) =>
        new() { Cells = Enumerable.Repeat(string.Empty, columnCount).ToList(), IsSpacer = true };

    private static ReportPdfRow PdfClientSeparatorRow(int columnCount) =>
        new() { Cells = Enumerable.Repeat(string.Empty, columnCount).ToList(), IsClientSeparator = true };

    private ReportPdfModel BuildProfitChargesPdf(string? period, PdfTextAlignment right)
    {
        var source = _filteredProfitCharges.Count > 0 || _profitFilterKind != null
            ? _filteredProfitCharges
            : _allProfitCharges;
        var rows = source.Select(r => PdfRow(r.TypeLabel, r.RefLibelle, r.LblDate, r.LblMontantHt, r.LblAmount)).ToList();

        var summary = new List<PdfKeyValueLine>();
        if (_lastProfitCharges != null)
        {
            summary.Add(new(LblProfitChargesVenteLabel, LblProfitChargesTotalVente));
            summary.Add(new(LblProfitChargesMarginLabel, LblProfitChargesTotalMargin));
            summary.Add(new(LblProfitChargesAvoirsFournisseurLabel, LblProfitChargesTotalAvoirsFournisseur));
            summary.Add(new(LblProfitChargesAvoirsClientLabel, LblProfitChargesTotalAvoirsClient));
            summary.Add(new(LblProfitChargesPurchasesLabel, LblProfitChargesTotalPurchases));
            summary.Add(new(LblProfitChargesChargesLabel, LblProfitChargesTotalCharges));
            summary.Add(new(LblProfitChargesNetLabel, LblProfitChargesNetResult));
        }

        return new ReportPdfModel
        {
            Title = BtnProfitCharges,
            PeriodLabel = period,
            Columns =
            [
                new(ColProfitType, 1.2f),
                new(ColProfitRef, 2f),
                new(ColProfitDate, 1f),
                new(ColProfitHt, 1.1f, right),
                new(ColProfitAmount, 1.2f, right)
            ],
            Rows = rows,
            SummaryLines = summary,
            Landscape = true
        };
    }

    private ReportPdfModel BuildSalesByProductPdf(string? period, PdfTextAlignment right)
    {
        var rows = _allSalesByProduct
            .Select(r => PdfRow(r.Reference, r.Designation, r.Categorie, r.LblQty, r.LblTtc, r.LblProfit, r.LblMargin))
            .ToList();
        return new ReportPdfModel
        {
            Title = BtnSaleByProduct,
            PeriodLabel = period,
            Columns =
            [
                new(_locale.T("Lbl_ColRef"), 1f),
                new(_locale.T("Lbl_ColDesignation"), 2.2f),
                new(_locale.T("Reports_ColCategory"), 1f),
                new(_locale.T("Lbl_Quantity"), 0.8f, right),
                new(_locale.T("Reports_LblTotalTtc"), 1.1f, right),
                new(_locale.T("Reports_LblProfit"), 1.1f, right),
                new(_locale.T("Reports_ColMarginPct"), 0.8f, right)
            ],
            Rows = rows,
            Landscape = true
        };
    }

    private ReportPdfModel BuildSalesByCustomerPdf(string? period, PdfTextAlignment right)
    {
        // Customer-facing PDF: totals only — no margin / profit columns.
        const int columnCount = 4;
        var totalLabel = _locale.T("Fact_ColTotal");
        var rows = new List<ReportPdfRow>();
        var clientIndex = 0;
        foreach (var r in _filteredSalesByCustomer)
        {
            if (clientIndex > 0)
                rows.Add(PdfClientSeparatorRow(columnCount));
            clientIndex++;

            rows.Add(PdfRow(r.Client, "", "", r.LblTtc));
            var dayIndex = 0;
            foreach (var d in r.Days)
            {
                if (dayIndex > 0)
                    rows.Add(PdfSpacerRow(columnCount));
                dayIndex++;

                rows.Add(PdfDayHeaderRow(
                    d.Date,
                    d.LblDayName,
                    d.LblCount, "", d.LblTtc));
                foreach (var p in d.Products)
                    rows.Add(PdfDetailRow($"  • {p.Reference} {p.Designation}", p.LblQty, p.LblUnitPrice, p.LblTtc));
            }
        }

        return new ReportPdfModel
        {
            Title = BtnSaleByCustomer,
            PeriodLabel = period,
            Columns =
            [
                new(_locale.T("Lbl_Client"), 2.8f),
                new(_locale.T("Lbl_Quantity"), 0.8f, right),
                new(_locale.T("Reports_ColUnitPrice"), 1.2f, right),
                new(totalLabel, 1.2f, right)
            ],
            Rows = rows,
            SummaryLines =
            [
                new(totalLabel, LblSaleByCustomerTotalTtc)
            ],
            Landscape = true
        };
    }

    private ReportPdfModel BuildRefundsPdf(string? period, PdfTextAlignment right)
    {
        var rows = _allRefunds
            .Select(r => PdfRow(r.Numero, r.LblDate, r.Client, r.Motif, r.LblRetour, r.LblTotal))
            .ToList();
        return new ReportPdfModel
        {
            Title = BtnRefunds,
            PeriodLabel = period,
            Columns =
            [
                new(_locale.T("Lbl_ColRef"), 1f),
                new(_locale.T("DevisList_ColDate"), 0.9f),
                new(_locale.T("Lbl_Client"), 1.5f),
                new(_locale.T("Lbl_Motif"), 1.5f),
                new(_locale.T("Reports_ColRetour"), 0.8f),
                new(_locale.T("Reports_LblTotalTtc"), 1.1f, right)
            ],
            Rows = rows,
            Landscape = true
        };
    }

    private ReportPdfModel BuildDailySalesPdf(string? period, PdfTextAlignment right)
    {
        const int columnCount = 6;
        var totalLabel = _locale.T("Fact_ColTotal");
        var culture = _locale.CurrentLanguage.StartsWith("ar", StringComparison.OrdinalIgnoreCase)
            ? CultureInfo.GetCultureInfo("ar")
            : CultureInfo.GetCultureInfo("fr-FR");
        var rows = new List<ReportPdfRow>();
        var dayIndex = 0;
        foreach (var r in _allDailySales)
        {
            if (dayIndex > 0)
                rows.Add(PdfSpacerRow(columnCount));
            dayIndex++;

            var dayName = DisplayDateHelper.DayName(r.Date, culture);
            rows.Add(PdfDayHeaderRow(
                r.Date,
                dayName,
                r.LblCount, "", r.LblTtc, r.LblProfit, r.LblMargin));

            var dayProducts = r.Details
                .SelectMany(d => d.Lines)
                .GroupBy(l => (l.Reference, l.Designation))
                .Select(g =>
                {
                    var qty = g.Sum(x => x.Quantite);
                    var ht = g.Sum(x => x.TotalHt);
                    var profit = g.Sum(x => x.Profit);
                    var marginPct = ht > 0 ? profit / ht * 100m : 0;
                    return new ReportDailySaleLineRow(
                        g.Key.Reference,
                        g.Key.Designation,
                        qty,
                        ht,
                        ht,
                        r.Devise,
                        profit,
                        marginPct);
                })
                .OrderByDescending(p => p.TotalTtc)
                .ToList();

            foreach (var line in dayProducts)
            {
                var label = string.IsNullOrWhiteSpace(line.Reference)
                    ? line.Designation
                    : $"{line.Reference} {line.Designation}";
                rows.Add(PdfDetailRow(
                    $"  • {label}",
                    line.LblQty,
                    line.LblUnitPrice,
                    line.LblTtc,
                    line.LblProfit,
                    line.LblMargin));
            }
        }

        return new ReportPdfModel
        {
            Title = BtnDailySales,
            PeriodLabel = period,
            Columns =
            [
                new(_locale.T("Lbl_Client"), 2.2f),
                new(_locale.T("Lbl_Quantity"), 0.7f, right),
                new(_locale.T("Reports_ColUnitPrice"), 1.1f, right),
                new(totalLabel, 1.1f, right),
                new(_locale.T("Reports_LblProfit"), 1.1f, right),
                new(_locale.T("Reports_ColMarginPct"), 0.8f, right)
            ],
            Rows = rows,
            SummaryLines =
            [
                new(totalLabel, LblDailySalesTotalTtc),
                new(LblSaleByCustomerLabelProfit, LblDailySalesTotalProfit)
            ],
            Landscape = true
        };
    }

    private ReportPdfModel BuildLowStockPdf(PdfTextAlignment right)
    {
        var rows = _allLowStock
            .Select(r => PdfRow(r.Reference, r.Designation, r.LblStockActuel, r.LblStockMinimum))
            .ToList();
        return new ReportPdfModel
        {
            Title = BtnLowStock,
            PeriodLabel = null,
            Columns =
            [
                new(ColLowStockRef, 1.2f),
                new(ColLowStockName, 2.2f),
                new(ColLowStockActual, 1.1f, right),
                new(ColLowStockMin, 1.1f, right)
            ],
            Rows = rows
        };
    }

    private ReportPdfModel BuildStockMovementsPdf(string? period, PdfTextAlignment right)
    {
        var rows = _allStockMovements
            .Select(r => PdfRow(
                r.LblDate,
                $"{r.ProduitRef} — {r.ProduitDesignation}",
                r.TypeMvt,
                r.LblQty,
                r.Origine,
                r.LblStockApres,
                r.ShowShouldQuote ? r.Status : "—"))
            .ToList();
        return new ReportPdfModel
        {
            Title = BtnStockMovements,
            PeriodLabel = period,
            Columns =
            [
                new(_locale.T("DevisList_ColDate"), 1.1f),
                new(_locale.T("Lbl_ColDesignation"), 1.8f),
                new(_locale.T("Reports_ColType"), 0.9f),
                new(_locale.T("Lbl_Quantity"), 0.7f, right),
                new(_locale.T("Lbl_ColOrigin"), 1f),
                new(_locale.T("Lbl_ColStockCurrent"), 0.9f, right),
                new(_locale.T("Reports_ColStatus"), 1.4f)
            ],
            Rows = rows,
            SummaryLines =
            [
                new(LblStockValHtLabel, LblStockValHt),
                new(LblStockValTtcLabel, LblStockValTtc)
            ],
            Landscape = true
        };
    }

    private ReportPdfModel BuildZakatPdf(string? period, PdfTextAlignment right)
    {
        var rows = _allZakatClients.Select(r => PdfRow(r.Client, r.LblSolde)).ToList();
        var summary = new List<PdfKeyValueLine>
        {
            new(LblZakatTotalBalancesLabel, LblZakatTotalBalances),
            new(LblZakatStockHtLabel, LblZakatStockHt),
            new(LblZakatBaseLabel, LblZakatBase),
            new(LblZakatAmountLabel, LblZakatAmount)
        };
        return new ReportPdfModel
        {
            Title = BtnZakat,
            PeriodLabel = period,
            Columns =
            [
                new(ColZakatClient, 3f),
                new(ColZakatBalance, 1.2f, right)
            ],
            Rows = rows,
            SummaryLines = summary
        };
    }

    private static string BuildReportFileName(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(title.Select(c => invalid.Contains(c) || c == ' ' ? '-' : c).ToArray());
        while (cleaned.Contains("--", StringComparison.Ordinal))
            cleaned = cleaned.Replace("--", "-", StringComparison.Ordinal);
        cleaned = cleaned.Trim('-');
        if (string.IsNullOrEmpty(cleaned))
            cleaned = "rapport";
        return $"{cleaned}-{DateTime.Today:yyyy-MM-dd}.pdf".ToLowerInvariant();
    }

    [RelayCommand]
    private void FilterProfitMargin() => ToggleProfitFilter(ReportProfitChargeKind.SaleMargin);

    [RelayCommand]
    private void FilterProfitAvoirsClient() => ToggleProfitFilter(ReportProfitChargeKind.AvoirClient);

    [RelayCommand]
    private void FilterProfitPurchases() => ToggleProfitFilter(ReportProfitChargeKind.Purchase);

    [RelayCommand]
    private void FilterProfitAvoirsFournisseur() => ToggleProfitFilter(ReportProfitChargeKind.AvoirFournisseur);

    [RelayCommand]
    private void FilterProfitCharges() => ToggleProfitFilter(ReportProfitChargeKind.Charge);

    [RelayCommand]
    private void FilterProfitAll() => ToggleProfitFilter(null);

    private void ToggleProfitFilter(ReportProfitChargeKind? kind)
    {
        if (_profitFilterKind == kind)
            kind = null; // click again clears filter
        ApplyProfitFilter(kind);
    }

    private void ApplyProfitFilter(ReportProfitChargeKind? kind)
    {
        _profitFilterKind = kind;
        IsProfitFilterMarginActive = kind == ReportProfitChargeKind.SaleMargin;
        IsProfitFilterAvoirsClientActive = kind == ReportProfitChargeKind.AvoirClient;
        IsProfitFilterPurchasesActive = kind == ReportProfitChargeKind.Purchase;
        IsProfitFilterAvoirsFournisseurActive = kind == ReportProfitChargeKind.AvoirFournisseur;
        IsProfitFilterChargesActive = kind == ReportProfitChargeKind.Charge;
        IsProfitFilterAllActive = kind == null;

        _filteredProfitCharges = kind == null
            ? _allProfitCharges
            : _allProfitCharges.Where(r => r.Kind == kind).ToList();

        FinishPagedLoad(_filteredProfitCharges.Count);
    }

    private void FinishPagedLoad(int totalCount)
    {
        Pagination.CurrentPage = 1;
        Pagination.TotalCount = totalCount;
        ShowEmpty = totalCount == 0;
        ShowPagination = totalCount > 0;
        ApplyCurrentPage();
    }

    private void ApplyCurrentPage()
    {
        switch (SelectedReportIndex)
        {
            case 0:
                ApplyPage(ProfitCharges, _filteredProfitCharges);
                break;
            case 1:
                ApplyPage(SalesByProduct, _allSalesByProduct);
                break;
            case 2:
                ApplyPage(SalesByCustomer, _filteredSalesByCustomer);
                break;
            case 3:
                ApplyPage(Refunds, _allRefunds);
                break;
            case 4:
                ApplyPage(DailySales, _allDailySales);
                break;
            case 5:
                ApplyPage(LowStockProducts, _allLowStock);
                break;
            case 6:
                ApplyPage(StockMovements, _allStockMovements);
                break;
            case 7:
                ApplyPage(ZakatClients, _allZakatClients);
                break;
        }
    }

    private void ApplyPage<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();
        foreach (var item in source.Skip(Pagination.Skip).Take(Pagination.PageSize))
            target.Add(item);
    }
}
