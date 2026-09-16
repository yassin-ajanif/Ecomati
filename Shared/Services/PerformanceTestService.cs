using System.Diagnostics;
using System.Globalization;
using GestionCommerciale.Shared.Database;
using Microsoft.Data.Sqlite;

namespace GestionCommerciale.Shared.Services;

public class PerformanceTestService
{
    private const int ProductCount = 10_000;
    private const int ClientCount = 1_000;
    private const int DocumentCount = 50_000;
    private const int DocumentsPerDay = 10;
    private const int InitialProductStock = 10_000;
    private const string FactureOrigineType = "Facture";

    private static readonly Random Rng = Random.Shared;
    private readonly string _cs;

    public PerformanceTestService()
    {
        _cs = DatabasePath.GetConnectionString();
    }

    public async Task<string> RunAsync(IProgress<string> progress, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        await using var conn = new SqliteConnection(_cs);
        await conn.OpenAsync(ct);

        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA synchronous=OFF; PRAGMA journal_mode=WAL; PRAGMA cache_size=-500000; PRAGMA temp_store=MEMORY;";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        var max = await GetMaxIdsAsync(conn, ct);
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var startDate = DateTime.Today;
        var dayCount = DocumentCount / DocumentsPerDay;

        progress.Report($"Création de {ProductCount:N0} produits...");
        await InsertProductsAsync(conn, max.ProdId, now, ct);

        progress.Report($"Création de {ClientCount:N0} clients...");
        await InsertClientsAsync(conn, max.TiersId, now, ct);

        progress.Report($"Création de {DocumentCount:N0} factures (~{DocumentsPerDay}/jour sur {dayCount:N0} jours)...");
        var factureMeta = await InsertFactureHeadersAsync(conn, max, now, startDate, ct);

        progress.Report("Création des lignes de factures...");
        var factureLines = await InsertFactureLinesAsync(conn, max, factureMeta, ct);

        progress.Report("Mise à jour des totaux TTC des factures...");
        await UpdateFactureTotalTtcAsync(conn, factureMeta, ct);

        progress.Report("Création des paiements clients (soldes)...");
        var paiementCount = await InsertPaiementsAsync(conn, max.PaiementId, factureMeta, now, ct);

        progress.Report("Création des mouvements de stock (sorties factures)...");
        var mvtCount = await InsertStockMovementsAsync(conn, max.MouvementId, max.ProdId, factureLines, now, ct);

        sw.Stop();
        var e = sw.Elapsed;
        return $"Terminé en {e.Hours}h {e.Minutes}m {e.Seconds}s ({e.TotalSeconds:F1}s) — {ProductCount:N0} produits, {DocumentCount:N0} factures, {paiementCount:N0} paiements, {mvtCount:N0} mouvements stock sur {dayCount:N0} jours (~{dayCount / 365.25:F1} ans à {DocumentsPerDay}/jour).";
    }

    private static async Task<(long ProdId, long TiersId, long FactId, long FactLigneId, long PaiementId, long MouvementId)>
        GetMaxIdsAsync(SqliteConnection conn, CancellationToken ct)
    {
        async Task<long> Max(string table)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT IFNULL(MAX(Id),0) FROM \"{table}\"";
            var r = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt64(r);
        }

        return (
            await Max("Produits"),
            await Max("Tiers"),
            await Max("Factures"),
            await Max("FactureLignes"),
            await Max("Paiements"),
            await Max("MouvementsStock")
        );
    }

    private static string DateForDocumentIndex(DateTime startDate, int docIndex)
    {
        var dayOffset = docIndex / DocumentsPerDay;
        return startDate.AddDays(dayOffset).ToString("yyyy-MM-dd");
    }

    private static decimal ComputeTtc(decimal ht) => ht;

    private static async Task InsertProductsAsync(SqliteConnection conn, long startId, string now, CancellationToken ct)
    {
        const int batch = 500;
        var designs = new[] { "Ordinateur portable", "Souris sans fil", "Clavier mécanique", "Écran 24\"", "Disque dur SSD", "Carte mémoire", "Imprimante", "Scanner", "Webcam HD", "Casque audio", "Enceinte Bluetooth", "Hub USB", "Câble HDMI", "Adaptateur secteur", "Batterie externe", "Sacoche ordinateur", "Tapis de souris", "Support téléphone", "Ventilateur USB", "Lampe LED" };

        for (var i = 0; i < ProductCount; i += batch)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("INSERT INTO Produits (Id,CreatedAt,UpdatedAt,Reference,CodeBarre,Designation,Unite,PrixAchatHT,PrixVenteHT,StockActuel,StockMinimum,Actif) VALUES ");
            var end = Math.Min(i + batch, ProductCount);
            for (var j = i; j < end; j++)
            {
                var id = startId + 1 + j;
                var desig = $"{designs[j % designs.Length]} #{j}";
                var pa = Rng.Next(500, 500_000) / 100m;
                var pv = pa + Rng.Next(200, 300_000) / 100m;
                if (j > i) sb.Append(',');
                sb.Append(CultureInfo.InvariantCulture, $"({id},'{now}','{now}','PROD-{j:D5}',NULL,'{Escape(desig)}','U',{pa:F2},{pv:F2},{InitialProductStock},{Rng.Next(0,51)},1)");
            }
            await ExecAsync(conn, sb.ToString(), ct);
        }
    }

    private static async Task InsertClientsAsync(SqliteConnection conn, long startId, string now, CancellationToken ct)
    {
        const int batch = 500;
        for (var i = 0; i < ClientCount; i += batch)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("INSERT INTO Tiers (Id,CreatedAt,UpdatedAt,Type,Nom,ICE,Adresse,Ville,Telephone,Email,ConditionsPaiement,Actif) VALUES ");
            var end = Math.Min(i + batch, ClientCount);
            for (var j = i; j < end; j++)
            {
                var id = startId + 1 + j;
                if (j > i) sb.Append(',');
                sb.Append(CultureInfo.InvariantCulture, $"({id},'{now}','{now}',0,'Client test {j}','','Casablanca','Casablanca','','','',1)");
            }
            await ExecAsync(conn, sb.ToString(), ct);
        }
    }

    private sealed class FactureMeta
    {
        public required long Id { get; init; }
        public required DateTime Date { get; init; }
        public required bool EstPayee { get; init; }
        public decimal TotalHt { get; set; }
        public decimal TotalTtc { get; set; }
    }

    private static async Task<FactureMeta[]> InsertFactureHeadersAsync(SqliteConnection conn,
        (long ProdId, long TiersId, long FactId, long FactLigneId, long PaiementId, long MouvementId) max,
        string now, DateTime startDate, CancellationToken ct)
    {
        const int batch = 500;
        var startFact = max.FactId + 1;
        var clientStart = max.TiersId + 1;
        var meta = new FactureMeta[DocumentCount];

        for (var i = 0; i < DocumentCount; i += batch)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("INSERT INTO Factures (Id,CreatedAt,UpdatedAt,Numero,ClientId,Date,TotalTtc) VALUES ");
            var end = Math.Min(i + batch, DocumentCount);
            for (var j = i; j < end; j++)
            {
                var id = startFact + j;
                var clientId = clientStart + Rng.Next(0, ClientCount);
                var date = DateTime.Parse(DateForDocumentIndex(startDate, j));
                var estPayee = Rng.NextDouble() < 0.5;
                var year = date.Year;
                meta[j] = new FactureMeta
                {
                    Id = id,
                    Date = date,
                    EstPayee = estPayee
                };
                if (j > i) sb.Append(',');
                sb.Append(CultureInfo.InvariantCulture, $"({id},'{now}','{now}','FAC-{year}-{j:D6}',{clientId},'{date:yyyy-MM-dd}',0)");
            }
            await ExecAsync(conn, sb.ToString(), ct);
        }

        return meta;
    }

    private static async Task<List<(long FactureId, long ProdId, decimal Qty)>> InsertFactureLinesAsync(SqliteConnection conn,
        (long ProdId, long TiersId, long FactId, long FactLigneId, long PaiementId, long MouvementId) max,
        FactureMeta[] factureMeta, CancellationToken ct)
    {
        const int batch = 1000;
        var startFact = max.FactId + 1;
        var startLigne = max.FactLigneId + 1;
        var prodStart = max.ProdId + 1;
        var ligneIdx = 0;
        System.Text.StringBuilder? sb = null;
        var seeds = new List<(long FactureId, long ProdId, decimal Qty)>(DocumentCount * 3);

        for (var i = 0; i < DocumentCount; i++)
        {
            var factId = startFact + i;
            var meta = factureMeta[i];
            var linesPerFact = Rng.Next(1, 6);
            for (var li = 0; li < linesPerFact; li++)
            {
                if (ligneIdx % batch == 0)
                {
                    if (sb != null) await ExecAsync(conn, sb.ToString(), ct);
                    sb = new System.Text.StringBuilder();
                    sb.Append("INSERT INTO FactureLignes (Id,CreatedAt,UpdatedAt,FactureId,ProduitId,Designation,Quantite,PrixUnitaireHT,Conditionnement) VALUES ");
                }

                var id = startLigne + ligneIdx;
                var prodId = prodStart + Rng.Next(0, ProductCount);
                var qty = Rng.Next(1, 11);
                var pu = Rng.Next(1000, 500_000) / 100m;
                var desig = $"Produit {prodId}";
                var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                var lht = qty * pu;
                meta.TotalHt += lht;

                if (ligneIdx % batch > 0) sb!.Append(',');
                sb!.Append(CultureInfo.InvariantCulture, $"({id},'{now}','{now}',{factId},{prodId},'{Escape(desig)}',{qty},{pu:F2},'U')");
                seeds.Add((factId, prodId, qty));
                ligneIdx++;
            }
        }

        if (sb != null) await ExecAsync(conn, sb.ToString(), ct);
        return seeds;
    }

    private static async Task UpdateFactureTotalTtcAsync(SqliteConnection conn, FactureMeta[] factureMeta, CancellationToken ct)
    {
        const int batch = 500;
        for (var i = 0; i < factureMeta.Length; i++)
            factureMeta[i].TotalTtc = ComputeTtc(factureMeta[i].TotalHt);

        for (var i = 0; i < factureMeta.Length; i += batch)
        {
            var end = Math.Min(i + batch, factureMeta.Length);
            var sb = new System.Text.StringBuilder();
            for (var j = i; j < end; j++)
            {
                var f = factureMeta[j];
                if (j > i) sb.Append(';');
                sb.Append(CultureInfo.InvariantCulture, $"UPDATE Factures SET TotalTtc={f.TotalTtc:F2} WHERE Id={f.Id}");
            }
            await ExecAsync(conn, sb.ToString(), ct);
        }
    }

    private static async Task<int> InsertPaiementsAsync(SqliteConnection conn, long startPaiementId, FactureMeta[] factureMeta, string now, CancellationToken ct)
    {
        const int batch = 500;
        var paiementId = startPaiementId;
        var count = 0;
        System.Text.StringBuilder? sb = null;
        var batchCount = 0;

        foreach (var f in factureMeta)
        {
            if (f.TotalTtc <= 0) continue;

            decimal montant;
            DateTime date;
            if (f.EstPayee)
            {
                montant = f.TotalTtc;
                date = f.Date.AddDays(Rng.Next(0, 31));
            }
            else if (Rng.NextDouble() < 0.45)
            {
                montant = Math.Round(f.TotalTtc * Rng.Next(20, 81) / 100m, 2);
                date = f.Date.AddDays(Rng.Next(5, 91));
            }
            else continue;

            if (montant <= 0) continue;

            if (batchCount % batch == 0)
            {
                if (sb != null)
                {
                    await ExecAsync(conn, sb.ToString(), ct);
                    sb = null;
                }
                sb = new System.Text.StringBuilder();
                sb.Append("INSERT INTO Paiements (Id,CreatedAt,UpdatedAt,FactureId,Montant,Date,Mode,Reference) VALUES ");
            }
            else
            {
                sb!.Append(',');
            }

            paiementId++;
            var mode = Rng.Next(0, 6);
            var reference = $"REF-{paiementId:D7}";
            sb!.Append(CultureInfo.InvariantCulture, $"({paiementId},'{now}','{now}',{f.Id},{montant:F2},'{date:yyyy-MM-dd}',{mode},'{reference}')");
            count++;
            batchCount++;
        }

        if (sb != null) await ExecAsync(conn, sb.ToString(), ct);
        return count;
    }

    private static async Task<int> InsertStockMovementsAsync(
        SqliteConnection conn,
        long startMouvementId,
        long prodStartId,
        List<(long FactureId, long ProdId, decimal Qty)> factureLines,
        string now,
        CancellationToken ct)
    {
        const int batch = 1000;
        const int sortieType = 1;
        var stockByProd = new Dictionary<long, decimal>(ProductCount);
        for (var p = 1; p <= ProductCount; p++)
            stockByProd[prodStartId + p] = InitialProductStock;

        var mouvementId = startMouvementId;
        var count = 0;
        System.Text.StringBuilder? sb = null;

        foreach (var factureGroup in factureLines.GroupBy(l => l.FactureId).OrderBy(g => g.Key))
        {
            var factureId = factureGroup.Key;
            foreach (var prodGroup in factureGroup.GroupBy(l => l.ProdId))
            {
                var prodId = prodGroup.Key;
                var qty = prodGroup.Sum(l => l.Qty);
                stockByProd.TryGetValue(prodId, out var stockAvant);

                if (count % batch == 0)
                {
                    if (sb != null) await ExecAsync(conn, sb.ToString(), ct);
                    sb = new System.Text.StringBuilder();
                    sb.Append("INSERT INTO MouvementsStock (Id,CreatedAt,UpdatedAt,ProduitId,Type,StockAvant,Quantite,OrigineType,OrigineId,Note) VALUES ");
                }
                else
                {
                    sb!.Append(',');
                }

                mouvementId++;
                var note = $"FAC-{factureId}";
                sb!.Append(CultureInfo.InvariantCulture, $"({mouvementId},'{now}','{now}',{prodId},{sortieType},{stockAvant:F2},{qty:F2},'{FactureOrigineType}',{factureId},'{Escape(note)}')");
                stockByProd[prodId] = stockAvant - qty;
                count++;
            }
        }

        if (sb != null) await ExecAsync(conn, sb.ToString(), ct);

        const int updateBatch = 500;
        var prodIds = stockByProd.Keys.OrderBy(k => k).ToList();
        for (var i = 0; i < prodIds.Count; i += updateBatch)
        {
            var end = Math.Min(i + updateBatch, prodIds.Count);
            var sbUpdate = new System.Text.StringBuilder();
            for (var j = i; j < end; j++)
            {
                var prodId = prodIds[j];
                if (j > i) sbUpdate.Append(';');
                sbUpdate.Append(CultureInfo.InvariantCulture, $"UPDATE Produits SET StockActuel={stockByProd[prodId]:F2} WHERE Id={prodId}");
            }
            await ExecAsync(conn, sbUpdate.ToString(), ct);
        }

        return count;
    }

    private static async Task ExecAsync(SqliteConnection conn, string sql, CancellationToken ct)
    {
        await using var tx = conn.BeginTransaction();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Transaction = tx;
        await cmd.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static string Escape(string s) => s.Replace("'", "''");
}
