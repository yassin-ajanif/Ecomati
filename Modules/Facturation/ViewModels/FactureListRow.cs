using System.Globalization;
using FactureEntity = GestionCommerciale.Modules.Facturation.Models.Facture;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Facturation.ViewModels;

public sealed class FactureListRow
{
    public required FactureEntity Facture { get; init; }
    public string ClientNom { get; init; } = string.Empty;
    public string DateShort { get; init; } = string.Empty;
    public string StatutLabel { get; init; } = string.Empty;
    public string TtcLabel { get; init; } = string.Empty;

    public static FactureListRow Create(FactureEntity f, string clientNom, string devise, ILocaleService locale)
    {
        var ttc = f.TotalTtc;
        var paid = DocumentTotalsHelper.IsFacturePaid(f.TotalTtc, f.Paiements);
        return new FactureListRow
        {
            Facture = f,
            ClientNom = clientNom,
            DateShort = f.Date.ToString("d", CultureInfo.CurrentCulture),
            StatutLabel = paid ? locale.T("Fact_Paid") : locale.T("Fact_Unpaid"),
            TtcLabel = $"{ttc:N2} {devise}",
        };
    }
}
