namespace GestionCommerciale.Modules.Charges.ViewModels;

public sealed class ImportCalculPickItem
{
    public int Id { get; init; }
    public string Libelle { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public int LineCount { get; init; }
    public string Display => $"{Libelle}  —  {Date:dd/MM/yyyy}  ({LineCount})";
}
