using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Facturation.Services;

internal static class ReglementGroupeSync
{
    public static async Task RecalculateAsync(
        AppDbContext db,
        int? groupeId,
        int? excludedPaiementId,
        decimal replacementAmount,
        CancellationToken cancellationToken)
    {
        if (groupeId is not int id)
            return;

        var slices = await db.Paiements.AsNoTracking()
            .Where(p => p.ReglementGroupeId == id && p.Id != excludedPaiementId)
            .SumAsync(p => (decimal?)p.Montant, cancellationToken) ?? 0m;

        var total = Math.Round(slices + replacementAmount, 2, MidpointRounding.AwayFromZero);
        var header = await db.ReglementsGroupes.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
        if (header == null)
            return;

        if (total <= 0)
            db.ReglementsGroupes.Remove(header);
        else
            header.Montant = total;
    }
}
