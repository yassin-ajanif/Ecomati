using System.Globalization;
using GestionCommerciale.Modules.Facturation.Models;

namespace GestionCommerciale.Modules.Facturation.ViewModels;

public sealed class AvoirListRow
{
    public required Avoir Avoir { get; init; }
    public string ClientNom { get; init; } = string.Empty;
    public string FactureNumero { get; init; } = string.Empty;
    public string DateShort { get; init; } = string.Empty;
    public string TtcLabel { get; init; } = string.Empty;

    public static AvoirListRow Create(Avoir avoir, string clientNom, string factureNumero, string devise)
    {
        return new AvoirListRow
        {
            Avoir = avoir,
            ClientNom = clientNom,
            FactureNumero = factureNumero,
            DateShort = avoir.Date.ToString("d", CultureInfo.CurrentCulture),
            TtcLabel = $"{avoir.TotalTtc:N2} {devise}",
        };
    }
}
