namespace Postek.Caisse.Caisse;

/// <summary>Modes de règlement acceptés en caisse (multi-paiement d'un ticket).</summary>
public enum ModeReglement
{
    Especes,
    CarteBancaire,
    Cheque,
    TicketRestaurant,
    Avoir,
}

/// <summary>Un paiement saisi sur le ticket en cours.</summary>
public record Reglement(ModeReglement Mode, decimal Montant);

/// <summary>Libellés et codes stables des modes (les codes sont persistés en base).</summary>
public static class ModesReglement
{
    /// <summary>Code texte stable — jamais de nombre magique en base.</summary>
    public static string Code(ModeReglement mode) => mode switch
    {
        ModeReglement.Especes => "especes",
        ModeReglement.CarteBancaire => "cb",
        ModeReglement.Cheque => "cheque",
        ModeReglement.TicketRestaurant => "ticket_resto",
        ModeReglement.Avoir => "avoir",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    public static ModeReglement DepuisCode(string code) => code switch
    {
        "especes" => ModeReglement.Especes,
        "cb" => ModeReglement.CarteBancaire,
        "cheque" => ModeReglement.Cheque,
        "ticket_resto" => ModeReglement.TicketRestaurant,
        "avoir" => ModeReglement.Avoir,
        _ => throw new ArgumentException($"mode de règlement inconnu : {code}", nameof(code)),
    };

    /// <summary>Libellé affiché à l'écran.</summary>
    public static string Libelle(ModeReglement mode) => mode switch
    {
        ModeReglement.Especes => "Espèces",
        ModeReglement.CarteBancaire => "Carte bancaire",
        ModeReglement.Cheque => "Chèque",
        ModeReglement.TicketRestaurant => "Ticket restaurant",
        ModeReglement.Avoir => "Avoir",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };

    /// <summary>Libellé imprimé sur le ticket (ASCII pur — la page de codes
    /// du ticket n'a pas d'accents majuscules fiables).</summary>
    public static string LibelleTicket(ModeReglement mode) => mode switch
    {
        ModeReglement.Especes => "ESPECES",
        ModeReglement.CarteBancaire => "CB",
        ModeReglement.Cheque => "CHEQUE",
        ModeReglement.TicketRestaurant => "TICKET RESTO",
        ModeReglement.Avoir => "AVOIR",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };
}
