using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Facturation.Services;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Models.Pdf;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Facturation.ViewModels;

public partial class FactureEditViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDocumentNumberService _numbers;
    private readonly IAppSettingsService _settings;
    private readonly IFactureWorkflowService _factureWorkflow;
    private readonly IStockMovementService _stock;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly IPdfService _pdf;
    private readonly IPdfPrintService _pdfPrint;
    private readonly AddLineCatalogSearchCoordinator _addLineSearch;

    public FactureEditViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDocumentNumberService numbers,
        IAppSettingsService settings,
        IFactureWorkflowService factureWorkflow,
        IStockMovementService stock,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale,
        IPdfService pdf,
        IPdfPrintService pdfPrint,
        ICatalogSearchService catalogSearch)
    {
        _dbFactory = dbFactory;
        _numbers = numbers;
        _settings = settings;
        _factureWorkflow = factureWorkflow;
        _stock = stock;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _session = session;
        _locale = locale;
        _pdf = pdf;
        _pdfPrint = pdfPrint;
        _addLineSearch = new AddLineCatalogSearchCoordinator(catalogSearch);
        _locale.CultureApplied += (_, _) =>
        {
            RefreshFactureUi();
            UpdateFactureTotalLines();
        };
        Title = _locale.T("Fact_Title");
        RefreshFactureUi();
    }

    public ObservableCollection<GestionCommerciale.Modules.Tiers.Models.Tiers> Clients { get; } = [];
    public ObservableCollection<FactureLineRow> Lignes { get; } = [];
    public ObservableCollection<FacturePaiementRowViewModel> Paiements { get; } = [];

    [ObservableProperty] private int? _factureId;
    [ObservableProperty] private int _clientId;
    [ObservableProperty] private GestionCommerciale.Modules.Tiers.Models.Tiers? _selectedClient;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private DateTimeOffset _date = new(DateTime.Today);
    [ObservableProperty] private decimal _totalTtc;
    [ObservableProperty] private decimal _montantPaye;
    [ObservableProperty] private bool _canEditDraft;

    [ObservableProperty] private decimal _paiementMontant;
    [ObservableProperty] private DateTimeOffset _paiementDate = new(DateTime.Today);
    [ObservableProperty] private ModePaiement _paiementMode = ModePaiement.Especes;
    [ObservableProperty] private string _paiementReference = string.Empty;
    [ObservableProperty] private FactureLineRow? _selectedLine;
    [ObservableProperty] private string _addLineSearchText = string.Empty;
    [ObservableProperty] private object? _addLineCatalogPick;

    [ObservableProperty] private string _btnPdf = string.Empty;
    [ObservableProperty] private string _btnPrint = string.Empty;
    [ObservableProperty] private string _btnBack = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;
    [ObservableProperty] private string _menuDeleteFacture = string.Empty;
    [ObservableProperty] private string _lblClient = string.Empty;
    [ObservableProperty] private string _wmClientSearch = string.Empty;
    [ObservableProperty] private string _lblDateFacture = string.Empty;
    [ObservableProperty] private string _btnRemoveLine = string.Empty;
    [ObservableProperty] private string _lblAddProduct = string.Empty;
    [ObservableProperty] private string _wmAddProduct = string.Empty;
    [ObservableProperty] private string _lblTotals = string.Empty;
    [ObservableProperty] private string _devise = string.Empty;
    [ObservableProperty] private string _totalTtcLabel = string.Empty;
    [ObservableProperty] private string _montantPayeLine = string.Empty;
    [ObservableProperty] private string _lblPaymentsRecorded = string.Empty;
    [ObservableProperty] private string _lblMontant = string.Empty;
    [ObservableProperty] private string _lblPaymentDate = string.Empty;
    [ObservableProperty] private string _lblMode = string.Empty;
    [ObservableProperty] private string _lblReference = string.Empty;
    [ObservableProperty] private string _wmRefShort = string.Empty;
    [ObservableProperty] private string _lblNewPayment = string.Empty;
    [ObservableProperty] private string _btnAddPayment = string.Empty;
    [ObservableProperty] private string _btnDelete = string.Empty;
    [ObservableProperty] private string _btnCancel = string.Empty;
    [ObservableProperty] private string _payEditTooltip = string.Empty;
    [ObservableProperty] private string _lblDocColDesignation = string.Empty;
    [ObservableProperty] private string _lblDocColQte = string.Empty;
    [ObservableProperty] private string _lblDocColCond = string.Empty;
    [ObservableProperty] private string _wmDocLineUnite = string.Empty;
    [ObservableProperty] private string _lblDocColPrice = string.Empty;
    [ObservableProperty] private string _lblDocColTotal = string.Empty;

    public ObservableCollection<DocumentCatalogItem> AddLineSearchResults => _addLineSearch.Results;

    public AutoCompleteFilterPredicate<object?> PartyAutocompleteFilter => PartyAutoComplete.ItemFilter;

    private bool _suppressAddLinePick;

    private void RefreshFactureUi()
    {
        BtnPdf = _locale.T("Btn_Pdf");
        BtnPrint = _locale.T("Btn_Print");
        BtnBack = _locale.T("Btn_Back");
        BtnSave = _locale.T("Btn_Save");
        MenuDeleteFacture = _locale.T("Fact_MenuDelete");
        LblClient = _locale.T("Lbl_Client");
        WmClientSearch = _locale.T("Wm_SearchClient");
        LblDateFacture = _locale.T("Lbl_DateFacture");
        BtnRemoveLine = _locale.T("Btn_RemoveLine");
        LblAddProduct = _locale.T("Devis_LblAddProduct");
        WmAddProduct = _locale.T("Wm_SearchCatalog");
        LblTotals = _locale.T("Lbl_Totals");
        LblPaymentsRecorded = _locale.T("Lbl_PaymentsRecorded");
        LblMontant = _locale.T("Lbl_Montant");
        LblPaymentDate = _locale.T("Lbl_PaymentDate");
        LblMode = _locale.T("Lbl_Mode");
        LblReference = _locale.T("Lbl_Reference");
        WmRefShort = _locale.T("Lbl_RefShort");
        LblNewPayment = _locale.T("Lbl_NewPayment");
        BtnAddPayment = _locale.T("Btn_AddPayment");
        BtnDelete = _locale.T("Btn_Delete");
        BtnCancel = _locale.T("Btn_Cancel");
        PayEditTooltip = _locale.T("Pay_EditTooltip");
        LblDocColDesignation = _locale.T("DocLine_ColDesignation");
        LblDocColQte = _locale.T("DocLine_ColQte");
        LblDocColCond = _locale.T("DocLine_ColCond");
        WmDocLineUnite = _locale.T("DocLine_WmUnite");
        LblDocColPrice = _locale.T("Fact_ColPrice");
        LblDocColTotal = _locale.T("Fact_ColTotal");
    }

    private void UpdateFactureTotalLines()
    {
        TotalTtcLabel = _locale.Tf("Doc_FmtTtc", TotalTtc, Devise).TrimEnd();
        MontantPayeLine = _locale.Tf("Doc_FmtPaye", MontantPaye);
    }

    partial void OnDeviseChanged(string value) => UpdateFactureTotalLines();

    public Array ModesPaiement => Enum.GetValues(typeof(ModePaiement));

    private bool CanExecuteAddPaiement() => FactureId.HasValue;

    partial void OnMontantPayeChanged(decimal value) => UpdateFactureTotalLines();

    partial void OnFactureIdChanged(int? value)
    {
        AddPaiementCommand.NotifyCanExecuteChanged();
        RemoveFactureCommand.NotifyCanExecuteChanged();
    }

    private bool CanRemoveFacture() => FactureId != null;

    [RelayCommand(CanExecute = nameof(CanRemoveFacture))]
    private async Task RemoveFactureAsync(CancellationToken cancellationToken)
    {
        if (FactureId is not { } id) return;

        if (!await _dialog.ConfirmAsync(_locale.T("Fact_Title"), _locale.Tf("Fact_ConfirmDelete", Numero), cancellationToken))
            return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            if (await db.Avoirs.AsNoTracking().AnyAsync(a => a.FactureId == id, cancellationToken))
            {
                await _dialog.ShowErrorAsync(_locale.T("Fact_Title"), _locale.T("Fact_ErrDeleteReferenced"), cancellationToken);
                return;
            }

            var entity = await db.Factures.Include(f => f.Lignes).Include(f => f.Paiements).FirstAsync(f => f.Id == id, cancellationToken);
            await _stock.SyncFactureStockAsync(db, entity.Id, entity.Numero, [], null, cancellationToken);
            db.Factures.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);

            await _dialog.ShowInfoAsync(_locale.T("Fact_Title"), _locale.T("Fact_Deleted"), cancellationToken);
            Back();
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de la suppression de la facture", ex, "FactureEditViewModel.RemoveFactureAsync");
            await _dialog.ShowErrorAsync(_locale.T("Fact_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ReloadPaiementsList(IEnumerable<Paiement> paiements)
    {
        Paiements.Clear();
        foreach (var p in paiements.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id))
            Paiements.Add(new FacturePaiementRowViewModel(this, p));
    }

    public async Task CommitPaiementRowAsync(FacturePaiementRowViewModel row, CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;
        if (FactureId == null || row.Montant <= 0)
        {
            await _dialog.ShowErrorAsync(_locale.T("Pay_Title"), _locale.T("Pay_ErrAmount"), cancellationToken);
            return;
        }

        try
        {
            IsBusy = true;
            await _factureWorkflow.UpdatePaiementAsync(
                FactureId.Value,
                row.Id,
                row.Montant,
                row.Date.DateTime,
                row.Mode,
                row.Reference,
                cancellationToken);
            await LoadAsync(FactureId, cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de la mise à jour du paiement", ex, "FactureEditViewModel.CommitPaiementRowAsync");
            await _dialog.ShowErrorAsync(_locale.T("Pay_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task DeletePaiementRowAsync(FacturePaiementRowViewModel row, CancellationToken cancellationToken = default)
    {
        if (IsBusy) return;
        if (FactureId == null) return;
        if (!await _dialog.ConfirmAsync(_locale.T("Pay_Title"), _locale.T("Pay_ConfirmDelete"), cancellationToken))
            return;

        try
        {
            IsBusy = true;
            await _factureWorkflow.DeletePaiementAsync(FactureId.Value, row.Id, cancellationToken);
            await LoadAsync(FactureId, cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de la suppression du paiement", ex, "FactureEditViewModel.DeletePaiementRowAsync");
            await _dialog.ShowErrorAsync(_locale.T("Pay_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void HookLines()
    {
        foreach (var row in Lignes)
            row.PropertyChanged += LineChanged;
    }

    private void LineChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshTotals();
        if (e.PropertyName is nameof(FactureLineRow.ProduitId) or nameof(FactureLineRow.ServiceId)
            && sender is FactureLineRow changed && (changed.ProduitId is > 0 || changed.ServiceId is > 0))
            ConsolidateDuplicateCatalogLines();
    }

    partial void OnAddLineSearchTextChanged(string value)
    {
        if (_suppressAddLinePick) return;
        _addLineSearch.QueueSearch(value);
    }

    partial void OnAddLineCatalogPickChanged(object? value)
    {
        if (_suppressAddLinePick) return;
        if (value is not DocumentCatalogItem item) return;
        _suppressAddLinePick = true;
        var existing = item.Kind == DocumentCatalogKind.Service
            ? Lignes.FirstOrDefault(l => l.ServiceId == item.Id && item.Id != 0)
            : Lignes.FirstOrDefault(l => l.ProduitId == item.Id && item.Id != 0);
        if (existing != null)
        {
            existing.Quantite += 1;
            SelectedLine = existing;
        }
        else
        {
            var row = new FactureLineRow();
            row.ApplyCatalogItem(item);
            row.Quantite = 1;
            row.PropertyChanged += LineChanged;
            Lignes.Add(row);
            SelectedLine = row;
        }

        _addLineSearch.ResetAfterPick(
            () =>
            {
                AddLineCatalogPick = null;
                AddLineSearchText = string.Empty;
            },
            () => _suppressAddLinePick = false);
        RefreshTotals();
    }

    private void ConsolidateDuplicateCatalogLines()
    {
        foreach (var g in Lignes.Where(l => l.ProduitId is > 0).GroupBy(l => l.ProduitId).ToList())
        {
            if (g.Count() < 2) continue;
            MergeDuplicateGroup(g);
        }

        foreach (var g in Lignes.Where(l => l.ServiceId is > 0).GroupBy(l => l.ServiceId).ToList())
        {
            if (g.Count() < 2) continue;
            MergeDuplicateGroup(g);
        }
    }

    private void MergeDuplicateGroup(IEnumerable<FactureLineRow> group)
    {
        var ordered = group.OrderBy(l => Lignes.IndexOf(l)).ToList();
        var keep = ordered[0];
        var extraQty = ordered.Skip(1).Sum(l => l.Quantite);
        foreach (var line in ordered.Skip(1))
        {
            if (ReferenceEquals(SelectedLine, line))
                SelectedLine = keep;
            line.PropertyChanged -= LineChanged;
            Lignes.Remove(line);
        }

        keep.Quantite += extraQty;
    }

    private void ResetAddProductSearch()
    {
        _suppressAddLinePick = true;
        AddLineCatalogPick = null;
        AddLineSearchText = string.Empty;
        _suppressAddLinePick = false;
        _addLineSearch.Clear();
    }

    private void RefreshTotals()
    {
        var lines = Lignes.Select(l => new FactureLigne
        {
            Quantite = l.Quantite,
            PrixUnitaireHT = l.PrixUnitaireHt
        });
        TotalTtc = DocumentTotalsHelper.FactureTtc(lines);
        UpdateFactureTotalLines();
        RefreshSuggestedPaiementMontant();
    }

    private void RefreshSuggestedPaiementMontant()
    {
        if (!FactureId.HasValue) return;
        PaiementMontant = Math.Round(Math.Max(0, ComputeFullPaymentTtc() - MontantPaye), 2);
    }

    private decimal ComputeFullPaymentTtc() =>
        DocumentTotalsHelper.FactureTtc(
            Lignes.Select(l => new FactureLigne
            {
                Quantite = l.Quantite,
                PrixUnitaireHT = l.PrixUnitaireHt
            }));

    private async Task<bool> ValidatePaymentsAgainstTtcAsync(decimal ttc, decimal totalPayments, CancellationToken cancellationToken)
    {
        if (!DocumentTotalsHelper.PaymentsExceedTtc(ttc, totalPayments))
            return true;

        await _dialog.ShowErrorAsync(
            _locale.T("Pay_Title"),
            _locale.Tf("Pay_ErrPaymentsExceedTtc", totalPayments, ttc),
            cancellationToken);
        return false;
    }

    partial void OnSelectedClientChanged(GestionCommerciale.Modules.Tiers.Models.Tiers? value)
    {
        var id = value?.Id ?? 0;
        if (ClientId == id) return;
        ClientId = id;
    }

    partial void OnClientIdChanged(int value)
    {
        if (SelectedClient?.Id == value) return;
        SelectedClient = Clients.FirstOrDefault(c => c.Id == value);
    }

    public async Task LoadAsync(int? id, CancellationToken cancellationToken = default)
    {
        FactureId = id;
        var cfg = await _settings.GetAsync(cancellationToken);
        Devise = CurrencyHelper.FromSettings(cfg);
        Lignes.Clear();
        ResetAddProductSearch();
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await LoadClientsAsync(db, cancellationToken);

        if (id == null)
        {
            Numero = _locale.T("Fact_NewNumPlaceholder");
            ClientId = Clients.FirstOrDefault()?.Id ?? 0;
            Date = new DateTimeOffset(DateTime.Today);
            CanEditDraft = true;
            Title = _locale.T("Fact_NewTitle");
            MontantPaye = 0;
            Paiements.Clear();
            RefreshTotals();
            ResetAddProductSearch();
            return;
        }

        var f = await db.Factures.Include(x => x.Lignes).Include(x => x.Paiements).FirstAsync(x => x.Id == id, cancellationToken);
        Numero = f.Numero;
        ClientId = f.ClientId;
        Date = new DateTimeOffset(f.Date);
        foreach (var l in f.Lignes)
        {
            var row = new FactureLineRow
            {
                ProduitId = l.ProduitId,
                ServiceId = l.ServiceId,
                Designation = l.Designation,
                Conditionnement = l.Conditionnement,
                Quantite = l.Quantite,
                PrixUnitaireHt = l.PrixUnitaireHT
            };
            Lignes.Add(row);
        }

        HookLines();
        MontantPaye = f.Paiements.Where(p => p.Mode != ModePaiement.Credit).Sum(p => p.Montant);
        ReloadPaiementsList(f.Paiements);
        DocumentTotalsHelper.SyncFactureTotalTtc(f);
        if (db.Entry(f).Property(x => x.TotalTtc).IsModified)
            await db.SaveChangesAsync(cancellationToken);
        CanEditDraft = true;
        Title = _locale.Tf("Fact_TitleNum", Numero);
        RefreshTotals();
        ResetAddProductSearch();
    }

    private async Task LoadClientsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => t.Actif && (t.Type == TypeTiers.Client || t.Type == TypeTiers.LesDeux))
            .OrderBy(t => t.Nom).ToListAsync(cancellationToken);
        Clients.Clear();
        foreach (var c in clients) Clients.Add(c);
    }

    public void Load(int? id) => _ = LoadAsync(id, CancellationToken.None);

    [RelayCommand]
    private void RemoveLine(FactureLineRow? row)
    {
        if (row == null) return;
        row.PropertyChanged -= LineChanged;
        Lignes.Remove(row);
        RefreshTotals();
    }

    [RelayCommand]
    private void RemoveSelectedLine()
    {
        if (SelectedLine == null) return;
        RemoveLine(SelectedLine);
        SelectedLine = null;
    }

    [RelayCommand]
    private async Task SaveDraftAsync(CancellationToken cancellationToken)
    {
        if (ClientId == 0 || !Lignes.Any())
        {
            await _dialog.ShowErrorAsync(_locale.T("Fact_Title"), _locale.T("Fact_ErrClientLines"), cancellationToken);
            return;
        }

        if (DocumentTotalsHelper.IsEffectivelyZeroTotal(ComputeFullPaymentTtc()))
        {
            await _dialog.ShowErrorAsync(_locale.T("Fact_Title"), _locale.T("Doc_ErrZeroTtc"), cancellationToken);
            return;
        }

        if (FactureId != null)
        {
            await using var checkDb = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var paid = await checkDb.Paiements.AsNoTracking()
                .Where(p => p.FactureId == FactureId)
                .SumAsync(p => p.Montant, cancellationToken);
            if (!await ValidatePaymentsAgainstTtcAsync(ComputeFullPaymentTtc(), paid, cancellationToken))
                return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            Facture entity;
            if (FactureId == null)
            {
                var num = await _numbers.NextFactureAsync(cancellationToken);
                entity = new Facture
                {
                    Numero = num,
                    ClientId = ClientId,
                    Date = Date.DateTime,
                    CreatedByUserId = _session.UserId
                };
                foreach (var l in Lignes)
                {
                    entity.Lignes.Add(new FactureLigne
                    {
                        ProduitId = l.IsService ? null : l.ProduitId,
                        ServiceId = l.IsService ? l.ServiceId : null,
                        Designation = l.Designation,
                        Conditionnement = l.Conditionnement,
                        Quantite = l.Quantite,
                        PrixUnitaireHT = l.PrixUnitaireHt
                    });
                }

                DocumentTotalsHelper.SyncFactureTotalTtc(entity);
                db.Factures.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                FactureId = entity.Id;
            }
            else
            {
                entity = await db.Factures.Include(f => f.Lignes).FirstAsync(f => f.Id == FactureId, cancellationToken);

                entity.ClientId = ClientId;
                entity.Date = Date.DateTime;
                db.FactureLignes.RemoveRange(entity.Lignes);
                foreach (var l in Lignes)
                {
                    entity.Lignes.Add(new FactureLigne
                    {
                        ProduitId = l.IsService ? null : l.ProduitId,
                        ServiceId = l.IsService ? l.ServiceId : null,
                        Designation = l.Designation,
                        Conditionnement = l.Conditionnement,
                        Quantite = l.Quantite,
                        PrixUnitaireHT = l.PrixUnitaireHt
                    });
                }

                DocumentTotalsHelper.SyncFactureTotalTtc(entity);
                await db.SaveChangesAsync(cancellationToken);
            }

            await _stock.SyncFactureStockAsync(
                db,
                entity.Id,
                entity.Numero,
                Lignes
                    .Where(l => l.ProduitId is int && !l.IsService)
                    .Select(l => (ProduitId: l.ProduitId!.Value, Quantite: l.Quantite)),
                _session.UserId,
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            Numero = entity.Numero;
            await _dialog.ShowInfoAsync(_locale.T("Fact_Title"), _locale.T("Fact_Saved"), cancellationToken);
            await LoadAsync(FactureId, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteAddPaiement))]
    private async Task AddPaiementAsync(CancellationToken cancellationToken)
    {
        if (IsBusy) return;

        if (!FactureId.HasValue)
        {
            await _dialog.ShowErrorAsync(_locale.T("Pay_Title"), _locale.T("Pay_ErrSaveFirst"), cancellationToken);
            return;
        }

        if (PaiementMontant <= 0)
        {
            await _dialog.ShowErrorAsync(_locale.T("Pay_Title"), _locale.T("Pay_ErrAmount"), cancellationToken);
            return;
        }

        var fullTtc = ComputeFullPaymentTtc();
        if (!await ValidatePaymentsAgainstTtcAsync(fullTtc, MontantPaye + PaiementMontant, cancellationToken))
            return;

        try
        {
            IsBusy = true;
            await _factureWorkflow.AddPaiementAsync(FactureId.Value, new Paiement
            {
                Montant = PaiementMontant,
                Date = PaiementDate.DateTime,
                Mode = PaiementMode,
                Reference = PaiementReference,
                CreatedByUserId = _session.UserId
            }, cancellationToken);
            PaiementMontant = 0;
            PaiementReference = string.Empty;
            PaiementDate = new DateTimeOffset(DateTime.Today);
            await LoadAsync(FactureId, cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de l'ajout du paiement", ex, "FactureEditViewModel.AddPaiementAsync");
            await _dialog.ShowErrorAsync(_locale.T("Pay_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Back()
    {
        var list = _sp.GetRequiredService<FactureListViewModel>();
        _workspace.Open(list);
        list.LoadCommand.Execute(null);
    }

    [RelayCommand]
    private async Task ExportPdfAsync(CancellationToken cancellationToken)
    {
        if (FactureId is not { }) return;
        try
        {
            IsBusy = true;
            var bytes = await BuildFacturePdfBytesAsync(cancellationToken);
            if (bytes == null) return;
            var ok = await _dialog.SavePickedFileBytesAsync(_locale.T("Export_PdfPicker"), $"{Numero}.pdf", new[] { "*.pdf" }, bytes, cancellationToken);
            if (ok)
                await _dialog.ShowInfoAsync(_locale.T("Export_Pdf"), _locale.T("Export_Done"), cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de l'export PDF de la facture", ex, "FactureEditViewModel.ExportPdfAsync");
            await _dialog.ShowErrorAsync(_locale.T("Export_Pdf"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PrintAsync(CancellationToken cancellationToken)
    {
        if (FactureId is not { }) return;
        try
        {
            IsBusy = true;
            var bytes = await BuildFacturePdfBytesAsync(cancellationToken);
            if (bytes == null) return;
            await _pdfPrint.PrintPdfAsync(bytes, Numero, cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de l'impression de la facture", ex, "FactureEditViewModel.PrintAsync");
            await _dialog.ShowErrorAsync(_locale.T("Btn_Print"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<byte[]?> BuildFacturePdfBytesAsync(CancellationToken cancellationToken)
    {
        if (FactureId is not { } id) return null;
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var f = await db.Factures.Include(x => x.Lignes).Include(x => x.Paiements).FirstAsync(x => x.Id == id, cancellationToken);
        var client = await db.Tiers.AsNoTracking().FirstAsync(t => t.Id == f.ClientId, cancellationToken);
        return await _pdf.BuildFacturePdfAsync(f, DocumentPartyPdfInfo.FromTiers(client), cancellationToken);
    }
}
