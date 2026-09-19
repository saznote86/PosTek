using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Services;

public sealed record ArticleLigne(
    string Code, string Designation, decimal PrixTtc, decimal TauxTva,
    string? FamilleCode = null, string? NomFamille = null,
    int? ImprimanteId = null, string? NomImprimante = null,
    string? CheminImage = null);

public sealed record ArticleEdition(
    string Code, string Designation, decimal PrixTtc, decimal TauxTva = 0.190m,
    string? FamilleCode = null, int? ImprimanteId = null, string? CheminImage = null);

/// <summary>ViewModel CRUD du catalogue article.</summary>
public sealed class ProduitsViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly string _cheminBase;
    private readonly Action<ArticleLigne?, Action<ArticleEdition?>?>? _ouvrirEditeur;
    private readonly AuditService _audit;
    private readonly Func<bool> _confirmerSuppression;
    private readonly Func<UtilisateurSession?> _utilisateurCourant;
    private readonly Dispatcher _dispatcher;
    private ArticleLigne? _articleSelectionne;
    private string _message = "";
    private string _recherche = "";
    private string _familleFiltre = "";

    public ProduitsViewModel(
        string cheminBase,
        Action? retour = null,
        Action<ArticleLigne?, Action<ArticleEdition?>?>? ouvrirEditeur = null,
        AuditService? audit = null,
        Func<bool>? confirmerSuppression = null,
        Func<UtilisateurSession?>? utilisateurCourant = null)
    {
        _cheminBase = cheminBase ?? throw new ArgumentNullException(nameof(cheminBase));
        _ouvrirEditeur = ouvrirEditeur;
        _utilisateurCourant = utilisateurCourant ?? (() => App.SessionCourante);
        _audit = audit ?? new AuditService(_cheminBase, _utilisateurCourant);
        _confirmerSuppression = confirmerSuppression ?? (() => true);
        _dispatcher = Dispatcher.CurrentDispatcher;
        DatabaseMaintenanceService.TablesVidees += SurTablesVidees;
        Articles = new ObservableCollection<ArticleLigne>();
        ArticlesFiltres = CollectionViewSource.GetDefaultView(Articles);
        ArticlesFiltres.Filter = ArticleCorrespond;
        ChargerCommand = new RelayCommand(ChargerArticles);
        ReinitialiserFiltresCommand = new RelayCommand(ReinitialiserFiltres);
        NouveauCommand = new RelayCommand(CreerDepuisModale, () => _ouvrirEditeur is not null);
        ModifierCommand = new RelayCommand(ModifierDepuisModale, () => ArticleSelectionne is not null && _ouvrirEditeur is not null);
        SupprimerCommand = new RelayCommand(SupprimerArticle, () => ArticleSelectionne is not null);
        RetourCommand = new RelayCommand(() => { retour?.Invoke(); RetourDemandee?.Invoke(); });
        Familles = new ObservableCollection<FamilleArticle>();
        ChargerFamilles();
        ChargerArticles();
    }

    public ObservableCollection<ArticleLigne> Articles { get; }
    public ObservableCollection<ArticleLigne> Produits => Articles;
    public ICollectionView ArticlesFiltres { get; }
    public ObservableCollection<FamilleArticle> Familles { get; }
    public string Recherche
    {
        get => _recherche;
        set
        {
            if (_recherche == value) return;
            _recherche = value ?? "";
            ActualiserFiltre();
        }
    }

    public string FiltreRecherche
    {
        get => Recherche;
        set => Recherche = value;
    }

    public string FamilleFiltre
    {
        get => _familleFiltre;
        set
        {
            if (_familleFiltre == value) return;
            _familleFiltre = value ?? "";
            ActualiserFiltre();
        }
    }

    public int NombreArticlesFiltres => ArticlesFiltres.Cast<ArticleLigne>().Count();
    public string FamilleActive => string.IsNullOrWhiteSpace(FamilleFiltre)
        ? "Toutes les familles"
        : Familles.FirstOrDefault(f => f.Code == FamilleFiltre)?.Libelle ?? "Famille inconnue";
    public ArticleLigne? ArticleSelectionne
    {
        get => _articleSelectionne;
        set
        {
            if (Equals(_articleSelectionne, value)) return;
            _articleSelectionne = value;
            OnPropertyChanged();
            (ModifierCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SupprimerCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public string Message { get => _message; private set { _message = value; OnPropertyChanged(); } }
    public ICommand ChargerCommand { get; }
    public ICommand ReinitialiserFiltresCommand { get; }
    public ICommand NouveauCommand { get; }
    public ICommand ModifierCommand { get; }
    public ICommand SupprimerCommand { get; }
    public ICommand RetourCommand { get; }
    public event Action? RetourDemandee;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void ChargerArticles()
    {
        Articles.Clear();
        using var connexion = new SqliteConnection($"Data Source={_cheminBase}");
        connexion.Open();
        using var commande = connexion.CreateCommand();
        commande.CommandText = @"
            SELECT a.code, a.designation, a.prix_ttc, a.taux_tva,
                     a.famille_code, f.libelle, a.imprimante_id, i.nom, a.chemin_image
            FROM article a
            LEFT JOIN famille_article f ON f.code = a.famille_code
            LEFT JOIN imprimante i ON i.id = a.imprimante_id
            ORDER BY a.designation";
        using var lecture = commande.ExecuteReader();
        while (lecture.Read())
        {
            Articles.Add(new ArticleLigne(
                lecture.GetString(0),
                lecture.GetString(1),
                DecimalStocke(lecture.GetString(2)),
                DecimalStocke(lecture.GetString(3)),
                lecture.IsDBNull(4) ? null : lecture.GetString(4),
                lecture.IsDBNull(5) ? null : lecture.GetString(5),
                lecture.IsDBNull(6) ? null : lecture.GetInt32(6),
                lecture.IsDBNull(7) ? null : lecture.GetString(7),
                lecture.IsDBNull(8) ? null : lecture.GetString(8)));
        }
        Message = $"{Articles.Count} produit(s) chargé(s).";
        ActualiserFiltre();
    }

    public void Recharger(string? familleCode)
    {
        FamilleFiltre = familleCode ?? "";
        ChargerArticles();
    }

    public void ChargerFamilles()
    {
        Familles.Clear();
        Familles.Add(new FamilleArticle("", "Toutes les familles", 0, "#3B82F6"));
        using var connexion = Ouvrir();
        using var commande = connexion.CreateCommand();
        commande.CommandText = "SELECT code, libelle, ordre, couleur FROM famille_article ORDER BY ordre, libelle";
        using var lecture = commande.ExecuteReader();
        while (lecture.Read())
        {
            Familles.Add(new FamilleArticle(
                lecture.GetString(0), lecture.GetString(1), lecture.GetInt32(2), lecture.GetString(3)));
        }
        OnPropertyChanged(nameof(FamilleActive));
    }

    private void ReinitialiserFiltres()
    {
        Recherche = "";
        FamilleFiltre = "";
        ChargerArticles();
    }

    public void CreerArticle(ArticleEdition article)
    {
        Valider(article);
        using var connexion = Ouvrir();
        using var commande = connexion.CreateCommand();
        commande.CommandText = @"INSERT INTO article
            (code, designation, prix_ttc, taux_tva, famille_code, imprimante_id, chemin_image)
            VALUES($code, $designation, $prix, $tva, $famille, $imprimante, $image)";
        AjouterParametres(commande, article);
        AjouterParametresMetier(commande, article);
        commande.ExecuteNonQuery();
        _audit.EnregistrerAudit(Utilisateur(), "CreationProduit",
            DetailsAudit(article));
        ChargerArticles();
        ChargerFamilles();
        Message = "Produit créé.";
    }

    public void ModifierArticle(ArticleLigne original, ArticleEdition article)
    {
        Valider(article);
        using var connexion = Ouvrir();
        using var commande = connexion.CreateCommand();
        commande.CommandText = @"UPDATE article SET code=$code, designation=$designation,
            prix_ttc=$prix, taux_tva=$tva, famille_code=$famille, imprimante_id=$imprimante,
            chemin_image=$image
            WHERE code=$ancienCode";
        AjouterParametres(commande, article);
        commande.Parameters.AddWithValue("$ancienCode", original.Code);
        AjouterParametresMetier(commande, article);
        commande.ExecuteNonQuery();
        _audit.EnregistrerAudit(Utilisateur(), "ModificationProduit", DetailsAudit(article));
        ChargerArticles();
        ChargerFamilles();
        Message = "Produit modifié.";
    }

    public void SupprimerArticle()
    {
        if (ArticleSelectionne is null) return;
        if (!_confirmerSuppression()) return;
        SupprimerArticleSelectionne();
    }

    public bool ConfirmerEtSupprimer()
    {
        if (ArticleSelectionne is null || !_confirmerSuppression()) return false;
        SupprimerArticleSelectionne();
        return true;
    }

    private void SupprimerArticleSelectionne()
    {
        if (ArticleSelectionne is null) return;
        var article = ArticleSelectionne;
        using var connexion = Ouvrir();
        using var commande = connexion.CreateCommand();
        commande.CommandText = "DELETE FROM article WHERE code=$code";
        commande.Parameters.AddWithValue("$code", ArticleSelectionne.Code);
        commande.ExecuteNonQuery();
        _audit.EnregistrerAudit(Utilisateur(), "SuppressionProduit",
            $"Code={article.Code}, Nom={article.Designation}, Prix={article.PrixTtc:0.000}");
        ChargerArticles();
        ChargerFamilles();
        Message = "Produit supprimé.";
    }

    private bool ArticleCorrespond(object item)
    {
        if (item is not ArticleLigne article) return false;
        var texte = Recherche.Trim();
        var correspondRecherche = string.IsNullOrWhiteSpace(texte)
            || article.Code.Contains(texte, StringComparison.OrdinalIgnoreCase)
            || article.Designation.Contains(texte, StringComparison.OrdinalIgnoreCase);
        return correspondRecherche
            && (string.IsNullOrWhiteSpace(FamilleFiltre) || article.FamilleCode == FamilleFiltre);
    }

    private void ActualiserFiltre()
    {
        ArticlesFiltres.Refresh();
        OnPropertyChanged(nameof(NombreArticlesFiltres));
        OnPropertyChanged(nameof(FamilleActive));
    }

    private void CreerDepuisModale() => _ouvrirEditeur?.Invoke(null, article =>
    {
        if (article is not null) CreerArticle(article);
    });

    private void ModifierDepuisModale()
    {
        if (ArticleSelectionne is null) return;
        var original = ArticleSelectionne;
        _ouvrirEditeur?.Invoke(original, article =>
        {
            if (article is not null) ModifierArticle(original, article);
        });
    }

    private SqliteConnection Ouvrir()
    {
        var connexion = new SqliteConnection($"Data Source={_cheminBase}");
        connexion.Open();
        return connexion;
    }

    private static void AjouterParametres(SqliteCommand commande, ArticleEdition article)
    {
        commande.Parameters.AddWithValue("$code", article.Code.Trim());
        commande.Parameters.AddWithValue("$designation", article.Designation.Trim());
        commande.Parameters.AddWithValue("$prix", article.PrixTtc.ToString("0.000", CultureInfo.InvariantCulture));
        commande.Parameters.AddWithValue("$tva", article.TauxTva.ToString("0.000", CultureInfo.InvariantCulture));
    }

    private static void AjouterParametresMetier(SqliteCommand commande, ArticleEdition article)
    {
        commande.Parameters.AddWithValue("$famille", (object?)article.FamilleCode ?? DBNull.Value);
        commande.Parameters.AddWithValue("$imprimante", (object?)article.ImprimanteId ?? DBNull.Value);
        commande.Parameters.AddWithValue("$image", (object?)article.CheminImage ?? DBNull.Value);
    }

    private static string DetailsAudit(ArticleEdition article) =>
        $"Code={article.Code.Trim()}, Nom={article.Designation.Trim()}, Prix={article.PrixTtc:0.000}, TVA={article.TauxTva:0.000}, Famille={article.FamilleCode ?? "-"}, Imprimante={article.ImprimanteId?.ToString() ?? "-"}";

    private static void Valider(ArticleEdition article)
    {
        if (string.IsNullOrWhiteSpace(article.Code)) throw new ArgumentException("Le code est obligatoire.");
        if (string.IsNullOrWhiteSpace(article.Designation)) throw new ArgumentException("La désignation est obligatoire.");
        if (article.PrixTtc < 0m) throw new ArgumentException("Le prix ne peut pas être négatif.");
        if (article.TauxTva < 0m || article.TauxTva > 1m) throw new ArgumentException("Le taux TVA doit être une fraction entre 0 et 1.");
    }

    private static decimal DecimalStocke(string valeur) =>
        decimal.Parse(valeur, NumberStyles.Number, CultureInfo.InvariantCulture);

    private string Utilisateur() => _utilisateurCourant()?.Identifiant ?? "systeme";

    private void SurTablesVidees()
    {
        void Recharger()
        {
            ChargerFamilles();
            ChargerArticles();
        }

        // La RAZ peut être déclenchée depuis un thread de fond : marshaler
        // le rechargement sur le thread du ViewModel sans jamais bloquer
        // l'émetteur (BeginInvoke, pas Invoke).
        if (_dispatcher.CheckAccess()) Recharger();
        else _dispatcher.BeginInvoke(Recharger);
    }

    public void Dispose() => DatabaseMaintenanceService.TablesVidees -= SurTablesVidees;

    private void OnPropertyChanged([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}