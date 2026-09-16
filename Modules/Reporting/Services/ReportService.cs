using GestionCommerciale.Modules.AvoirFournisseur.Models;
using GestionCommerciale.Modules.Charges.Models;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.FactureFournisseur.Models;
using GestionCommerciale.Modules.Reporting.ViewModels;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Reporting.Services;

public sealed class ReportService : IReportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IAppSettingsService _settings;
    private readonly ILocaleService _locale;

    public ReportService(
        IDbContextFactory<AppDbContext> dbFactory,
        IAppSettingsService settings,
        ILocaleService locale)
    {
        _dbFactory = dbFactory;
        _settings = settings;
        _locale = locale;
    }

    public async Task<List<ReportSaleByProductRow>> GetSalesByProductAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var toEnd = to.Date.AddDays(1);
        var serviceCategory = _locale.T("Reports_CategoryService");

        var lignes = await db.FactureLignes.AsNoTracking()
            .Where(l => l.Facture!.Date >= from && l.Facture.Date < toEnd)
            .Select(l => new
            {
                l.ProduitId,
                l.ServiceId,
                l.Quantite,
                l.PrixUnitaireHT,
                l.Designation
            })
            .ToListAsync(ct);

        var prodIds = lignes.Where(l => l.ProduitId is > 0).Select(l => l.ProduitId!.Value).Distinct().ToList();
        var produits = prodIds.Count == 0
            ? []
            : await db.Produits.AsNoTracking()
                .Where(p => prodIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Reference, p.Designation, p.PrixAchatHT, Categorie = p.Categorie != null ? p.Categorie.Nom : "" })
                .ToListAsync(ct);
        var prodMap = produits.ToDictionary(p => p.Id);

        var svcIds = lignes.Where(l => l.ServiceId is > 0).Select(l => l.ServiceId!.Value).Distinct().ToList();
        var services = svcIds.Count == 0
            ? []
            : await db.Services.AsNoTracking()
                .Where(s => svcIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Reference, s.Designation, s.CoutHT })
                .ToListAsync(ct);
        var svcMap = services.ToDictionary(s => s.Id);

        var productRows = lignes
            .Where(l => l.ProduitId is > 0)
            .GroupBy(l => l.ProduitId!.Value)
            .Select(g =>
            {
                var p = prodMap.GetValueOrDefault(g.Key);
                var prixAchat = p?.PrixAchatHT ?? 0;
                var ht = g.Sum(l => l.Quantite * l.PrixUnitaireHT);
                var cost = g.Sum(l => l.Quantite * prixAchat);
                var profit = ht - cost;
                var marginPct = ht > 0 ? profit / ht * 100m : 0;
                return new ReportSaleByProductRow(
                    p?.Reference ?? string.Empty,
                    p?.Designation ?? g.First().Designation,
                    p?.Categorie ?? string.Empty,
                    g.Sum(l => l.Quantite),
                    ht,
                    ht,
                    dev,
                    profit,
                    marginPct);
            });

        var serviceRows = lignes
            .Where(l => l.ServiceId is > 0)
            .GroupBy(l => l.ServiceId!.Value)
            .Select(g =>
            {
                var s = svcMap.GetValueOrDefault(g.Key);
                var cout = s?.CoutHT ?? 0;
                var ht = g.Sum(l => l.Quantite * l.PrixUnitaireHT);
                var cost = g.Sum(l => l.Quantite * cout);
                var profit = ht - cost;
                var marginPct = ht > 0 ? profit / ht * 100m : 0;
                return new ReportSaleByProductRow(
                    s?.Reference ?? string.Empty,
                    s?.Designation ?? g.First().Designation,
                    serviceCategory,
                    g.Sum(l => l.Quantite),
                    ht,
                    ht,
                    dev,
                    profit,
                    marginPct);
            });

        return productRows.Concat(serviceRows)
            .OrderByDescending(r => r.TotalTtc)
            .ToList();
    }

    public async Task<List<ReportSaleByCustomerRow>> GetSalesByCustomerAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var toEnd = to.Date.AddDays(1);

        var factures = await db.Factures.AsNoTracking()
            .Where(f => f.Date >= from && f.Date < toEnd)
            .Select(f => new
            {
                f.Id,
                f.ClientId,
                Lignes = f.Lignes!.Select(l => new
                {
                    l.ProduitId,
                    l.ServiceId,
                    l.Quantite,
                    l.PrixUnitaireHT,
                    l.Designation
                }).ToList()
            })
            .ToListAsync(ct);

        var clientIds = factures.Select(f => f.ClientId).Distinct().ToList();
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => clientIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Nom, t.ICE, t.Ville })
            .ToListAsync(ct);
        var clientMap = clients.ToDictionary(c => c.Id);

        var allProdIds = factures.SelectMany(f => f.Lignes).Where(l => l.ProduitId is > 0).Select(l => l.ProduitId!.Value).Distinct().ToList();
        var produits = allProdIds.Count == 0
            ? []
            : await db.Produits.AsNoTracking()
                .Where(p => allProdIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Reference, p.Designation, p.PrixAchatHT })
                .ToListAsync(ct);
        var prodMap = produits.ToDictionary(p => p.Id);

        var allSvcIds = factures.SelectMany(f => f.Lignes).Where(l => l.ServiceId is > 0).Select(l => l.ServiceId!.Value).Distinct().ToList();
        var services = allSvcIds.Count == 0
            ? []
            : await db.Services.AsNoTracking()
                .Where(s => allSvcIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Reference, s.Designation, s.CoutHT })
                .ToListAsync(ct);
        var svcMap = services.ToDictionary(s => s.Id);

        var grouped = factures
            .GroupBy(f => f.ClientId)
            .Select(g =>
            {
                var c = clientMap.GetValueOrDefault(g.Key);

                var allLignes = g.SelectMany(f => f.Lignes).ToList();

                // Per-product / service sub-rows (profit before global discount)
                var productRows = allLignes
                    .Where(l => l.ProduitId is > 0)
                    .GroupBy(l => l.ProduitId!.Value)
                    .Select(pg =>
                    {
                        var p = prodMap.GetValueOrDefault(pg.Key);
                        var prixAchat = p?.PrixAchatHT ?? 0;
                        var ht = pg.Sum(l => l.Quantite * l.PrixUnitaireHT);
                        var cost = pg.Sum(l => l.Quantite * prixAchat);
                        var profit = ht - cost;
                        var marginPct = ht > 0 ? profit / ht * 100m : 0;
                        return new ReportSaleByCustomerProductRow(
                            p?.Reference ?? string.Empty,
                            p?.Designation ?? pg.First().Designation,
                            pg.Sum(l => l.Quantite),
                            ht,
                            ht,
                            dev,
                            profit,
                            marginPct);
                    });

                var serviceRows = allLignes
                    .Where(l => l.ServiceId is > 0)
                    .GroupBy(l => l.ServiceId!.Value)
                    .Select(sg =>
                    {
                        var s = svcMap.GetValueOrDefault(sg.Key);
                        var cout = s?.CoutHT ?? 0;
                        var ht = sg.Sum(l => l.Quantite * l.PrixUnitaireHT);
                        var cost = sg.Sum(l => l.Quantite * cout);
                        var profit = ht - cost;
                        var marginPct = ht > 0 ? profit / ht * 100m : 0;
                        return new ReportSaleByCustomerProductRow(
                            s?.Reference ?? string.Empty,
                            s?.Designation ?? sg.First().Designation,
                            sg.Sum(l => l.Quantite),
                            ht,
                            ht,
                            dev,
                            profit,
                            marginPct);
                    });

                var products = productRows.Concat(serviceRows)
                    .OrderByDescending(pr => pr.TotalTtc)
                    .ToList();

                // Client-level totals with profit (global discount applied)
                decimal totalHt = 0, totalCost = 0;
                foreach (var f in g)
                {
                    foreach (var l in f.Lignes)
                    {
                        var lht = l.Quantite * l.PrixUnitaireHT;
                        decimal unitCost = 0;
                        if (l.ProduitId is int pid)
                            unitCost = prodMap.GetValueOrDefault(pid)?.PrixAchatHT ?? 0;
                        else if (l.ServiceId is int sid)
                            unitCost = svcMap.GetValueOrDefault(sid)?.CoutHT ?? 0;
                        totalHt += lht;
                        totalCost += l.Quantite * unitCost;
                    }
                }
                var totalProfit = totalHt - totalCost;
                var marginPct = totalHt > 0 ? totalProfit / totalHt * 100m : 0;

                return new ReportSaleByCustomerRow(
                    g.Key,
                    c?.Nom ?? string.Empty,
                    c?.ICE ?? string.Empty,
                    c?.Ville ?? string.Empty,
                    g.Count(),
                    totalHt,
                    totalHt,
                    dev,
                    totalProfit,
                    marginPct,
                    products);
            })
            .OrderByDescending(r => r.TotalTtc)
            .ToList();

        return grouped;
    }

    public async Task<List<ReportRefundRow>> GetRefundsAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var toEnd = to.Date.AddDays(1);

        var avoirs = await db.Avoirs.AsNoTracking()
            .Where(a => a.Date >= from && a.Date < toEnd)
            .OrderByDescending(a => a.Date)
            .Select(a => new
            {
                a.Id,
                a.Numero,
                a.Date,
                a.ClientId,
                a.RetourMarchandise,
                a.TotalTtc
            })
            .ToListAsync(ct);

        var clientIds = avoirs.Select(a => a.ClientId).Distinct().ToList();
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => clientIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Nom })
            .ToListAsync(ct);
        var clientMap = clients.ToDictionary(c => c.Id);

        return avoirs.Select(a =>
            new ReportRefundRow(
                a.Numero ?? string.Empty,
                a.Date,
                clientMap.GetValueOrDefault(a.ClientId)?.Nom ?? string.Empty,
                string.Empty,
                a.RetourMarchandise,
                a.TotalTtc,
                dev)).ToList();
    }

    public async Task<List<ReportDailySaleRow>> GetDailySalesAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var toEnd = to.Date.AddDays(1);

        var factures = await db.Factures.AsNoTracking()
            .Where(f => f.Date >= from && f.Date < toEnd)
            .OrderBy(f => f.Date)
            .Select(f => new
            {
                f.Id,
                f.ClientId,
                f.Numero,
                f.Date,
                Lignes = f.Lignes!.Select(l => new
                {
                    l.ProduitId,
                    l.ServiceId,
                    l.Quantite,
                    l.PrixUnitaireHT
                }).ToList()
            })
            .ToListAsync(ct);

        var clientIds = factures.Select(f => f.ClientId).Distinct().ToList();
        var clients = await db.Tiers.AsNoTracking()
            .Where(t => clientIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Nom })
            .ToListAsync(ct);
        var clientMap = clients.ToDictionary(c => c.Id);

        var allProdIds = factures.SelectMany(f => f.Lignes).Where(l => l.ProduitId is > 0).Select(l => l.ProduitId!.Value).Distinct().ToList();
        var produits = allProdIds.Count == 0
            ? []
            : await db.Produits.AsNoTracking()
                .Where(p => allProdIds.Contains(p.Id))
                .Select(p => new { p.Id, p.PrixAchatHT })
                .ToListAsync(ct);
        var prodMap = produits.ToDictionary(p => p.Id);

        var allSvcIds = factures.SelectMany(f => f.Lignes).Where(l => l.ServiceId is > 0).Select(l => l.ServiceId!.Value).Distinct().ToList();
        var services = allSvcIds.Count == 0
            ? []
            : await db.Services.AsNoTracking()
                .Where(s => allSvcIds.Contains(s.Id))
                .Select(s => new { s.Id, s.CoutHT })
                .ToListAsync(ct);
        var svcMap = services.ToDictionary(s => s.Id);

        var grouped = factures
            .GroupBy(f => f.Date.Date)
            .Select(g =>
            {
                decimal dayHt = 0, dayCost = 0;

                var details = g.Select(f =>
                {
                    decimal ht = 0, cost = 0;
                    foreach (var l in f.Lignes)
                    {
                        var lht = l.Quantite * l.PrixUnitaireHT;
                        decimal unitCost = 0;
                        if (l.ProduitId is int pid)
                            unitCost = prodMap.GetValueOrDefault(pid)?.PrixAchatHT ?? 0;
                        else if (l.ServiceId is int sid)
                            unitCost = svcMap.GetValueOrDefault(sid)?.CoutHT ?? 0;
                        ht += lht;
                        cost += l.Quantite * unitCost;
                    }
                    dayHt += ht;
                    dayCost += cost;
                    var profit = ht - cost;
                    var marginPct = ht > 0 ? profit / ht * 100m : 0;
                    return new ReportDailySaleDetailRow(
                        f.Numero ?? string.Empty,
                        clientMap.GetValueOrDefault(f.ClientId)?.Nom ?? string.Empty,
                        ht,
                        ht,
                        dev,
                        profit,
                        marginPct);
                }).ToList();

                var dayProfit = dayHt - dayCost;
                var dayMargin = dayHt > 0 ? dayProfit / dayHt * 100m : 0;

                return new ReportDailySaleRow(
                    g.Key,
                    g.Count(),
                    dayHt,
                    0,
                    dayHt,
                    dev,
                    dayProfit,
                    dayMargin,
                    details);
            })
            .OrderByDescending(r => r.Date)
            .ToList();

        return grouped;
    }

    public async Task<List<ReportLowStockRow>> GetLowStockProductsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var produits = await db.Produits.AsNoTracking()
            .Where(p => p.Actif && p.StockMinimum > 0 && p.StockActuel <= p.StockMinimum)
            .OrderBy(p => p.Reference)
            .Select(p => new
            {
                p.Reference,
                p.Designation,
                p.StockActuel,
                p.StockMinimum
            })
            .Take(500)
            .ToListAsync(ct);

        return produits
            .Select(p => new ReportLowStockRow(p.Reference, p.Designation, p.StockActuel, p.StockMinimum))
            .ToList();
    }

    public async Task<List<ReportStockMovementRow>> GetStockMovementsAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var toEnd = to.Date.AddDays(1);

        var mouvements = await db.MouvementsStock.AsNoTracking()
            .Where(m => m.CreatedAt >= from && m.CreatedAt < toEnd)
            .Include(m => m.Produit)
            .OrderByDescending(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Take(500)
            .ToListAsync(ct);

        return mouvements.Select(m =>
        {
            var typeStr = m.Type switch
            {
                TypeMouvement.Entree => _locale.T("TypeMvt_Entree"),
                TypeMouvement.Sortie => _locale.T("TypeMvt_Sortie"),
                TypeMouvement.Ajustement => _locale.T("TypeMvt_Ajustement"),
                _ => m.Type.ToString()
            };
            var min = m.Produit?.StockMinimum ?? 0m;
            var shouldQuote = min > 0 && m.StockApres <= min
                ? _locale.T("Report_StockShouldQuote")
                : string.Empty;
            return new ReportStockMovementRow(
                m.CreatedAt,
                m.Produit?.Reference ?? string.Empty,
                m.Produit?.Designation ?? string.Empty,
                typeStr,
                m.Quantite,
                m.OrigineType,
                m.StockApres,
                shouldQuote);
        }).ToList();
    }

    public async Task<(decimal ht, decimal ttc, string devise)> GetStockValuationAsync(CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var produits = await db.Produits.AsNoTracking()
            .Where(p => p.StockActuel > 0)
            .Select(p => new { p.StockActuel, p.PrixAchatHT, p.PrixVenteHT, p.TauxTVA })
            .ToListAsync(ct);

        decimal totalHt = 0, totalTtc = 0;
        foreach (var p in produits)
        {
            totalHt += p.StockActuel * p.PrixAchatHT;
            totalTtc += p.StockActuel * p.PrixVenteHT * (1 + p.TauxTVA / 100m);
        }
        return (totalHt, totalTtc, dev);
    }

    public async Task<ReportProfitChargesResult> GetProfitChargesAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var toEnd = to.Date.AddDays(1);
        var rows = new List<ReportProfitChargeRow>();

        var typeMarge = _locale.T("Reports_TypeSaleMargin");
        var typeAchat = _locale.T("Reports_TypePurchase");
        var typeCharge = _locale.T("Reports_TypeCharge");
        var typeAvoirClient = _locale.T("Reports_TypeAvoirClient");
        var typeAvoirFournisseur = _locale.T("Reports_TypeAvoirFournisseur");

        var factures = await db.Factures.AsNoTracking()
            .Where(f => f.Date >= from && f.Date < toEnd)
            .Select(f => new
            {
                f.Numero,
                f.Date,
                Lignes = f.Lignes!.Select(l => new
                {
                    l.ProduitId,
                    l.ServiceId,
                    l.Quantite,
                    l.PrixUnitaireHT
                }).ToList()
            })
            .ToListAsync(ct);

        var avoirsClient = await db.Avoirs.AsNoTracking()
            .Where(a => a.Date >= from && a.Date < toEnd)
            .Select(a => new
            {
                a.Numero,
                a.Date,
                a.TotalTtc
            })
            .ToListAsync(ct);

        var allProdIds = factures.SelectMany(f => f.Lignes).Where(l => l.ProduitId is > 0).Select(l => l.ProduitId!.Value)
            .Distinct()
            .ToList();
        var prodMap = allProdIds.Count == 0
            ? new Dictionary<int, decimal>()
            : await db.Produits.AsNoTracking()
                .Where(p => allProdIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.PrixAchatHT, ct);

        var allSvcIds = factures.SelectMany(f => f.Lignes).Where(l => l.ServiceId is > 0).Select(l => l.ServiceId!.Value)
            .Distinct()
            .ToList();
        var svcMap = allSvcIds.Count == 0
            ? new Dictionary<int, decimal>()
            : await db.Services.AsNoTracking()
                .Where(s => allSvcIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.CoutHT, ct);

        decimal totalMargin = 0;
        decimal totalVente = 0;
        decimal totalAvoirsClient = 0;
        foreach (var f in factures)
        {
            decimal ttc = 0, costHt = 0;
            foreach (var l in f.Lignes)
            {
                ttc += l.Quantite * l.PrixUnitaireHT;
                if (l.ProduitId is int pid)
                    costHt += l.Quantite * prodMap.GetValueOrDefault(pid);
                else if (l.ServiceId is int sid)
                    costHt += l.Quantite * svcMap.GetValueOrDefault(sid);
            }
            var profit = ttc - costHt;
            totalMargin += profit;
            totalVente += ttc;
            rows.Add(new ReportProfitChargeRow(
                ReportProfitChargeKind.SaleMargin,
                typeMarge,
                f.Numero ?? string.Empty,
                f.Date,
                ttc,
                profit,
                dev,
                profit >= 0));
        }

        foreach (var a in avoirsClient)
        {
            var ttc = a.TotalTtc;
            totalAvoirsClient += ttc;
            rows.Add(new ReportProfitChargeRow(
                ReportProfitChargeKind.AvoirClient,
                typeAvoirClient,
                a.Numero ?? string.Empty,
                a.Date,
                ttc,
                -ttc,
                dev,
                false));
        }

        var facturesFournisseur = await db.FacturesFournisseurs.AsNoTracking()
            .Where(f => f.Date >= from && f.Date < toEnd)
            .Select(f => new
            {
                f.Numero,
                f.Date,
                f.RemiseGlobale,
                Lignes = f.Lignes!.Select(l => new
                {
                    l.Quantite,
                    l.PrixUnitaireHT,
                    l.Remise,
                    l.TauxTVA
                }).ToList()
            })
            .ToListAsync(ct);

        decimal totalPurchases = 0;
        decimal totalAvoirsFournisseur = 0;
        foreach (var f in facturesFournisseur)
        {
            var lignes = f.Lignes.Select(l => new FactureFournisseurLigne
            {
                Quantite = l.Quantite,
                PrixUnitaireHT = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTVA = l.TauxTVA
            }).ToList();
            var (_, _, ttc) = DocumentTotalsHelper.FactureFournisseurTotals(lignes, f.RemiseGlobale);
            totalPurchases += ttc;
            rows.Add(new ReportProfitChargeRow(
                ReportProfitChargeKind.Purchase,
                typeAchat,
                f.Numero ?? string.Empty,
                f.Date,
                ttc,
                -ttc,
                dev,
                false));
        }

        var avoirsFournisseur = await db.AvoirsFournisseurs.AsNoTracking()
            .Where(a => a.Date >= from && a.Date < toEnd)
            .Select(a => new
            {
                a.Numero,
                a.Date,
                Lignes = a.Lignes!.Select(l => new
                {
                    l.Quantite,
                    l.PrixUnitaireHT,
                    l.Remise,
                    l.TauxTVA
                }).ToList()
            })
            .ToListAsync(ct);

        foreach (var a in avoirsFournisseur)
        {
            var lignes = a.Lignes.Select(l => new AvoirFournisseurLigne
            {
                Quantite = l.Quantite,
                PrixUnitaireHT = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTVA = l.TauxTVA
            }).ToList();
            var (_, _, ttc) = DocumentTotalsHelper.AvoirFournisseurTotals(lignes);
            totalAvoirsFournisseur += ttc;
            rows.Add(new ReportProfitChargeRow(
                ReportProfitChargeKind.AvoirFournisseur,
                typeAvoirFournisseur,
                a.Numero ?? string.Empty,
                a.Date,
                ttc,
                ttc,
                dev,
                true));
        }

        var charges = await db.Charges.AsNoTracking()
            .Include(c => c.TypeCharge)
            .Include(c => c.Fournisseur)
            .Where(c => c.Date >= from && c.Date < toEnd)
            .ToListAsync(ct);

        decimal totalCharges = 0;
        foreach (var c in charges)
        {
            totalCharges += c.MontantTtc;
            var beneficiary = c.Fournisseur?.Nom;
            if (string.IsNullOrWhiteSpace(beneficiary))
                beneficiary = c.BeneficiaireLibre;
            var label = string.IsNullOrWhiteSpace(c.Libelle)
                ? beneficiary ?? string.Empty
                : string.IsNullOrWhiteSpace(beneficiary)
                    ? c.Libelle
                    : $"{c.Libelle} — {beneficiary}";

            rows.Add(new ReportProfitChargeRow(
                ReportProfitChargeKind.Charge,
                c.TypeCharge?.Nom ?? typeCharge,
                label,
                c.Date,
                c.MontantTtc,
                -c.MontantTtc,
                dev,
                false));
        }

        var sorted = rows.OrderByDescending(r => r.Date).ThenBy(r => r.TypeLabel).ToList();
        // Net = total vente + total avoir fournisseur - charge - achat - avoir client
        var net = totalVente + totalAvoirsFournisseur - totalCharges - totalPurchases - totalAvoirsClient;

        return new ReportProfitChargesResult
        {
            TotalSalesMargin = totalMargin,
            TotalVente = totalVente,
            TotalAvoirsClient = totalAvoirsClient,
            TotalPurchases = totalPurchases,
            TotalAvoirsFournisseur = totalAvoirsFournisseur,
            TotalCharges = totalCharges,
            NetResult = net,
            Devise = dev,
            Rows = sorted
        };
    }

    public async Task<ReportZakatResult> GetZakatAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var dev = await GetDeviseAsync(ct);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var toEnd = to.Date.AddDays(1);

        var produits = await db.Produits.AsNoTracking()
            .Where(p => p.StockActuel > 0)
            .Select(p => new { p.StockActuel, p.PrixAchatHT })
            .ToListAsync(ct);
        var stockHt = produits.Sum(p => p.StockActuel * p.PrixAchatHT);

        var clients = await db.Tiers.AsNoTracking()
            .Where(t => t.Type == TypeTiers.Client || t.Type == TypeTiers.LesDeux)
            .Select(t => new { t.Id, t.Nom })
            .ToListAsync(ct);

        // Soldes as of DateTo (include all ledger entries on or before the end date).
        var factureByClient = await db.Factures.AsNoTracking()
            .Where(f => f.Date < toEnd)
            .GroupBy(f => f.ClientId)
            .Select(g => new { ClientId = g.Key, Total = g.Sum(f => f.TotalTtc) })
            .ToDictionaryAsync(x => x.ClientId, x => x.Total, ct);

        var paiementByClient = await (
                from p in db.Paiements.AsNoTracking()
                join f in db.Factures.AsNoTracking() on p.FactureId equals f.Id
                where p.Montant > 0 && p.Date < toEnd
                group p by f.ClientId into g
                select new { ClientId = g.Key, Total = g.Sum(x => x.Montant) })
            .ToDictionaryAsync(x => x.ClientId, x => x.Total, ct);

        var avoirByClient = await db.Avoirs.AsNoTracking()
            .Where(a => a.Date < toEnd)
            .GroupBy(a => a.ClientId)
            .Select(g => new { ClientId = g.Key, Total = g.Sum(x => x.TotalTtc) })
            .ToDictionaryAsync(x => x.ClientId, x => x.Total, ct);

        var rows = new List<ReportZakatClientRow>();
        decimal totalBalances = 0;
        foreach (var c in clients.OrderBy(c => c.Nom))
        {
            var factures = factureByClient.GetValueOrDefault(c.Id);
            var avoirsTtc = avoirByClient.GetValueOrDefault(c.Id);
            var paiements = paiementByClient.GetValueOrDefault(c.Id);
            var solde = factures - avoirsTtc - paiements;
            if (Math.Abs(solde) < 0.01m) continue;

            totalBalances += solde;
            rows.Add(new ReportZakatClientRow(c.Nom, solde, dev));
        }

        var zakatBase = totalBalances + stockHt;
        var zakatAmount = Math.Round(zakatBase * 0.025m, 2, MidpointRounding.AwayFromZero);

        return new ReportZakatResult
        {
            TotalBalances = totalBalances,
            StockHt = stockHt,
            ZakatBase = zakatBase,
            ZakatAmount = zakatAmount,
            Devise = dev,
            Clients = rows
        };
    }

    private async Task<string> GetDeviseAsync(CancellationToken ct = default)
    {
        var cfg = await _settings.GetAsync(ct);
        return string.IsNullOrWhiteSpace(cfg.Devise) ? "MAD" : cfg.Devise!;
    }
}
