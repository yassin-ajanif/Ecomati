using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Charges.Models;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Models.Pdf;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public partial class ImportCalculPreviewDialogViewModel : ObservableObject
{
    private readonly Action _close;
    private readonly Action<int> _edit;
    private readonly ILocaleService _locale;
    private readonly IPdfService _pdf;
    private readonly IDialogService _dialog;

    private ImportCalculPreviewDialogViewModel(
        ILocaleService locale,
        IPdfService pdf,
        IDialogService dialog,
        Action close,
        Action<int> edit)
    {
        _locale = locale;
        _pdf = pdf;
        _dialog = dialog;
        _close = close;
        _edit = edit;
    }

    public string Title { get; private init; } = string.Empty;
    public string Subtitle { get; private init; } = string.Empty;
    public string LblTotal { get; private init; } = string.Empty;
    public string BtnClose { get; private init; } = string.Empty;
    public string BtnEdit { get; private init; } = string.Empty;
    public string BtnPdf { get; private init; } = string.Empty;
    public int ImportCalculId { get; private init; }
    public string ColDesignation { get; private init; } = string.Empty;
    public string ColPm { get; private init; } = string.Empty;
    public string ColPc { get; private init; } = string.Empty;
    public string ColRmb { get; private init; } = string.Empty;
    public string ColLaDouane { get; private init; } = string.Empty;
    public string ColM { get; private init; } = string.Empty;
    public string ColCntPs { get; private init; } = string.Empty;
    public string ColTm { get; private init; } = string.Empty;
    public string ColCntColis { get; private init; } = string.Empty;
    public string ColMNet { get; private init; } = string.Empty;
    public string ColCa { get; private init; } = string.Empty;
    public string ColTMarge { get; private init; } = string.Empty;
    public decimal TotalMNet { get; private init; }
    public decimal TotalCa { get; private init; }
    public decimal TotalTMarge { get; private init; }
    public string TotalMNetLabel { get; private init; } = string.Empty;
    public string TotalCaLabel { get; private init; } = string.Empty;
    public string TotalTMargeLabel { get; private init; } = string.Empty;
    public string Devise { get; private init; } = "dh";
    public ObservableCollection<ImportCalculPreviewRow> Rows { get; } = [];

    public static ImportCalculPreviewDialogViewModel Create(
        ImportCalcul entity,
        ILocaleService locale,
        IPdfService pdf,
        IDialogService dialog,
        Action close,
        Action<int> edit)
    {
        var devise = CurrencyHelper.NormalizeImportDevise(entity.Devise);
        var vm = new ImportCalculPreviewDialogViewModel(locale, pdf, dialog, close, edit)
        {
            ImportCalculId = entity.Id,
            Title = entity.Libelle,
            Devise = devise,
            Subtitle = $"{entity.Date:dd/MM/yyyy}  —  {devise}",
            LblTotal = locale.T("Calc_Total"),
            BtnClose = locale.T("Btn_Cancel"),
            BtnEdit = locale.T("Calc_Edit"),
            BtnPdf = locale.T("Btn_Pdf"),
            ColDesignation = locale.T("Calc_ColDesignation"),
            ColPm = locale.T("Calc_ColPm"),
            ColPc = locale.T("Calc_ColPc"),
            ColRmb = locale.T("Calc_ColRmb"),
            ColLaDouane = locale.T("Calc_ColLaDouane"),
            ColM = locale.T("Calc_ColM"),
            ColCntPs = locale.T("Calc_ColCntPs"),
            ColTm = locale.T("Calc_ColTm"),
            ColCntColis = locale.T("Calc_ColCntColis"),
            ColMNet = locale.T("Calc_ColMNet"),
            ColCa = locale.T("Calc_ColCa"),
            ColTMarge = locale.T("Calc_ColTMarge"),
            TotalMNet = entity.TotalMNet,
            TotalCa = entity.TotalCa,
            TotalTMarge = entity.TotalTMarge,
            TotalMNetLabel = CurrencyHelper.Format(entity.TotalMNet, devise),
            TotalCaLabel = CurrencyHelper.Format(entity.TotalCa, devise),
            TotalTMargeLabel = CurrencyHelper.Format(entity.TotalTMarge, devise)
        };

        foreach (var ligne in entity.Lignes.OrderBy(l => l.Ordre))
        {
            vm.Rows.Add(ImportCalculPreviewRow.FromLigne(
                ligne.Designation,
                ligne.Pm,
                ligne.Pc,
                ligne.Rmb,
                ligne.LaDouane,
                ligne.M,
                ligne.CntPs,
                ligne.Tm,
                ligne.CntColis,
                ligne.MNet,
                ligne.Ca,
                ligne.TMarge,
                ligne.Produit?.ImageData));
        }

        return vm;
    }

    [RelayCommand]
    private void Close() => _close();

    [RelayCommand]
    private void Edit()
    {
        _edit(ImportCalculId);
        _close();
    }

    [RelayCommand]
    private async Task ExportPdfAsync(CancellationToken cancellationToken)
    {
        if (Rows.Count == 0)
        {
            await _dialog.ShowInfoAsync(_locale.T("Export_Pdf"), _locale.T("Calc_PdfEmpty"), cancellationToken);
            return;
        }

        try
        {
            var right = PdfTextAlignment.End;
            var rows = Rows.Select(r => new ReportPdfRow
            {
                Cells =
                [
                    r.Designation,
                    r.Pm.ToString("N2"),
                    r.Pc.ToString("N2"),
                    r.Rmb.ToString("N2"),
                    r.LaDouane.ToString("N2"),
                    r.M.ToString("N2"),
                    r.CntPs.ToString("N0"),
                    r.Tm.ToString("N2"),
                    r.CntColis.ToString("N0"),
                    r.MNet.ToString("N2"),
                    r.Ca.ToString("N2"),
                    r.TMarge.ToString("N2")
                ],
                CellImages = r.ProductImageData is { Length: > 0 }
                    ? new byte[]?[] { r.ProductImageData }
                    : null
            }).ToList();

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

            var model = new ReportPdfModel
            {
                Title = Title,
                PeriodLabel = Subtitle,
                Columns =
                [
                    new(ColDesignation, 4.5f),
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

            var bytes = await _pdf.BuildReportPdfAsync(model, cancellationToken);
            var safeName = string.IsNullOrWhiteSpace(Title) ? "import-calc" : Title;
            foreach (var c in Path.GetInvalidFileNameChars())
                safeName = safeName.Replace(c, '-');
            var fileName = $"{safeName}-{DateTime.Today:yyyy-MM-dd}.pdf";
            var ok = await _dialog.SavePickedFileBytesAsync(
                _locale.T("Export_PdfPicker"), fileName, new[] { "*.pdf" }, bytes, cancellationToken);
            if (ok)
                await _dialog.ShowInfoAsync(_locale.T("Export_Pdf"), _locale.T("Export_Done"), cancellationToken);
        }
        catch (Exception ex)
        {
            AppLog.Error("Échec de l'export PDF du calcul import", ex, "ImportCalculPreviewDialogViewModel.ExportPdfAsync");
            await _dialog.ShowErrorAsync(_locale.T("Export_Pdf"), ex.Message, cancellationToken);
        }
    }

    private string FmtTotal(decimal value) => CurrencyHelper.Format(value, Devise);
}
