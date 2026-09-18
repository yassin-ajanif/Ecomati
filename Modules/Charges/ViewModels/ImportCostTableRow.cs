using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public sealed class ImportRmbExpenseMemory
{
    public string Libelle { get; set; } = string.Empty;
    public decimal Montant { get; set; }
}

/// <summary>Lightweight product pick for the import table designation Autocomplete.</summary>
public sealed class ImportProductPick
{
    public int Id { get; init; }
    public string Designation { get; init; } = string.Empty;
    public byte[]? ImageData { get; init; }
    public override string ToString() => Designation;
}

public partial class ImportCostTableRow : ObservableObject, IDisposable
{
    [ObservableProperty] private string _designation = string.Empty;
    [ObservableProperty] private int? _produitId;
    [ObservableProperty] private bool _isExistingProduct;
    [ObservableProperty] private Bitmap? _productImage;
    [ObservableProperty] private bool _hasProductImage;
    public byte[]? ProductImageData { get; private set; }
    public int NumericRow => HasProductImage ? 0 : 1;
    [ObservableProperty] private ImportProductPick? _selectedProduct;

    private bool _applyingProduct;
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

    /// <summary>Backward-compatible alias used by older bindings.</summary>
    public string Reference
    {
        get => Designation;
        set => Designation = value;
    }

    partial void OnSelectedProductChanged(ImportProductPick? value)
    {
        if (value is null)
            return;
        ApplyProduct(value.Id, value.Designation, value.ImageData);
    }

    partial void OnHasProductImageChanged(bool value) =>
        OnPropertyChanged(nameof(NumericRow));

    partial void OnDesignationChanged(string value)
    {
        if (_applyingProduct)
            return;
        if (SelectedProduct is not null &&
            string.Equals(SelectedProduct.Designation, value, StringComparison.Ordinal))
            return;

        ClearProductLink();
    }

    public void ApplyProduct(int produitId, string designation, byte[]? imageData)
    {
        _applyingProduct = true;
        try
        {
            ProduitId = produitId;
            IsExistingProduct = true;
            Designation = designation;
            SetProductImage(imageData);
        }
        finally
        {
            _applyingProduct = false;
        }
    }

    public void ClearProductLink()
    {
        ProduitId = null;
        IsExistingProduct = false;
        if (SelectedProduct is not null)
            SelectedProduct = null;
        SetProductImage(null);
    }

    public void SetProductImage(byte[]? bytes)
    {
        ProductImage?.Dispose();
        ProductImage = null;
        HasProductImage = false;
        ProductImageData = null;
        if (bytes is null || bytes.Length == 0)
            return;
        ProductImageData = bytes;
        try
        {
            using var ms = new MemoryStream(bytes);
            ProductImage = new Bitmap(ms);
            HasProductImage = true;
        }
        catch
        {
            ProductImage = null;
            HasProductImage = false;
        }
    }

    public void Dispose()
    {
        ProductImage?.Dispose();
        ProductImage = null;
    }

    partial void OnPmChanged(decimal? value) => RecalcDerived();
    partial void OnPcChanged(decimal? value) => RecalcDerived();
    partial void OnRmbChanged(decimal? value) => RecalcDerived();
    partial void OnCntPsChanged(decimal? value) => RecalcTm();
    partial void OnCntColisChanged(decimal? value) => RecalcDownstreamFromColis();

    /// <summary>Recompute all derived columns (PAF, M, TM, M/NET, CA, T/M).</summary>
    public void RecalcDerived()
    {
        if (Rmb is decimal rmb && Pc is decimal pc)
            LaDouane = decimal.Round(rmb * pc, 2);
        else
            LaDouane = null;

        if (Pm is decimal pm && LaDouane is decimal paf)
            M = decimal.Round(pm - paf, 2);
        else
            M = null;

        RecalcTm();
    }

    private void RecalcTm()
    {
        if (M is decimal m && CntPs is decimal cnt)
            Tm = decimal.Round(m * cnt, 2);
        else
            Tm = null;

        RecalcDownstreamFromColis();
    }

    private void RecalcDownstreamFromColis()
    {
        if (Tm is decimal tm && CntColis is decimal colisForNet)
            MNet = decimal.Round(tm * colisForNet, 2);
        else
            MNet = null;

        if (LaDouane is decimal paf && CntPs is decimal cnt && CntColis is decimal colisForCa)
            Ca = decimal.Round(paf * cnt * colisForCa, 2);
        else
            Ca = null;

        if (Pm is decimal pm && CntPs is decimal cntPs && CntColis is decimal cntColis)
            TMarge = decimal.Round(pm * cntPs * cntColis, 2);
        else
            TMarge = null;
    }
}
