using Avalonia.Media.Imaging;

namespace GestionCommerciale.Modules.Charges.ViewModels;

public sealed class ImportCalculPreviewRow : IDisposable
{
    public string Designation { get; init; } = string.Empty;
    public decimal Pm { get; init; }
    public decimal Pc { get; init; }
    public decimal Rmb { get; init; }
    public decimal LaDouane { get; init; }
    public decimal M { get; init; }
    public decimal CntPs { get; init; }
    public decimal Tm { get; init; }
    public decimal CntColis { get; init; }
    public decimal MNet { get; init; }
    public decimal Ca { get; init; }
    public decimal TMarge { get; init; }
    public Bitmap? ProductImage { get; private set; }
    public bool HasProductImage { get; private set; }
    public byte[]? ProductImageData { get; private set; }
    public int NumericRow => HasProductImage ? 0 : 1;

    public static ImportCalculPreviewRow FromLigne(string designation, decimal pm, decimal pc, decimal rmb,
        decimal laDouane, decimal m, decimal cntPs, decimal tm, decimal cntColis, decimal mNet, decimal ca,
        decimal tMarge, byte[]? imageData)
    {
        var row = new ImportCalculPreviewRow
        {
            Designation = designation,
            Pm = pm,
            Pc = pc,
            Rmb = rmb,
            LaDouane = laDouane,
            M = m,
            CntPs = cntPs,
            Tm = tm,
            CntColis = cntColis,
            MNet = mNet,
            Ca = ca,
            TMarge = tMarge
        };
        row.SetProductImage(imageData);
        return row;
    }

    private void SetProductImage(byte[]? bytes)
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

}
