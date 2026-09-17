namespace Postek.Caisse.Caisse;

/// <summary>Une ligne du récapitulatif TVA d'une clôture Z (un taux).</summary>
public record LigneZTva(decimal TauxTva, decimal TotalHt, decimal TotalTva)
{
    public decimal TotalTtc => decimal.Round(TotalHt + TotalTva, 3, MidpointRounding.AwayFromZero);
}

/// <summary>Totaux d'un mode de règlement sur la période (récap Z).</summary>
public record LigneZMode(ModeReglement Mode, decimal Total);

/// <summary>
/// Récapitulatif d'une clôture Z — équivalent POSTEK des fichiers `numeroz`/`Dern_Z` de l'existant.
/// Les ventes de la période sont rattachées à la clôture via z_numero sur ticket.
/// Montants en décimal (millimes) — conventions POSTEK §4.2.
/// </summary>
public record RecapZ(
    int Numero,
    string DateHeure,
    int NbTickets,
    decimal TotalTtc,
    decimal TotalHt,
    decimal TotalTva,
    IReadOnlyList<LigneZTva> LignesTva,
    IReadOnlyList<LigneZMode> ReglementsParMode)
{
    /// <summary>Somme des règlements de la période — doit égaler TotalTtc.</summary>
    public decimal TotalRegle => ReglementsParMode.Sum(m => m.Total);
}
