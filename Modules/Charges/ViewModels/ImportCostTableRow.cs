using CommunityToolkit.Mvvm.ComponentModel;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public sealed class ImportRmbExpenseMemory
{
    public string Libelle { get; set; } = string.Empty;
    public decimal Montant { get; set; }
}

public partial class ImportCostTableRow : ObservableObject
{
    [ObservableProperty] private string _reference = string.Empty;
    [ObservableProperty] private decimal? _pm;
    [ObservableProperty] private decimal? _pc;
    [ObservableProperty] private decimal? _rmb;
    [ObservableProperty] private decimal? _laDouane;
    [ObservableProperty] private decimal? _m;
    [ObservableProperty] private decimal? _cntPs;
    [ObservableProperty] private decimal? _tm;
    [ObservableProperty] private decimal? _cntColis;
    [ObservableProperty] private decimal? _mNet;
    [ObservableProperty] private decimal? _ca;
    [ObservableProperty] private decimal? _tMarge;

    /// <summary>Remembered gros + frais for the RMB coefficient dialog.</summary>
    public decimal? RmbGrosPrice { get; set; }
    public List<ImportRmbExpenseMemory> RmbExpenses { get; } = [];
    public bool HasRmbMemory => RmbGrosPrice.HasValue || RmbExpenses.Count > 0;

    partial void OnPmChanged(decimal? value) => RecalcDerived();
    partial void OnPcChanged(decimal? value) => RecalcDerived();
    partial void OnRmbChanged(decimal? value) => RecalcDerived();
    partial void OnCntPsChanged(decimal? value) => RecalcTm();
    partial void OnCntColisChanged(decimal? value) => RecalcDownstreamFromColis();

    private void RecalcDerived()
    {
        // PAF = RMB × P/C
        if (Rmb is decimal rmb && Pc is decimal pc)
            LaDouane = rmb * pc;
        else
            LaDouane = null;

        // M = P/M − PAF  (e.g. 40 − 2.5×11)
        if (Pm is decimal pm && LaDouane is decimal paf)
            M = pm - paf;
        else
            M = null;

        RecalcTm();
    }

    private void RecalcTm()
    {
        // T M = M × CNT PS  (e.g. 12.5 × 32)
        if (M is decimal m && CntPs is decimal cnt)
            Tm = m * cnt;
        else
            Tm = null;

        RecalcDownstreamFromColis();
    }

    private void RecalcDownstreamFromColis()
    {
        // M/NET = T M × CNT COLIS  (e.g. 400 × 18)
        if (Tm is decimal tm && CntColis is decimal colisForNet)
            MNet = tm * colisForNet;
        else
            MNet = null;

        // CA = PAF × CNT PS × CNT COLIS  (e.g. 27.5 × 32 × 18)
        if (LaDouane is decimal paf && CntPs is decimal cnt && CntColis is decimal colisForCa)
            Ca = paf * cnt * colisForCa;
        else
            Ca = null;

        // T/M = P/M × CNT PS × CNT COLIS  (e.g. 40 × 32 × 18)
        if (Pm is decimal pm && CntPs is decimal cntPs && CntColis is decimal cntColis)
            TMarge = pm * cntPs * cntColis;
        else
            TMarge = null;
    }
}
