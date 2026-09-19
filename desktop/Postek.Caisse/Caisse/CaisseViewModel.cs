using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace Postec.Caisse.Caisse;

/// <summary>Projection observable du ticket pour les vues WPF.</summary>
public sealed class CaisseViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly CaisseService _caisse;
    private readonly List<Article> _tousLesProduits = new();
    private readonly ObservableCollection<Article> _produitsFiltres = new();
    private readonly ObservableCollection<FamilleArticle> _familles = new();
    private Timer? _rechercheTimer;
    private string _rechercheText = "";
    private FamilleArticle? _familleSelectionnee;
    private readonly Dispatcher _dispatcher;

    public CaisseViewModel(CaisseService caisse)
    {
        _caisse = caisse;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _caisse.TicketModifie += SurTicketModifie;
        Services.DatabaseMaintenanceService.TablesVidees += SurTablesVidees;
        PayerCommand = new RelayCommand(() => PayerDemande?.Invoke(), () => NbArticles > 0);
        AnnulerCommand = new RelayCommand(() => AnnulationDemandee?.Invoke(), () => NbArticles > 0);
        MiseEnAttenteCommand = new RelayCommand(() => MiseEnAttenteDemandee?.Invoke(), () => NbArticles > 0);
        RechercheClientCommand = new RelayCommand(() => RechercheClientDemandee?.Invoke());
        AjouterArticleCommand = new RelayCommandParametre(param =>
        {
            if (param is Article article) ArticleDemandee?.Invoke(article);
        });
        FiltrerParFamilleCommand = new RelayCommandParametre(param =>
        {
            FamilleSelectionnee = param as FamilleArticle;
            FiltrerProduits();
            ProduitsRecharges?.Invoke();
        });
        SupprimerLigneCommand = new RelayCommandParametre(param =>
        {
            if (param is LigneTicket ligne) _caisse.Supprimer(ligne);
        });
        ModifierQuantiteCommand = new RelayCommandParametre(param =>
        {
            if (param is LigneTicket ligne) _caisse.ModifierQuantite(ligne, ligne.Quantite + 1m);
        });
    }

    public IReadOnlyList<LigneTicket> Lignes => _caisse.Lignes;
    public ObservableCollection<Article> ProduitsFiltres => _produitsFiltres;
    public ObservableCollection<FamilleArticle> Familles => _familles;
    public FamilleArticle? FamilleSelectionnee
    {
        get => _familleSelectionnee;
        set { if (Equals(_familleSelectionnee, value)) return; _familleSelectionnee = value; OnPropertyChanged(); }
    }
    public string RechercheText
    {
        get => _rechercheText;
        set
        {
            if (_rechercheText == value) return;
            _rechercheText = value;
            OnPropertyChanged();
            _rechercheTimer?.Dispose();
            _rechercheTimer = new Timer(_ =>
            {
                var dispatcher = App.Current?.Dispatcher;
                if (dispatcher is null) return;
                dispatcher.Invoke(() =>
                {
                    FiltrerProduits();
                    ProduitsRecharges?.Invoke();
                });
            }, null, 300, Timeout.Infinite);
        }
    }
    public int NumeroTicket => _caisse.NumeroTicket;
    public decimal SousTotalHt => _caisse.TotalHt;
    public decimal TotalTva => _caisse.TotalTva;
    public decimal TotalTtc => _caisse.TotalTtc;
    public decimal TimbreFiscal => TotalTtc > 0m ? 1.000m : 0m;
    public decimal SousTotalHT => SousTotalHt;
    public decimal TotalTTC => decimal.Round(SousTotalHt + TotalTva + TimbreFiscal, 3, MidpointRounding.AwayFromZero);
    public int NbArticles => _caisse.NbArticles;
    public ICommand PayerCommand { get; }
    public ICommand AnnulerCommand { get; }
    public ICommand MiseEnAttenteCommand { get; }
    public ICommand RechercheClientCommand { get; }
    public ICommand AjouterArticleCommand { get; }
    public ICommand FiltrerParFamilleCommand { get; }
    public ICommand SupprimerLigneCommand { get; }
    public ICommand ModifierQuantiteCommand { get; }

    public event Action? PayerDemande;
    public event Action? AnnulationDemandee;
    public event Action? MiseEnAttenteDemandee;
    public event Action? RechercheClientDemandee;
    public event Action<Article>? ArticleDemandee;
    public event Action? ProduitsRecharges;
    public string? CodeClient
    {
        get => _caisse.CodeClient;
        set
        {
            if (_caisse.CodeClient == value) return;
            _caisse.CodeClient = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Invalide le catalogue affiché par la caisse après une maintenance SQLite.
    /// La vue propriétaire reconstruit alors ses boutons depuis la base.
    /// </summary>
    public void RechargerProduits() => ProduitsRecharges?.Invoke();

    public void ChargerProduits(BaseDonnees bdd)
    {
        _familles.Clear();
        _familles.Add(new FamilleArticle("", "Tous", 0, "#3B82F6"));
        foreach (var famille in bdd.ChargerFamilles()) _familles.Add(famille);
        _familleSelectionnee = null;
        _produitsFiltres.Clear();
        _tousLesProduits.Clear();
        foreach (var article in bdd.ListerArticles()) _tousLesProduits.Add(article with { Emoji = ObtenirEmoji(article) });
        foreach (var article in _tousLesProduits) _produitsFiltres.Add(article);
        OnPropertyChanged(nameof(Familles));
        OnPropertyChanged(nameof(ProduitsFiltres));
    }

    public void FiltrerProduits(IEnumerable<Article>? source = null)
    {
        var articles = source ?? _tousLesProduits;
        var filtreFamille = FamilleSelectionnee?.Code;
        var recherche = RechercheText.Trim();
        var resultats = articles.Where(a => (filtreFamille is null || a.FamilleCode == filtreFamille)
            && (recherche.Length == 0 || a.Code.Contains(recherche, StringComparison.OrdinalIgnoreCase)
                || a.Designation.Contains(recherche, StringComparison.OrdinalIgnoreCase))).ToList();
        _produitsFiltres.Clear();
        foreach (var article in resultats) _produitsFiltres.Add(article);
        OnPropertyChanged(nameof(ProduitsFiltres));
    }

    private static string ObtenirEmoji(Article article) => article.FamilleCode switch
    {
        "BOISSONS" => article.Designation.Contains("café", StringComparison.OrdinalIgnoreCase) ? "☕" : "🥤",
        "BOULANGERIE" => "🥖",
        "PIZZAS" => "🍕",
        "SANDWICHS" => "🥪",
        _ => "📦",
    };

    private void SurTicketModifie()
    {
        OnPropertyChanged(nameof(Lignes));
        OnPropertyChanged(nameof(NumeroTicket));
        OnPropertyChanged(nameof(SousTotalHt));
        OnPropertyChanged(nameof(TotalTva));
        OnPropertyChanged(nameof(TotalTtc));
        OnPropertyChanged(nameof(TimbreFiscal));
        OnPropertyChanged(nameof(NbArticles));
        (PayerCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (AnnulerCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (MiseEnAttenteCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SurTablesVidees()
    {
        void Notifier() => RechargerProduits();
        // La maintenance peut vider les tables depuis un thread de fond :
        // marshaler la notification sur le thread du ViewModel sans jamais
        // bloquer l'émetteur (BeginInvoke, pas Invoke).
        if (_dispatcher.CheckAccess()) Notifier();
        else _dispatcher.BeginInvoke(Notifier);
    }

    public void Dispose()
    {
        Services.DatabaseMaintenanceService.TablesVidees -= SurTablesVidees;
        _caisse.TicketModifie -= SurTicketModifie;
        PayerDemande = null;
        AnnulationDemandee = null;
        MiseEnAttenteDemandee = null;
        RechercheClientDemandee = null;
        ArticleDemandee = null;
        ProduitsRecharges = null;
        _rechercheTimer?.Dispose();
    }
}

public sealed class RelayCommandParametre : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private EventHandler? _canExecuteChanged;

    public RelayCommandParametre(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => _canExecuteChanged += value;
        remove => _canExecuteChanged -= value;
    }
    public void RaiseCanExecuteChanged() => _canExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
