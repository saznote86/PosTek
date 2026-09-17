using System.Collections.ObjectModel;

namespace Postek.Caisse.Caisse;

/// <summary>Article affichable sur un bouton de clavier virtuel.</summary>
public record Article(string Code, string Designation, decimal PrixTtc, decimal TauxTva);

/// <summary>Ligne du ticket en cours.</summary>
public record LigneTicket(Article Article, decimal Quantite, decimal RemisePct)
{
    public decimal TotalLigne =>
        decimal.Round(Quantite * Article.PrixTtc * (1 - RemisePct / 100m), 3,
                      MidpointRounding.AwayFromZero);

    /// <summary>TVA contenue dans le TTC de la ligne : TTC × t/(1+t) — cf. fiscal/comptabilite.py.</summary>
    public decimal TvaLigne =>
        decimal.Round(TotalLigne * Article.TauxTva / (1 + Article.TauxTva), 3,
                      MidpointRounding.AwayFromZero);
}

/// <summary>
/// Cœur métier de la caisse : le ticket en cours.
/// Montants en decimal (millimes) — jamais de double, conformément aux
/// conventions POSTEK (cf. docs/CAHIER_DES_CHARGES.md §4.2).
/// </summary>
public sealed class CaisseService
{
    private readonly ObservableCollection<LigneTicket> _lignes = new();
    public ReadOnlyObservableCollection<LigneTicket> Lignes { get; }

    public string? CodeClient { get; set; }
    public int NumeroTicket { get; private set; } = 1;

    public decimal TotalTtc => _lignes.Sum(l => l.TotalLigne);
    public decimal TotalTva => _lignes.Sum(l => l.TvaLigne);
    public decimal TotalHt => TotalTtc - TotalTva;
    public int NbArticles => (int)_lignes.Sum(l => l.Quantite);

    public event Action? TicketModifie;

    /// <summary>Règlements saisis sur le ticket en cours (multi-paiement).</summary>
    private readonly List<Reglement> _reglements = new();
    public IReadOnlyList<Reglement> Reglements => _reglements;

    /// <summary>Somme déjà réglée sur le ticket en cours.</summary>
    public decimal TotalRegle => _reglements.Sum(r => r.Montant);

    /// <summary>Reste à régler (peut être négatif = trop perçu / rendu monnaie).</summary>
    public decimal ResteAPayer => TotalTtc - TotalRegle;

    /// <summary>
    /// Rendu monnaie : uniquement sur les espèces, plafonné au trop perçu.
    /// CB/chèque/TR ne rendent jamais de monnaie.</summary>
    public decimal MonnaieARendre
    {
        get
        {
            var tropPercu = TotalRegle - TotalTtc;
            if (tropPercu <= 0m) return 0m;
            var especes = _reglements.Where(r => r.Mode == ModeReglement.Especes).Sum(r => r.Montant);
            return Math.Min(especes, tropPercu);
        }
    }

    /// <summary>Ticket soldé : tout est réglé, trop perçu éventuel couvert par espèces.</summary>
    public bool EstSolde => ResteAPayer <= 0m;

    public CaisseService()
    {
        Lignes = new(_lignes);
    }

    /// <summary>Initialise le compteur depuis la base (numérotation continue).</summary>
    public void DefinirNumeroTicket(int numero) => NumeroTicket = numero;

    public void Ajouter(Article article, decimal quantite = 1m, decimal remisePct = 0m)
    {
        var existante = _lignes.FirstOrDefault(l =>
            l.Article.Code == article.Code && l.RemisePct == remisePct);
        if (existante is not null)
        {
            var i = _lignes.IndexOf(existante);
            _lignes[i] = existante with { Quantite = existante.Quantite + quantite };
        }
        else
        {
            _lignes.Add(new LigneTicket(article, quantite, remisePct));
        }
        TicketModifie?.Invoke();
    }

    public void Supprimer(LigneTicket ligne)
    {
        _lignes.Remove(ligne);
        TicketModifie?.Invoke();
    }

    public void Vider()
    {
        _lignes.Clear();
        _reglements.Clear();
        TicketModifie?.Invoke();
    }

    /// <summary>Ajoute (ou fusionne) un règlement sur le ticket en cours.</summary>
    public void Regler(ModeReglement mode, decimal montant)
    {
        if (montant <= 0m)
            throw new ArgumentOutOfRangeException(nameof(montant), montant,
                "le montant d'un règlement doit être strictement positif");
        if (_lignes.Count == 0)
            throw new InvalidOperationException("aucun article : rien à régler");

        var existant = _reglements.LastOrDefault(r => r.Mode == mode);
        if (existant is not null)
        {
            _reglements.Remove(existant);
            _reglements.Add(existant with { Montant = existant.Montant + montant });
        }
        else
        {
            _reglements.Add(new Reglement(mode, montant));
        }
        TicketModifie?.Invoke();
    }

    /// <summary>Retire un règlement (correction avant validation).</summary>
    public void SupprimerReglement(Reglement reglement)
    {
        _reglements.Remove(reglement);
        TicketModifie?.Invoke();
    }

    /// <summary>
    /// Valide le ticket : renvoie le total encaissé et prépare le suivant.
    /// Sans règlement saisi, le ticket est soldé d'un coup (espèces implicites).
    /// Avec des règlements partiels, il doit être soldé avant l'appel.
    /// </summary>
    public decimal Encaisser()
    {
        if (_lignes.Count == 0) return 0m;
        if (_reglements.Count > 0 && ResteAPayer > 0m)
            throw new InvalidOperationException(
                $"ticket non soldé : reste à payer {ResteAPayer:0.000}");

        var total = TotalTtc;
        var reglements = _reglements.ToList();   // copie : Vidage dans Encaisser
        _lignes.Clear();
        _reglements.Clear();
        NumeroTicket++;
        TicketModifie?.Invoke();
        DerniersReglements = reglements;
        return total;
    }

    /// <summary>Règlements du dernier ticket encaissé (persistance avant vidage).</summary>
    public IReadOnlyList<Reglement> DerniersReglements { get; private set; } =
        Array.Empty<Reglement>();
}
