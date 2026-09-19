namespace Postec.Caisse.Caisse;

/// <summary>Une ligne du récapitulatif TVA d'une clôture Z (un taux).</summary>
public record LigneZTva(decimal TauxTva, decimal TotalHt, decimal TotalTva)
{
    public decimal TotalTtc => decimal.Round(TotalHt + TotalTva, 3, MidpointRounding.AwayFromZero);
}

/// <summary>Totaux d'un mode de règlement sur la période (récap Z).</summary>
public record LigneZMode(ModeReglement Mode, decimal Total);

/// <summary>
/// Récapitulatif d'une clôture Z — équivalent POSTEC des fichiers `numeroz`/`Dern_Z` de l'existant.
/// Les ventes de la période sont rattachées à la clôture via z_numero sur ticket.
/// Montants en décimal (millimes) — conventions POSTEC §4.2.
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
    /// <summary>
    /// Somme des règlements de la période, montants REÇUS (les espèces incluent
    /// le trop perçu rendu au client) : égale TotalTtc + rendu monnaie rendu.
    /// </summary>
    public decimal TotalRegle => ReglementsParMode.Sum(m => m.Total);

    /// <summary>
    /// Rendu monnaie de la période : trop perçu total = montants reçus − ventes.
    /// Le parcours d'encaissement plafonne le rendu aux espèces reçues et les
    /// autres modes au reste à payer, donc le trop perçu EST le rendu.
    /// Dérivé, jamais stocké — cohérent par construction avec TotalRegle.
    /// </summary>
    public decimal MonnaieRendue =>
        decimal.Round(TotalRegle - TotalTtc, 3, MidpointRounding.AwayFromZero) > 0m
            ? TotalRegle - TotalTtc : 0m;
}
