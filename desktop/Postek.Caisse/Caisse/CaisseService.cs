using System.Collections.ObjectModel;

namespace Postec.Caisse.Caisse;

/// <summary>Article affichable sur un bouton de clavier virtuel.</summary>
public record Article(string Code, string Designation, decimal PrixTtc, decimal TauxTva)
{
    public string? FamilleCode { get; init; }
    public int? ImprimanteId { get; init; }
    public string? NomImprimante { get; init; }
    public string? CheminImage { get; init; }
    public string Emoji { get; init; } = "📦";
    public decimal PrixVente => PrixTtc;
}

/// <summary>Ligne du ticket en cours.</summary>
public record LigneTicket(Article Article, decimal Quantite, decimal RemisePct)
{
    public decimal TotalLigne =>
        decimal.Round(Quantite * Article.PrixTtc * (1 - RemisePct / 100m), 3,
                      MidpointRounding.AwayFromZero);

    public decimal TvaLigne =>
        decimal.Round(TotalLigne * Article.TauxTva / (1 + Article.TauxTva), 3,
                      MidpointRounding.AwayFromZero);
}

public sealed class CaisseService
{
    private readonly ObservableCollection<LigneTicket> _lignes = new();
    public ReadOnlyObservableCollection<LigneTicket> Lignes { get; }

    public string? CodeClient { get; set; }
    public int NumeroTicket { get; private set; } = 1;

    public decimal TotalTtc => _lignes.Sum(l => l.TotalLigne);
    public decimal TotalTva => _lignes.Sum(l => l.TvaLigne);
    public decimal TotalHt  => TotalTtc - TotalTva;
    public int     NbArticles => (int)_lignes.Sum(l => l.Quantite);

    public event Action? TicketModifie;

    private readonly List<Reglement> _reglements = new();
    public IReadOnlyList<Reglement> Reglements => _reglements;

    public decimal TotalRegle => _reglements.Sum(r => r.Montant);
    public decimal ResteAPayer => TotalTtc - TotalRegle;

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

    public bool EstSolde => ResteAPayer <= 0m;

    public CaisseService() { Lignes = new(_lignes); }

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

    public void ModifierQuantite(LigneTicket ligne, decimal nouvelleQuantite)
    {
        var i = _lignes.IndexOf(ligne);
        if (i < 0) return;
        if (nouvelleQuantite <= 0m)
            _lignes.RemoveAt(i);
        else
            _lignes[i] = ligne with { Quantite = decimal.Round(nouvelleQuantite, 3, MidpointRounding.AwayFromZero) };
        TicketModifie?.Invoke();
    }

    public void AppliquerRemise(LigneTicket ligne, decimal remisePct)
    {
        if (remisePct < 0m || remisePct > 100m)
            throw new ArgumentOutOfRangeException(nameof(remisePct));
        var i = _lignes.IndexOf(ligne);
        if (i < 0) return;
        _lignes[i] = ligne with { RemisePct = remisePct };
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

    public void Regler(ModeReglement mode, decimal montant)
    {
        if (montant <= 0m)
            throw new ArgumentOutOfRangeException(nameof(montant), montant,
                "le montant d un reglement doit etre strictement positif");
        if (_lignes.Count == 0)
            throw new InvalidOperationException("aucun article : rien a regler");

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

    public void SupprimerReglement(Reglement reglement)
    {
        _reglements.Remove(reglement);
        TicketModifie?.Invoke();
    }

    public decimal Encaisser()
    {
        if (_lignes.Count == 0) return 0m;
        if (_reglements.Count > 0 && ResteAPayer > 0m)
            throw new InvalidOperationException(
                string.Format("ticket non solde : reste a payer {0:0.000}", ResteAPayer));

        var total = TotalTtc;
        var reglements = _reglements.ToList();
        _lignes.Clear();
        _reglements.Clear();
        NumeroTicket++;
        TicketModifie?.Invoke();
        DerniersReglements = reglements;
        return total;
    }

    public IReadOnlyList<Reglement> DerniersReglements { get; private set; } =
        Array.Empty<Reglement>();

    public static IReadOnlyDictionary<int, IReadOnlyList<LigneTicket>> GrouperParImprimante(
        IEnumerable<LigneTicket> lignes) =>
        lignes.GroupBy(l => l.Article.ImprimanteId)
              // SQLite/les imprimantes métier sont identifiées par un entier ; 0
              // représente explicitement le groupe « aucune imprimante ».
              .ToDictionary(g => g.Key ?? 0, g => (IReadOnlyList<LigneTicket>)g.ToList());
}
