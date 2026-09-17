using System.Globalization;
using System.IO;
using System.Text;

namespace Postek.Caisse.Caisse;

/// <summary>Une ligne du récapitulatif TVA imprimé (par taux).</summary>
public record LigneRecapTvaImpression(decimal Taux, decimal Ht, decimal Tva)
{
    public decimal Ttc => decimal.Round(Ht + Tva, 3, MidpointRounding.AwayFromZero);
}

/// <summary>Données à imprimer sur un ticket de caisse 80 mm.</summary>
public sealed class DonneesTicket
{
    public string EnTete { get; init; } = "POSTEK";
    public string? SousTitre { get; init; }
    public string? Adresse { get; init; }
    /// <summary>Mentions obligatoires (MF, TVA, RC…) — cf. cahier des charges §3.2.</summary>
    public IReadOnlyList<string> MentionsLegales { get; init; } = Array.Empty<string>();
    public int NumeroTicket { get; init; }
    public DateTime DateHeure { get; init; } = DateTime.Now;
    public string Caisse { get; init; } = "1";
    public string Caissier { get; init; } = "1";

    public IReadOnlyList<LigneTicketImpression> Lignes { get; init; } =
        Array.Empty<LigneTicketImpression>();

    public IReadOnlyList<LigneRecapTvaImpression> RecapTva { get; init; } =
        Array.Empty<LigneRecapTvaImpression>();

    /// <summary>Règlements du ticket (multi-paiement) — imprimés en pied.</summary>
    public IReadOnlyList<LigneReglementImpression> Reglements { get; init; } =
        Array.Empty<LigneReglementImpression>();

    /// <summary>Monnaie rendue au client (espèces uniquement).</summary>
    public decimal MonnaieRendue { get; init; }

    public decimal TotalTtc { get; init; }
    public string? Pied { get; init; }
}

/// <summary>Règlement imprimé en pied de ticket.</summary>
public record LigneReglementImpression(string Libelle, decimal Montant);

/// <summary>Données à imprimer pour le rapport de clôture Z (80 mm).</summary>
public sealed class DonneesRapportZ
{
    public string EnTete { get; init; } = "POSTEK";
    public int Numero { get; init; }
    public DateTime DateHeure { get; init; } = DateTime.Now;
    public int NbTickets { get; init; }
    public decimal TotalTtc { get; init; }
    public decimal TotalHt { get; init; }
    public decimal TotalTva { get; init; }

    /// <summary>Récap TVA par taux (exigence fiscale NACEF).</summary>
    public IReadOnlyList<LigneRecapTvaImpression> RecapTva { get; init; } =
        Array.Empty<LigneRecapTvaImpression>();

    /// <summary>Totaux par mode de règlement de la période.</summary>
    public IReadOnlyList<LigneReglementImpression> Reglements { get; init; } =
        Array.Empty<LigneReglementImpression>();

    public string? Pied { get; init; }
}

/// <summary>Article imprimé sur le ticket.</summary>
public record LigneTicketImpression(
    string Designation,
    decimal Quantite,
    decimal PrixUnitaire,
    decimal TotalLigne);

/// <summary>
/// Générateur de flux ESC/POS pour ticket 80 mm (imprimantes thermiques compatibles).
/// S'adresse à l'imprimante en mode « texte brut » — aucune dépendance graphique.
///
/// Choix d'implémentation :
///  - page de code CP-863 (Canada-français) : dispose des accents français, et
///    contrairement à CP-850 elle est incluse dans .NET (table 863) — l'app peut
///    donc convertir sans table manuscrite. Si un caractère manque dans CP-863,
///    repli sur '?', jamais d'octet invalide envoyé à l'imprimante.
///  - montants formatés « 1 234,567 » (séparateur millier espace insécable inclus
///    dans CP-863, repli espace ordinaire).
/// </summary>
public static class TicketEscPos
{
    private const byte Esc = 0x1B;

    // Largeur 80 mm : police A = 48 colonnes (12 double-largeur, 24 quadruple).
    public const int ColonnesPoliceA = 48;

    // Commandes ESC/POS utilisées.
    private static readonly byte[] Init = { Esc, (byte)'@' };                       // réinitialise
    private static readonly byte[] Decoupe = { Esc, (byte)'d', 3 };                 // GS-free, coupe partielle
    private static readonly byte[] GrasOn = { Esc, (byte)'E', 1 };
    private static readonly byte[] GrasOff = { Esc, (byte)'E', 0 };
    private static readonly byte[] DoubleHauteur = { Esc, (byte)'!', 0x10 };        // Select print mode
    private static readonly byte[] DoubleLargeurHauteur = { Esc, (byte)'!', 0x30 };
    private static readonly byte[] PoliceNormale = { Esc, (byte)'!', 0x00 };

    public static byte[] Generer(DonneesTicket t) =>
        Generer(t, ColonnesPoliceA);

    public static byte[] Generer(DonneesTicket t, int colonnes)
    {
        var flux = new MemoryStream();
        using var w = new BinaryWriter(flux, Encoding.ASCII, leaveOpen: true);

        w.Write(Init);

        // ----- En-tête -----
        LigneCentree(w, t.EnTete, colonnes, DoubleLargeurHauteur);
        if (!string.IsNullOrWhiteSpace(t.SousTitre))
            LigneCentree(w, t.SousTitre!, colonnes, DoubleHauteur);
        if (!string.IsNullOrWhiteSpace(t.Adresse))
        {
            LigneCentree(w, t.Adresse!, colonnes, PoliceNormale);
        }
        LigneTirets(w, colonnes);

        // ----- Métadonnées -----
        LigneDeuxColonnes(w, $"Ticket n° {t.NumeroTicket}",
            t.DateHeure.ToString("dd/MM/yyyy HH:mm"), colonnes);
        LigneDeuxColonnes(w, $"Caisse {t.Caisse}  Caissier {t.Caissier}", "", colonnes);
        LigneTirets(w, colonnes);

        // ----- Articles -----
        foreach (var l in t.Lignes)
        {
            // Ligne 1 : désignation (éventuellement tronquée, repli '?').
            EcritTexte(w, Tronquer(l.Designation, colonnes));
            SautLigne(w);

            // Ligne 2 : "Q × PU" à gauche, total à droite.
            var gauche = $"{Formate(l.Quantite)} × {Formate(l.PrixUnitaire)}";
            LigneDeuxColonnes(w, gauche, Formate(l.TotalLigne), colonnes);
        }
        LigneTirets(w, colonnes);

        // ----- Récap TVA par taux (exigence fiscale) -----
        if (t.RecapTva.Count > 0)
        {
            foreach (var r in t.RecapTva)
            {
                LigneDeuxColonnes(w,
                    $"TVA {FormateTaux(r.Taux)}%  HT {Formate(r.Ht)}  TVA {Formate(r.Tva)}",
                    Formate(r.Ttc), colonnes);
            }
            LigneTirets(w, colonnes);
        }

        // ----- Règlements (multi-paiement) -----
        foreach (var r in t.Reglements)
        {
            LigneDeuxColonnes(w, r.Libelle, Formate(r.Montant), colonnes);
        }
        if (t.MonnaieRendue > 0m)
        {
            LigneDeuxColonnes(w, "RENDU", Formate(t.MonnaieRendue), colonnes);
        }
        LigneTirets(w, colonnes);

        // ----- Total -----
        w.Write(DoubleLargeurHauteur);
        LigneDeuxColonnes(w, "TOTAL", $"{Formate(t.TotalTtc)} TND", colonnes);
        w.Write(PoliceNormale);
        LigneTirets(w, colonnes);

        // ----- Mentions légales + pied -----
        foreach (var m in t.MentionsLegales)
            LigneCentree(w, m, colonnes, PoliceNormale);
        if (!string.IsNullOrWhiteSpace(t.Pied))
            LigneCentree(w, t.Pied!, colonnes, PoliceNormale);

        // Sauts + découpe partielle.
        w.Write(new byte[] { 0x0A, 0x0A, 0x0A });
        w.Write(Decoupe);

        w.Flush();
        return flux.ToArray();
    }

    // -----------------------------------------------------------------
    // Rapport de clôture Z : récap fiscal + règlements de la période.
    // Même gabarit 80 mm que le ticket — pas de détail article, uniquement
    // les totaux rattachés à la clôture (cf. fichier Dern_Z de l'existant).
    // -----------------------------------------------------------------
    public static byte[] GenererRapportZ(DonneesRapportZ z) =>
        GenererRapportZ(z, ColonnesPoliceA);

    public static byte[] GenererRapportZ(DonneesRapportZ z, int colonnes)
    {
        var flux = new MemoryStream();
        using var w = new BinaryWriter(flux, Encoding.ASCII, leaveOpen: true);

        w.Write(Init);

        // ----- En-tête -----
        LigneCentree(w, z.EnTete, colonnes, DoubleLargeurHauteur);
        LigneCentree(w, $"RAPPORT Z n° {z.Numero}", colonnes, DoubleHauteur);
        LigneCentree(w, z.DateHeure.ToString("dd/MM/yyyy HH:mm"), colonnes, PoliceNormale);
        LigneTirets(w, colonnes);

        // ----- Volumétrie -----
        LigneDeuxColonnes(w, "Tickets", Formate(z.NbTickets), colonnes);
        LigneTirets(w, colonnes);

        // ----- Récap TVA par taux (exigence fiscale) -----
        foreach (var r in z.RecapTva)
        {
            LigneDeuxColonnes(w,
                $"TVA {FormateTaux(r.Taux)}%  HT {Formate(r.Ht)}  TVA {Formate(r.Tva)}",
                Formate(r.Ttc), colonnes);
        }
        LigneTirets(w, colonnes);

        // ----- Règlements par mode -----
        foreach (var r in z.Reglements)
        {
            LigneDeuxColonnes(w, r.Libelle, Formate(r.Montant), colonnes);
        }
        LigneTirets(w, colonnes);

        // ----- Totaux -----
        w.Write(DoubleLargeurHauteur);
        LigneDeuxColonnes(w, "TOTAL TTC", $"{Formate(z.TotalTtc)} TND", colonnes);
        w.Write(PoliceNormale);
        LigneDeuxColonnes(w, "dont HT", Formate(z.TotalHt), colonnes);
        LigneDeuxColonnes(w, "dont TVA", Formate(z.TotalTva), colonnes);
        LigneTirets(w, colonnes);

        if (!string.IsNullOrWhiteSpace(z.Pied))
            LigneCentree(w, z.Pied!, colonnes, PoliceNormale);

        // Sauts + découpe partielle.
        w.Write(new byte[] { 0x0A, 0x0A, 0x0A });
        w.Write(Decoupe);

        w.Flush();
        return flux.ToArray();
    }

    // ------------------------------------------------------------------
    private static void LigneCentree(BinaryWriter w, string texte, int colonnes, byte[] mode)
    {
        w.Write(mode);
        // En mode double-largeur, chaque caractère occupe 2 colonnes.
        var facteur = mode == DoubleLargeurHauteur ? 2 : 1;
        var maxCaracteres = Math.Max(1, colonnes / facteur);
        texte = Tronquer(texte, maxCaracteres);
        var espaces = Math.Max(0, (colonnes - texte.Length * facteur) / 2);
        EcritTexte(w, new string(' ', espaces) + texte);
        SautLigne(w);
        w.Write(PoliceNormale);
    }

    private static void LigneDeuxColonnes(BinaryWriter w, string gauche, string droite, int colonnes)
    {
        gauche = Tronquer(gauche, colonnes);
        droite = Tronquer(droite, colonnes);
        var espaces = colonnes - longueurVisible(gauche) - longueurVisible(droite);
        if (espaces < 1)
        {
            // Ne tient pas : on tronque la partie gauche.
            gauche = Tronquer(gauche, colonnes - longueurVisible(droite) - 1);
            espaces = colonnes - longueurVisible(gauche) - longueurVisible(droite);
        }
        EcritTexte(w, gauche + new string(' ', Math.Max(1, espaces)) + droite);
        SautLigne(w);
    }

    private static void LigneTirets(BinaryWriter w, int colonnes)
    {
        EcritTexte(w, new string('-', colonnes));
        SautLigne(w);
    }

    private static void SautLigne(BinaryWriter w) => w.Write((byte)0x0A);

    /// <summary>
    /// Écart .NET ↔ imprimantes ESC/POS : le ° (U+00B0) encode 0xF8 côté .NET
    /// en CP-863, mais 0xF8 affiche Ø sur l'imprimante ; le ° ESC/POS réel est
    /// 0xEF. Mappage manuel avant encodage CP-863 pour ces caractères.
    /// </summary>
    private static readonly Dictionary<char, byte> RemplacementsEscPos = new()
    {
        ['°'] = 0xEF,   // ° ESC/POS (0xF8 = Ø sur l'imprimante)
        ['×'] = 0xF6,   // × présent en CP-863 imprimante (0xD7 côté .NET)
        ['é'] = 0x82,   // é CP850/863 imprimante (0x82 = é, pas de 0x82 .NET)
        ['è'] = 0x8A,   // è CP850/863 imprimante
    };

    private static void EcritTexte(BinaryWriter w, string texte)
    {
        // CP-863 : accents français ; le replacement fallback produit '?'
        // plutôt qu'un octet invalide pour un caractère non mappé.
        // Caractères du dictionnaire mappés manuellement d'abord.
        var octets = new List<byte>(texte.Length);
        foreach (var c in texte)
        {
            if (RemplacementsEscPos.TryGetValue(c, out var special))
                octets.Add(special);
            else
                octets.AddRange(Encoding.GetEncoding(863).GetBytes(c.ToString()));
        }
        w.Write(octets.ToArray());
    }

    private static string Tronquer(string texte, int max)
    {
        if (texte.Length <= max) return texte;
        return texte[..max];
    }

    private static int longueurVisible(string s) => s.Length;

    private static string Formate(decimal montant) =>
        montant.ToString("#,##0.000", CultureInfo.GetCultureInfo("fr-FR"));

    private static string FormateTaux(decimal taux) =>
        taux == decimal.Floor(taux) ? taux.ToString("0", CultureInfo.InvariantCulture)
                                    : taux.ToString("0.##", CultureInfo.InvariantCulture);
}
