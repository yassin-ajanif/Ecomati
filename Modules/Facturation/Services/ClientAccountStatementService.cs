using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GestionCommerciale.Modules.Facturation.Services;

public sealed class ClientAccountStatementService : IClientAccountStatementService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILocaleService _locale;

    public ClientAccountStatementService(IDbContextFactory<AppDbContext> dbFactory, ILocaleService locale)
    {
        _dbFactory = dbFactory;
        _locale = locale;
    }

    public async Task<ClientAccountStatementResult> GetStatementAsync(int clientId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var factures = await db.Factures.AsNoTracking()
            .Where(f => f.ClientId == clientId)
            .Select(f => new
            {
                f.Id,
                f.Numero,
                f.Date,
                f.TotalTtc,
                Paiements = f.Paiements!.Select(p => new
                {
                    p.Id,
                    p.Date,
                    p.Montant,
                    p.Mode,
                    p.Reference,
                    p.ReglementGroupeId
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var avoirs = await db.Avoirs.AsNoTracking()
            .Where(a => a.ClientId == clientId)
            .Select(a => new
            {
                a.Id,
                a.Numero,
                a.Date,
                a.TotalTtc
            })
            .ToListAsync(cancellationToken);

        var entries = new List<(DateTime Date, ClientAccountEntryKind Kind, long TieBreakId, string Designation, string Observation, decimal Debit, decimal Credit, int? GroupeId)>();

        foreach (var f in factures)
        {
            var ttc = f.TotalTtc;
            if (ttc <= 0) continue;

            entries.Add((
                f.Date.Date,
                ClientAccountEntryKind.Facture,
                f.Id,
                _locale.Tf("ClientLedger_FactureFmt", f.Numero),
                string.Empty,
                ttc,
                0,
                null));
        }

        foreach (var a in avoirs)
        {
            var ttc = a.TotalTtc;
            if (ttc <= 0) continue;

            entries.Add((
                a.Date.Date,
                ClientAccountEntryKind.Avoir,
                a.Id,
                _locale.Tf("ClientLedger_AvoirFmt", a.Numero),
                string.Empty,
                0,
                ttc,
                null));
        }

        foreach (var f in factures)
        {
            foreach (var p in f.Paiements)
            {
                if (p.Montant <= 0 || p.Mode == ModePaiement.Credit || p.ReglementGroupeId != null)
                    continue;
                var observation = string.IsNullOrWhiteSpace(p.Reference) ? string.Empty : p.Reference.Trim();
                entries.Add((
                    p.Date.Date,
                    ClientAccountEntryKind.Paiement,
                    p.Id,
                    PaymentDesignation(p.Mode),
                    observation,
                    0,
                    p.Montant,
                    null));
            }
        }

        var sliceRows = await (
            from p in db.Paiements.AsNoTracking()
            join f in db.Factures.AsNoTracking() on p.FactureId equals f.Id
            where p.ReglementGroupeId != null && f.ClientId == clientId && p.Montant > 0
            select new { GroupeId = p.ReglementGroupeId!.Value, f.Numero, p.Montant }
        ).ToListAsync(cancellationToken);
        var allocations = ReglementAllocationLines.ByGroup(sliceRows.Select(s => (s.GroupeId, s.Numero, s.Montant)));

        var groupes = await db.ReglementsGroupes.AsNoTracking()
            .Where(g => g.TiersId == clientId && g.Sens == SensReglement.Encaissement && g.Montant > 0)
            .ToListAsync(cancellationToken);
        foreach (var g in groupes)
        {
            if (g.Mode == ModePaiement.Credit)
                continue;
            var observation = string.IsNullOrWhiteSpace(g.Reference) ? string.Empty : g.Reference.Trim();
            entries.Add((
                g.Date.Date,
                ClientAccountEntryKind.Paiement,
                g.Id,
                PaymentDesignation(g.Mode),
                observation,
                0,
                g.Montant,
                g.Id));
        }

        var ordered = entries
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Kind)
            .ThenBy(e => e.TieBreakId)
            .ToList();

        decimal balance = 0;
        var rows = new List<ClientAccountStatementRow>(ordered.Count);
        foreach (var e in ordered)
        {
            balance += e.Debit - e.Credit;
            rows.Add(new ClientAccountStatementRow
            {
                Date = e.Date,
                Kind = e.Kind,
                TieBreakId = e.TieBreakId,
                Designation = e.Designation,
                Observation = e.Observation,
                Debit = e.Debit,
                Credit = e.Credit,
                Balance = balance
            });

            if (e.GroupeId is int groupeId
                && allocations.TryGetValue(groupeId, out var docs)
                && docs.Count > 1)
            {
                foreach (var doc in docs)
                {
                    rows.Add(new ClientAccountStatementRow
                    {
                        Date = e.Date,
                        Kind = ClientAccountEntryKind.Paiement,
                        TieBreakId = groupeId,
                        Designation = _locale.Tf("ClientLedger_FactureFmt", doc.Numero),
                        Observation = doc.Amount.ToString("N2", CultureInfo.GetCultureInfo("fr-FR")),
                        AllocationAmount = doc.Amount,
                        IsAllocationDetail = true,
                        Balance = balance
                    });
                }
            }
        }

        return new ClientAccountStatementResult
        {
            Rows = rows,
            SoldeActuel = balance
        };
    }

    private string PaymentDesignation(ModePaiement mode) =>
        _locale.T(mode switch
        {
            ModePaiement.Virement => "ClientLedger_PayVirement",
            ModePaiement.Cheque => "ClientLedger_PayCheque",
            ModePaiement.Especes => "ClientLedger_PayEspeces",
            ModePaiement.TPE => "ClientLedger_PayTpe",
            ModePaiement.Effet => "ClientLedger_PayEffet",
            ModePaiement.Credit => "ClientLedger_PayCredit",
            _ => "ClientLedger_PayReceived"
        });
}
