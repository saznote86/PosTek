using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Postec.Caisse.Caisse;
using Postec.Caisse.Services;
using Postec.Caisse.Views;
using Postec.Caisse.Views.Controls;

namespace Postec.Caisse;

public partial class MainWindow : Window
{
    private readonly CaisseService _caisse = new();
    private readonly CaisseViewModel _vue;
    private readonly BaseDonnees _bdd;
    private readonly DispatcherTimer _horlogeTimer = new();

    // Etat du clavier virtuel quantite
    private string _saisieQte = "1";
    private LigneTicket? _ligneSelectionnee;
    private IReadOnlyList<LigneTicket>? _ticketEnAttente;
    private string? _familleCourante;

    public MainWindow()
    {
        InitializeComponent();

        var dossierDonnees = AppDomain.CurrentDomain.BaseDirectory;
        _bdd = BaseDonnees.CreerDemo(System.IO.Path.Combine(dossierDonnees, "postec_demo.db"));
        _vue = new CaisseViewModel(_caisse);
        _vue.ChargerProduits(_bdd);
        DataContext = _vue;
        _vue.PayerDemande += () => SurEncaisser(this, new RoutedEventArgs());
        _vue.AnnulationDemandee += () => SurVider(this, new RoutedEventArgs());
        _vue.MiseEnAttenteDemandee += () => SurMiseEnAttente(this, new RoutedEventArgs());
        _vue.RechercheClientDemandee += () => SurRechercheClient(this, new RoutedEventArgs());
        _vue.ArticleDemandee += SurArticleDepuisViewModel;
        _vue.ProduitsRecharges += RechargerProduits;
        var administrateur = App.SessionCourante?.EstAdministrateur == true;
        BoutonBackOffice.IsEnabled = administrateur;
        BoutonMigration.IsEnabled = administrateur;
        BoutonClotureZ.IsEnabled = administrateur;
        TexteCaissier.Text = App.SessionCourante is null
            ? ""
            : $"Caissier : {App.SessionCourante.NomAffiche}";
        TexteEntreprise.Text = App.BrandCourant.Nom;
        TexteStatutCaisse.Text = _bdd.SessionCaisseOuverte() is null
            ? "CAISSE FERMÉE"
            : "CAISSE OUVERTE";

        _caisse.DefinirNumeroTicket(_bdd.DernierNumeroTicket() + 1);

        ConstruireFamilles();
        _caisse.TicketModifie += RafraichirPanier;
        RafraichirPanier();
        RafraichirEtatZ();

        // Horloge affichee dans la barre du haut
        _horlogeTimer.Interval = TimeSpan.FromSeconds(1);
        _horlogeTimer.Tick += (_, _) =>
            TexteHeureHaut.Text = DateTime.Now.ToString("HH:mm:ss");
        _horlogeTimer.Start();
        TexteHeureHaut.Text = DateTime.Now.ToString("HH:mm:ss");

        Closed += (_, _) =>
        {
            _horlogeTimer.Stop();
            _bdd.JournaliserAudit(App.SessionCourante, "Déconnexion");
            _vue.Dispose();
            _bdd.Dispose();
        };
    }

    // ------------------------------------------------------------------
    // Construction des onglets familles + grille articles
    // ------------------------------------------------------------------
    private void ConstruireFamilles()
    {
        PanneauFamilles.Children.Clear();
        PanneauFamilles.Orientation = Orientation.Horizontal;

        // Bouton "Tous"
        var btnTous = new RadioButton
        {
            Content = "Tous",
            IsChecked = true,
            Style = (Style)Resources["OngletFamille"],
            Background = (Brush)Application.Current.Resources["CouleurToucheFond"],
        };
        btnTous.Checked += (_, _) => ConstruireGrille(null);
        PanneauFamilles.Children.Add(btnTous);

        foreach (var famille in _bdd.ListerFamilles())
        {
            var f = famille; // capture locale
            SolidColorBrush? brosse = null;
            try
            {
                brosse = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(f.Couleur));
                brosse.Freeze();
            }
            catch { brosse = (SolidColorBrush)Application.Current.Resources["CouleurToucheFond"]; }

            var btn = new RadioButton
            {
                Content = f.Libelle,
                Style = (Style)Resources["OngletFamille"],
                Background = brosse,
                Tag = f.Code,
            };
            btn.Checked += (_, _) => ConstruireGrille(f.Code);
            PanneauFamilles.Children.Add(btn);
        }

        ConstruireGrille(null);
    }

    private void RechargerProduits()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(RechargerProduits);
            return;
        }

        _caisse.Vider();
        _familleCourante = null;
        ConstruireFamilles();
        RafraichirPanier();
        RafraichirEtatZ();
    }

    private void ConstruireGrille(string? familleCode)
    {
        _familleCourante = familleCode;
        GrilleArticles.Children.Clear();
        var articles = familleCode == null
            ? _bdd.ListerArticles()
            : _bdd.ListerArticlesParFamille(familleCode);

        var filtre = ChampCodeBarres?.Text.Trim();
        if (!string.IsNullOrWhiteSpace(filtre))
        {
            articles = articles.Where(a =>
                a.Code.Contains(filtre, StringComparison.OrdinalIgnoreCase) ||
                a.Designation.Contains(filtre, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        foreach (var article in articles)
        {
            GrilleArticles.Children.Add(new ProductCard
            {
                Article = article,
                Command = _vue.AjouterArticleCommand,
            });
        }
    }

    // ------------------------------------------------------------------
    // Raccourcis clavier globaux
    // ------------------------------------------------------------------
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        switch (e.Key)
        {
            case Key.F1:
                e.Handled = true;
                SurCaissePrincipale(this, new RoutedEventArgs());
                break;
            case Key.F2:
                e.Handled = true;
                SurGestionPrincipale(this, new RoutedEventArgs());
                break;
            case Key.F3:
                e.Handled = true;
                SurOutilsPrincipaux(this, new RoutedEventArgs());
                break;
            case Key.F8:
                e.Handled = true;
                SurClotureZ(this, new RoutedEventArgs());
                break;
            case Key.F4:
                e.Handled = true;
                SurMiseEnAttente(this, new RoutedEventArgs());
                break;
            case Key.F9:
                e.Handled = true;
                SurEncaisser(this, new RoutedEventArgs());
                break;
            case Key.F5:
                e.Handled = true;
                SurVider(this, new RoutedEventArgs());
                break;
            case Key.Delete:
            case Key.Back when Keyboard.Modifiers == ModifierKeys.None
                              && !(Keyboard.FocusedElement is TextBox):
                if (_ligneSelectionnee != null)
                {
                    _caisse.Supprimer(_ligneSelectionnee);
                    _ligneSelectionnee = null;
                    e.Handled = true;
                }
                break;
        }
    }

    // ------------------------------------------------------------------
    // Clics articles
    // ------------------------------------------------------------------
    private void SurArticle(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Article article })
        {
            // Quantite saisie dans le clavier virtuel (defaut 1)
            if (!decimal.TryParse(_saisieQte, NumberStyles.Number,
                                  CultureInfo.InvariantCulture, out var qte) || qte <= 0)
                qte = 1m;

            _caisse.Ajouter(article, qte);
            TexteArticleSelectionne.Text = article.Designation;

            // Remet le clavier a 1 apres chaque ajout
            _saisieQte = "1";
            TexteSaisieQte.Text = "1";
        }
    }

    private void SurArticleDepuisViewModel(Article article)
    {
        var qte = decimal.TryParse(_saisieQte, NumberStyles.Number,
            CultureInfo.InvariantCulture, out var valeur) && valeur > 0m ? valeur : 1m;
        _caisse.Ajouter(article, qte);
        TexteArticleSelectionne.Text = article.Designation;
        _saisieQte = "1";
        TexteSaisieQte.Text = "1";
    }

    private void SurMigration(object sender, RoutedEventArgs e)
    {
        if (App.SessionCourante?.EstAdministrateur != true) return;
        var fenetre = new FenetreMigration { Owner = this };
        fenetre.ShowDialog();
    }

    private void SurBackOffice(object sender, RoutedEventArgs e)
    {
        if (App.SessionCourante?.EstAdministrateur != true) return;
        var fenetre = new BackOffice { Owner = this };
        fenetre.ShowDialog();
    }

    private void SurCodeBarresKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return)
        {
            SurRechercherArticle(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void SurRechercherArticle(object sender, RoutedEventArgs e)
    {
        var texte = ChampCodeBarres.Text.Trim();
        ConstruireGrille(_familleCourante);
        if (string.IsNullOrEmpty(texte)) return;

        var articles = _bdd.ListerArticles()
            .Where(a => a.Code.Equals(texte, StringComparison.OrdinalIgnoreCase)
                     || a.Designation.Contains(texte, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (articles.Count == 1)
        {
            _caisse.Ajouter(articles[0]);
            ChampCodeBarres.Clear();
        }
        else if (articles.Count > 1)
        {
            // Affiche les resultats filtres dans la grille
            GrilleArticles.Children.Clear();
            foreach (var art in articles)
            {
                var a = art;
                var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                stack.Children.Add(new TextBlock { Text = a.Designation, FontSize = 13, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center });
                stack.Children.Add(new TextBlock { Text = string.Format("{0:0.000} TND", a.PrixTtc), FontSize = 12, TextAlignment = TextAlignment.Center, Foreground = (Brush)Application.Current.Resources["CouleurTexteSecondaire"] });
                var btn = new Button { Content = stack, Style = (Style)Resources["ToucheArticle"], Height = 90, Tag = a };
                btn.Click += SurArticle;
                GrilleArticles.Children.Add(btn);
            }
        }
        else
        {
            MessageBox.Show(this, "Aucun article trouve.", "POSTEC", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void SurRechercheTexteChangee(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        ConstruireGrille(_familleCourante);
    }

    // ------------------------------------------------------------------
    // Clavier virtuel quantite
    // ------------------------------------------------------------------
    private void SurToucheQte(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var tag = btn.Tag as string ?? "";

        switch (tag)
        {
            case "C":
                _saisieQte = "0";
                break;
            case "BS":
                _saisieQte = _saisieQte.Length > 1 ? _saisieQte[..^1] : "0";
                break;
            case "OK":
                AppliquerQuantiteSaisie();
                return;
            case "000":
            case "00":
                if (_saisieQte != "0") _saisieQte += tag;
                break;
            case ".":
                if (!_saisieQte.Contains('.')) _saisieQte += ".";
                break;
            default:
                if (_saisieQte == "0") _saisieQte = tag;
                else _saisieQte += tag;
                break;
        }
        TexteSaisieQte.Text = _saisieQte;
    }

    private void AppliquerQuantiteSaisie()
    {
        if (_ligneSelectionnee == null) return;
        if (decimal.TryParse(_saisieQte, NumberStyles.Number,
                             CultureInfo.InvariantCulture, out var qte) && qte > 0)
        {
            _caisse.ModifierQuantite(_ligneSelectionnee, qte);
            _ligneSelectionnee = null;
            _saisieQte = "1";
            TexteSaisieQte.Text = "1";
        }
    }

    private void SurActionRapide(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var tag = btn.Tag as string ?? "";

        switch (tag)
        {
            case "QTE1": _saisieQte = "1"; TexteSaisieQte.Text = "1"; break;
            case "QTE2": _saisieQte = "2"; TexteSaisieQte.Text = "2"; break;
            case "QTE5": _saisieQte = "5"; TexteSaisieQte.Text = "5"; break;
            case "REM5":
                if (_ligneSelectionnee != null) _caisse.AppliquerRemise(_ligneSelectionnee, 5m);
                break;
            case "REM10":
                if (_ligneSelectionnee != null) _caisse.AppliquerRemise(_ligneSelectionnee, 10m);
                break;
        }
    }

    // ------------------------------------------------------------------
    // Actions panier
    // ------------------------------------------------------------------
    private void SurSupprimerLigne(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: LigneTicket ligne })
        {
            _caisse.Supprimer(ligne);
            if (_ligneSelectionnee == ligne) _ligneSelectionnee = null;
        }
    }

    private void SurQuantiteLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        var ligne = tb.Tag as LigneTicket;
        if (ligne == null) return;

        if (decimal.TryParse(tb.Text, NumberStyles.Number,
                             CultureInfo.InvariantCulture, out var qte) && qte > 0)
        {
            _caisse.ModifierQuantite(ligne, qte);
        }
        else
        {
            // Valeur invalide : restaure l'affichage
            tb.Text = ligne.Quantite.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }

    private void SurQuantiteFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox { Tag: LigneTicket ligne })
            _ligneSelectionnee = ligne;
    }

    private void SurSelectionLigne(object sender, SelectionChangedEventArgs e)
    {
        if (ListeLignes.SelectedItem is LigneTicket ligne)
        {
            _ligneSelectionnee = ligne;
            TexteArticleSelectionne.Text = ligne.Article.Designation;
            _saisieQte = ligne.Quantite.ToString("0.###", CultureInfo.InvariantCulture);
            TexteSaisieQte.Text = _saisieQte;
        }
    }

    private void SurVider(object sender, RoutedEventArgs e)
    {
        if (_caisse.NbArticles > 0)
            _bdd.JournaliserAudit(App.SessionCourante, "Annulation vente",
                $"Ticket en cours {_caisse.NumeroTicket}");
        _caisse.Vider();
    }

    private void SurMiseEnAttente(object sender, RoutedEventArgs e)
    {
        if (_ticketEnAttente is null)
        {
            if (_caisse.NbArticles == 0) return;
            _ticketEnAttente = _caisse.Lignes.ToList();
            _caisse.Vider();
            TexteEtatZ.Text = "Ticket mis en attente - appuyez à nouveau pour le reprendre";
            return;
        }

        if (_caisse.NbArticles > 0)
        {
            MessageBox.Show(this, "Videz le ticket courant avant de reprendre le ticket en attente.",
                "POSTEC", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        foreach (var ligne in _ticketEnAttente)
            _caisse.Ajouter(ligne.Article, ligne.Quantite, ligne.RemisePct);
        _ticketEnAttente = null;
    }

    private void SurRechercheClient(object sender, RoutedEventArgs e)
    {
        ChampClient.Focus();
        ChampClient.SelectAll();
    }

    private void SurClientModifie(object sender, RoutedEventArgs e)
    {
        _vue.CodeClient = ChampClient.Text.Trim();
    }

    private void SurEncaisser(object sender, RoutedEventArgs e)
    {
        if (_caisse.NbArticles == 0) return;
        OuvrirPaiement();
    }

    private void SurCaissePrincipale(object sender, RoutedEventArgs e)
    {
        ChampCodeBarres.Focus();
    }

    private void SurGestionPrincipale(object sender, RoutedEventArgs e)
    {
        var fenetre = new Window
        {
            Title = "POSTEC - Menu Gestion",
            Content = new MenuGestionView(),
            Owner = this,
            Width = 1100,
            Height = 700,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
        };
        fenetre.ShowDialog();
    }

    private void SurOutilsPrincipaux(object sender, RoutedEventArgs e)
    {
        var vue = new OutilsView();
        var fenetre = new Window
        {
            Title = "POSTEC - Outils",
            Content = vue,
            Owner = this,
            Width = 1000,
            Height = 650,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
        };
        vue.CloseRequested += fenetre.Close;
        fenetre.ShowDialog();
    }

    private void SurQuitter(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Voulez-vous quitter POSTEC ?", "Confirmation",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            Close();
    }

    private void SurFenetrePreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            SurQuitter(sender, new RoutedEventArgs());
        }
    }

    // ------------------------------------------------------------------
    // Fenetre de paiement
    // ------------------------------------------------------------------
    private void OuvrirPaiement()
    {
        var fenetre = new FenetrePaiement(_caisse) { Owner = this };
        if (fenetre.ShowDialog() != true) return;

        var numero = _caisse.NumeroTicket;
        var reglements = fenetre.ReglementsValides;
        _bdd.EnregistrerTicket(numero, _caisse, reglements);
        var total = _caisse.Encaisser();

        ImprimerTicket(numero, total, reglements, fenetre.MonnaieRendue);

        TitreTicket.Text = string.Format("Ticket n {0}", _caisse.NumeroTicket);
        RafraichirPanier();
        RafraichirEtatZ();
        _ligneSelectionnee = null;
    }

    private void ImprimerTicket(int numero, decimal total,
                                IReadOnlyList<Reglement> reglements, decimal monnaieRendue)
    {
        try
        {
            var theme = App.ThemeCourant;
            var recap = _bdd.RecapTvaDunTicket(numero);
            var donnees = new DonneesTicket
            {
                EnTete = App.BrandCourant.Nom,
                SousTitre = App.BrandCourant.Slogan,
                Adresse = App.BrandCourant.Adresse,
                InfosSociete = App.BrandCourant.InfosSociete,
                MatriculeFiscal = App.BrandCourant.MatriculeFiscal,
                MentionsLegales = new[] { string.Format("Ticket n {0} - TVA incluse", numero) },
                NumeroTicket = numero,
                Caisse = "1",
                DateHeure = DateTime.Now,
                Lignes = _bdd.LignesDunTicket(numero)
                    .Select(l => new LigneTicketImpression(l.Designation, l.Quantite, l.PrixTtc, l.TotalLigne))
                    .ToList(),
                RecapTva = recap.Select(r => new LigneRecapTvaImpression(r.Taux * 100, r.Ht, r.Tva)).ToList(),
                Reglements = reglements.Select(r => new LigneReglementImpression(
                    ModesReglement.LibelleTicket(r.Mode), r.Montant)).ToList(),
                MonnaieRendue = monnaieRendue,
                SousTotalHt = _bdd.TotalDunTicket(numero) - recap.Sum(r => r.Tva),
                TimbreFiscal = _bdd.TotalDunTicket(numero) > 0m ? 1.000m : 0m,
                TotalTtc = _bdd.TotalDunTicket(numero),
                CodeBarres = $"{numero:0000000000}",
                Pied = "A bientot !",
            };
            var flux = TicketEscPos.Generer(donnees);
            var resultat = ImprimanteBrute.Envoyer(flux, theme.Imprimante);
            if (!resultat.Succes)
            {
                MessageBox.Show(this, resultat.Erreur ?? "Imprimante non disponible.",
                    "POSTEC - Impression", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (resultat.Canal.StartsWith("fichier:"))
                TitreTicket.Text = string.Format("Ticket n {0} (flux en attente)", _caisse.NumeroTicket);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format("Impression impossible : {0}", ex.Message),
                "POSTEC", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ------------------------------------------------------------------
    // Cloture Z
    // ------------------------------------------------------------------
    private void SurClotureZ(object sender, RoutedEventArgs e)
    {
        if (App.SessionCourante?.EstAdministrateur != true) return;
        OuvrirClotureComplete();
        return;
        /*
        var z = _bdd.RecapZCourant();
        if (z.NbTickets == 0)
        {
            MessageBox.Show(this, "Aucun ticket a cloturer.", "POSTEC - Cloture Z",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var detail = string.Join("\n", z.LignesTva.Select(l =>
            string.Format("  TVA {0:0.##} % - HT {1:0.000} - TVA {2:0.000} - TTC {3:0.000}",
                l.TauxTva * 100, l.TotalHt, l.TotalTva, l.TotalTtc)));
        if (z.ReglementsParMode.Count > 0)
        {
            detail += "\n\nReglements :\n" + string.Join("\n", z.ReglementsParMode.Select(m =>
                string.Format("  {0} : {1:0.000} TND", ModesReglement.Libelle(m.Mode), m.Total)));
        }

        var conf = MessageBox.Show(this,
            string.Format("Cloture Z n {0}\n{1} ticket(s) - Total TTC {2:0.000} TND\nHT {3:0.000} - TVA {4:0.000}\n\n{5}\n\nConfirmer la cloture ?",
                z.Numero, z.NbTickets, z.TotalTtc, z.TotalHt, z.TotalTva, detail),
            "POSTEC - Cloture Z", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (conf != MessageBoxResult.Yes) return;

        _bdd.CloturerZ(z);
        ImprimerRapportZ(z);

        MessageBox.Show(this,
            string.Format("Z n {0} cloture.\n{1} ticket(s) - {2:0.000} TND (TVA {3:0.000}).",
                z.Numero, z.NbTickets, z.TotalTtc, z.TotalTva),
            "POSTEC - Cloture Z", MessageBoxButton.OK, MessageBoxImage.Information);

        RafraichirEtatZ();
        */
    }

    private void OuvrirClotureComplete()
    {
        var vue = new ClotureView();
        var fenetre = new Window
        {
            Title = "POSTEC - Clôture de caisse",
            Content = vue,
            Owner = this,
            WindowState = WindowState.Maximized,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        vue.CloseRequested += fenetre.Close;
        fenetre.ShowDialog();
        vue.DisposeBase();
        RafraichirEtatZ();
    }

    private void ImprimerRapportZ(RecapZ z)
    {
        try
        {
            var theme = App.ThemeCourant;
            var donnees = new DonneesRapportZ
            {
                EnTete = App.BrandCourant.Nom,
                Numero = z.Numero,
                DateHeure = DateTime.Now,
                NbTickets = z.NbTickets,
                TotalTtc = z.TotalTtc,
                TotalHt = z.TotalHt,
                TotalTva = z.TotalTva,
                RecapTva = z.LignesTva.Select(l => new LigneRecapTvaImpression(
                    l.TauxTva * 100, l.TotalHt, l.TotalTva)).ToList(),
                Reglements = z.ReglementsParMode.Select(m => new LigneReglementImpression(
                    ModesReglement.LibelleTicket(m.Mode), m.Total)).ToList(),
                MonnaieRendue = z.MonnaieRendue,
                Pied = "A bientot !",
            };
            var flux = TicketEscPos.GenererRapportZ(donnees);
            ImprimanteBrute.Envoyer(flux, theme.Imprimante);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format("Rapport Z non imprime : {0}", ex.Message),
                "POSTEC", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ------------------------------------------------------------------
    // Rafraichissements UI
    // ------------------------------------------------------------------
    private void RafraichirPanier()
    {
        TitreTicket.Text = string.Format("Ticket n {0}", _caisse.NumeroTicket);
        var timbre = _caisse.TotalTtc > 0m ? 1.000m : 0m;
        TexteSousTotal.Text = string.Format("Sous-total HT : {0:0.000} TND", _caisse.TotalHt);
        TexteTvaDetail.Text = string.Format("TVA : {0:0.000} TND", _caisse.TotalTva);
        TexteTimbre.Text = string.Format("Timbre fiscal : {0:0.000} TND", timbre);
        TexteTotal.Text = string.Format("Total TTC : {0:0.000} TND", _caisse.TotalTtc);
    }

    private void RafraichirEtatZ()
    {
        var z = _bdd.RecapZCourant();
        TexteEtatZ.Text = z.NbTickets == 0
            ? string.Format("Z courant : n{0} - aucun ticket", z.Numero)
            : string.Format("Z courant : n{0} - {1} ticket(s), {2:0.000} TND, TVA {3:0.000}",
                z.Numero, z.NbTickets, z.TotalTtc, z.TotalTva);
    }
}
