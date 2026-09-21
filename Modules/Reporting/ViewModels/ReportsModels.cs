using System;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionCommerciale.Shared.Helpers;

namespace GestionCommerciale.Modules.Reporting.ViewModels;

public sealed class ReportSaleByProductRow
{
    public ReportSaleByProductRow(string reference, string designation, string categorie,
        decimal quantite, decimal totalHt, decimal totalTtc, string devise,
        decimal profit, decimal marginPct)
    {
        Reference = reference;
        Designation = designation;
        Categorie = categorie;
        Quantite = quantite;
        TotalHt = totalHt;
        TotalTtc = totalTtc;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblQty = quantite.ToString("N2");
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
    }

    public string Reference { get; }
    public string Designation { get; }
    public string Categorie { get; }
    public decimal Quantite { get; }
    public decimal TotalHt { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblQty { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }
}

public sealed class ReportSaleByCustomerProductRow
{
    public ReportSaleByCustomerProductRow(string reference, string designation,
        decimal quantite, decimal totalHt, decimal totalTtc, string devise,
        decimal profit, decimal marginPct)
    {
        Reference = reference;
        Designation = designation;
        Quantite = quantite;
        TotalHt = totalHt;
        TotalTtc = totalTtc;
        UnitPrice = quantite > 0 ? totalTtc / quantite : 0;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblQty = FormatQty(quantite);
        LblHt = $"{totalHt:N2} {devise}";
        LblUnitPrice = $"{UnitPrice:N2} {devise}";
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
    }

    public string Reference { get; }
    public string Designation { get; }
    public decimal Quantite { get; }
    public decimal UnitPrice { get; }
    public decimal TotalHt { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblQty { get; }
    public string LblHt { get; }
    public string LblUnitPrice { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }

    internal static string FormatQty(decimal qty) =>
        qty == decimal.Truncate(qty) ? qty.ToString("N0") : qty.ToString("N2");
}

public sealed partial class ReportSaleByCustomerDayRow : ObservableObject
{
    public ReportSaleByCustomerDayRow(DateTime date, int nbFactures,
        decimal totalHt, decimal totalTtc, string devise,
        decimal profit, decimal marginPct,
        CultureInfo culture,
        List<ReportSaleByCustomerProductRow>? products = null)
    {
        Date = date;
        NbFactures = nbFactures;
        TotalHt = totalHt;
        TotalTtc = totalTtc;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblDayName = DisplayDateHelper.DayName(date, culture);
        LblDate = DisplayDateHelper.ShortDatePdf(date);
        LblDayLabel = DisplayDateHelper.DayLabel(date, culture);
        LblCount = nbFactures.ToString();
        LblHt = $"{totalHt:N2} {devise}";
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
        if (products != null)
        {
            foreach (var p in products)
                _products.Add(p);
        }
    }

    public DateTime Date { get; }
    public int NbFactures { get; }
    public decimal TotalHt { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblDayName { get; }
    public string LblDate { get; }
    public string LblDayLabel { get; }
    public string LblCount { get; }
    public string LblHt { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }

    [ObservableProperty]
    private bool _isExpanded;

    private readonly ObservableCollection<ReportSaleByCustomerProductRow> _products = [];
    public ObservableCollection<ReportSaleByCustomerProductRow> Products => _products;
}

public sealed partial class ReportSaleByCustomerRow : ObservableObject
{
    public ReportSaleByCustomerRow(int clientId, string client, string ice, string ville,
        int nbFactures, decimal totalHt, decimal totalTtc, string devise,
        decimal profit, decimal marginPct,
        List<ReportSaleByCustomerDayRow>? days = null)
    {
        ClientId = clientId;
        Client = client;
        Ice = ice;
        Ville = ville;
        NbFactures = nbFactures;
        TotalHt = totalHt;
        TotalTtc = totalTtc;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblCount = nbFactures.ToString();
        LblHt = $"{totalHt:N2} {devise}";
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
        if (days != null)
        {
            foreach (var d in days)
                _days.Add(d);
        }
    }

    public int ClientId { get; }
    public string Client { get; }
    public string Ice { get; }
    public string Ville { get; }
    public int NbFactures { get; }
    public decimal TotalHt { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblCount { get; }
    public string LblHt { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }

    [ObservableProperty]
    private bool _isExpanded;

    private readonly ObservableCollection<ReportSaleByCustomerDayRow> _days = [];
    public ObservableCollection<ReportSaleByCustomerDayRow> Days => _days;
}

public sealed class ReportRefundRow
{
    public ReportRefundRow(string numero, DateTime date, string client,
        string motif, bool retourMarchandise, decimal totalTtc, string devise)
    {
        Numero = numero;
        Date = date;
        Client = client;
        Motif = motif;
        RetourMarchandise = retourMarchandise;
        TotalTtc = totalTtc;
        Devise = devise;
        LblDate = date.ToString("d");
        LblTotal = $"{totalTtc:N2} {devise}";
        LblRetour = retourMarchandise ? "\u2713" : "";
    }

    public string Numero { get; }
    public DateTime Date { get; }
    public string Client { get; }
    public string Motif { get; }
    public bool RetourMarchandise { get; }
    public decimal TotalTtc { get; }
    public string Devise { get; }
    public string LblDate { get; }
    public string LblTotal { get; }
    public string LblRetour { get; }
}

public sealed class ReportDailySaleDetailRow
{
    public ReportDailySaleDetailRow(string numero, string client,
        decimal totalHt, decimal totalTtc, string devise,
        decimal profit, decimal marginPct)
    {
        Numero = numero;
        Client = client;
        TotalHt = totalHt;
        TotalTtc = totalTtc;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblHt = $"{totalHt:N2} {devise}";
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
    }

    public string Numero { get; }
    public string Client { get; }
    public decimal TotalHt { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblHt { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }
}

public sealed partial class ReportDailySaleRow : ObservableObject
{
    public ReportDailySaleRow(DateTime date, int nbFactures,
        decimal totalHt, decimal totalTva, decimal totalTtc, string devise,
        decimal profit, decimal marginPct,
        List<ReportDailySaleDetailRow>? details = null)
    {
        Date = date;
        NbFactures = nbFactures;
        TotalHt = totalHt;
        TotalTva = totalTva;
        TotalTtc = totalTtc;
        Profit = profit;
        MarginPct = marginPct;
        Devise = devise;
        LblDate = date.ToString("d");
        LblCount = nbFactures.ToString();
        LblHt = $"{totalHt:N2} {devise}";
        LblTva = $"{totalTva:N2} {devise}";
        LblTtc = $"{totalTtc:N2} {devise}";
        LblProfit = $"{profit:N2} {devise}";
        LblMargin = $"{marginPct:N1}%";
        if (details != null)
        {
            foreach (var d in details)
                _details.Add(d);
        }
    }

    public DateTime Date { get; }
    public int NbFactures { get; }
    public decimal TotalHt { get; }
    public decimal TotalTva { get; }
    public decimal TotalTtc { get; }
    public decimal Profit { get; }
    public decimal MarginPct { get; }
    public string Devise { get; }
    public string LblDate { get; }
    public string LblCount { get; }
    public string LblHt { get; }
    public string LblTva { get; }
    public string LblTtc { get; }
    public string LblProfit { get; }
    public string LblMargin { get; }

    [ObservableProperty]
    private bool _isExpanded;

    private readonly ObservableCollection<ReportDailySaleDetailRow> _details = [];
    public ObservableCollection<ReportDailySaleDetailRow> Details => _details;
}

public enum ReportProfitChargeKind
{
    SaleMargin,
    AvoirClient,
    Purchase,
    AvoirFournisseur,
    Charge
}

public sealed class ReportProfitChargeRow
{
    public ReportProfitChargeRow(
        ReportProfitChargeKind kind,
        string typeLabel,
        string refLibelle,
        DateTime date,
        decimal montantHt,
        decimal amount,
        string devise,
        bool isPositive)
    {
        Kind = kind;
        TypeLabel = typeLabel;
        RefLibelle = refLibelle;
        Date = date;
        MontantHt = montantHt;
        Amount = amount;
        Devise = devise;
        IsPositive = isPositive;
        LblDate = date.ToString("d");
        LblMontantHt = montantHt > 0 ? $"{montantHt:N2} {devise}" : "—";
        var sign = amount >= 0 ? "+" : "";
        LblAmount = $"{sign}{amount:N2} {devise}";
    }

    public ReportProfitChargeKind Kind { get; }
    public string TypeLabel { get; }
    public string RefLibelle { get; }
    public DateTime Date { get; }
    public decimal MontantHt { get; }
    public decimal Amount { get; }
    public string Devise { get; }
    public bool IsPositive { get; }
    public string LblDate { get; }
    public string LblMontantHt { get; }
    public string LblAmount { get; }
}

public sealed class ReportProfitChargesResult
{
    public required decimal TotalSalesMargin { get; init; }
    public required decimal TotalVente { get; init; }
    public required decimal TotalAvoirsClient { get; init; }
    public required decimal TotalPurchases { get; init; }
    public required decimal TotalAvoirsFournisseur { get; init; }
    public required decimal TotalCharges { get; init; }
    public required decimal NetResult { get; init; }
    public required string Devise { get; init; }
    public required List<ReportProfitChargeRow> Rows { get; init; }
}

public sealed class ReportStockMovementRow
{
    public ReportStockMovementRow(DateTime date, string produitRef, string produitDesignation,
        string typeMvt, decimal quantite, string origine, decimal stockApres, string status)
    {
        Date = date;
        ProduitRef = produitRef;
        ProduitDesignation = produitDesignation;
        TypeMvt = typeMvt;
        Quantite = quantite;
        Origine = origine;
        StockApres = stockApres;
        Status = status;
        LblDate = date.ToString("g");
        LblQty = quantite.ToString("N2");
        LblStockApres = stockApres.ToString("N2");
        ShowShouldQuote = !string.IsNullOrWhiteSpace(status);
    }

    public DateTime Date { get; }
    public string ProduitRef { get; }
    public string ProduitDesignation { get; }
    public string TypeMvt { get; }
    public decimal Quantite { get; }
    public string Origine { get; }
    public decimal StockApres { get; }
    public string Status { get; }
    public bool ShowShouldQuote { get; }
    public string LblDate { get; }
    public string LblQty { get; }
    public string LblStockApres { get; }
}

public sealed class ReportLowStockRow
{
    public ReportLowStockRow(string reference, string designation, decimal stockActuel, decimal stockMinimum)
    {
        Reference = reference;
        Designation = designation;
        StockActuel = stockActuel;
        StockMinimum = stockMinimum;
        LblStockActuel = stockActuel.ToString("N2");
        LblStockMinimum = stockMinimum.ToString("N2");
    }

    public string Reference { get; }
    public string Designation { get; }
    public decimal StockActuel { get; }
    public decimal StockMinimum { get; }
    public string LblStockActuel { get; }
    public string LblStockMinimum { get; }
}

public sealed class ReportZakatClientRow
{
    public ReportZakatClientRow(string client, decimal solde, string devise)
    {
        Client = client;
        Solde = solde;
        Devise = devise;
        LblSolde = $"{solde:N2} {devise}";
    }

    public string Client { get; }
    public decimal Solde { get; }
    public string Devise { get; }
    public string LblSolde { get; }
}

public sealed class ReportZakatResult
{
    public required decimal TotalBalances { get; init; }
    public required decimal StockHt { get; init; }
    public required decimal ZakatBase { get; init; }
    public required decimal ZakatAmount { get; init; }
    public required string Devise { get; init; }
    public required List<ReportZakatClientRow> Clients { get; init; }
}
