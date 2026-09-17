using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Postek.Caisse.Caisse;

namespace Postek.Caisse;

public partial class MainWindow : Window
{
    private readonly CaisseService _caisse = new();
    private readonly BaseDonnees _bdd;

    public MainWindow()
    {
        InitializeComponent();

        var dossierDonnees = AppDomain.CurrentDomain.BaseDirectory;
        _bdd = BaseDonnees.CreerDemo(System.IO.Path.Combine(dossierDonnees, "postek_demo.db"));
        DataContext = _caisse;

        // Numérotation continue : le compteur reprend après le dernier ticket encaissé.
        _caisse.DefinirNumeroTicket(_bdd.DernierNumeroTicket() + 1);

        ConstruireClavier();
        _caisse.TicketModifie += RafraichirTotal;
        RafraichirTotal();
        RafraichirEtatZ();
        Closed += (_, _) => _bdd.Dispose();
    }

    private void ConstruireClavier()
    {
        PanneauArticles.Children.Clear();
        foreach (var article in _bdd.ListerArticles())
        {
            var bouton = new Button
            {
                Content = article.Designation,
                Style = (Style)Resources["ToucheArticle"],
                Width = 190,
                Height = 96,
                Tag = article,
            };
            bouton.Click += SurArticle;
            PanneauArticles.Children.Add(bouton);
        }
    }

    private void SurArticle(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Article article })
            _caisse.Ajouter(article);
    }

    private void SurVider(object sender, RoutedEventArgs e) => _caisse.Vider();

    private void SurEncaisser(object sender, RoutedEventArgs e)
    {
        if (_caisse.NbArticles == 0) return;
        OuvrirPaiement();
    }

    /// <summary>
    /// Fenêtre de paiement tactile : pavé numérique, modes espèces/CB/chèque/TR/avoir,
    /// multi-règlement, rendu monnaie calculé sur les espèces. Validée → ticket persisté
    /// (lignes + règlements), imprimé, compteur avancé.
    /// </summary>
    private void OuvrirPaiement()
    {
        var fenetre = new FenetrePaiement(_caisse) { Owner = this };
        if (fenetre.ShowDialog() != true) return;   // abandon

        var numero = _caisse.NumeroTicket;
        var reglements = fenetre.ReglementsValides;
        _bdd.EnregistrerTicket(numero, _caisse, reglements);   // persiste avant d'avancer
        var total = _caisse.Encaisser();

        ImprimerTicket(numero, total, reglements, fenetre.MonnaieRendue);

        TitreTicket.Text = $"Ticket n° {_caisse.NumeroTicket}";
        RafraichirTotal();
    }

    /// <summary>
    /// Impression ESC/POS 80 mm du ticket encaissé. Le récap TVA par taux est
    /// relu de la base (lignes du ticket persisté) — jamais recalculé à la volée,
    /// l'imprimé doit correspondre à ce qui a été comptabilisé.
    /// </summary>
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
                MentionsLegales = new[] { $"Ticket n° {numero} — TVA incluse" },
                NumeroTicket = numero,
                DateHeure = DateTime.Now,
                Lignes = _bdd.LignesDunTicket(numero)
                    .Select(l => new LigneTicketImpression(
                        l.Designation, l.Quantite, l.PrixTtc, l.TotalLigne))
                    .ToList(),
                RecapTva = recap.Select(r => new LigneRecapTvaImpression(r.Taux * 100, r.Ht, r.Tva)).ToList(),
                Reglements = reglements.Select(r => new LigneReglementImpression(
                        ModesReglement.LibelleTicket(r.Mode), r.Montant))
                    .ToList(),
                MonnaieRendue = monnaieRendue,
                TotalTtc = _bdd.TotalDunTicket(numero),
                Pied = "À bientôt !",
            };

            var flux = TicketEscPos.Generer(donnees);
            var resultat = ImprimanteBrute.Envoyer(flux, theme.Imprimante);

            if (resultat.Canal.StartsWith("fichier:"))
            {
                // Pas d'imprimante configurée : discret, mais traçable.
                TitreTicket.Text = $"Ticket n° {_caisse.NumeroTicket} (flux en attente d'imprimante)";
            }
        }
        catch (Exception ex)
        {
            // L'encaissement n'est jamais annulé pour un souci d'impression.
            MessageBox.Show(this, $"Impression impossible : {ex.Message}", "POSTEK",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// Impression automatique du rapport Z au moment de la clôture — mêmes
    /// conventions que le ticket (ESC/POS 80 mm, CP-863). Un souci d'impression
    /// ne remet jamais en cause la clôture, déjà persistée.
    /// </summary>
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
                Pied = "À bientôt !",
            };

            var flux = TicketEscPos.GenererRapportZ(donnees);
            ImprimanteBrute.Envoyer(flux, theme.Imprimante);
        }
        catch (Exception ex)
        {
            // La clôture est déjà persistée : on signale, sans annuler.
            MessageBox.Show(this, $"Rapport Z non imprimé : {ex.Message}", "POSTEK",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RafraichirTotal()
    {
        TexteTotal.Text = $"Total : {_caisse.TotalTtc:0.000} TND";
        TexteTva.Text = $"dont TVA : {_caisse.TotalTva:0.000} TND — HT : {_caisse.TotalHt:0.000} TND";
    }

    private void RafraichirEtatZ()
    {
        var z = _bdd.RecapZCourant();
        TexteEtatZ.Text = z.NbTickets == 0
            ? $"Z courant : n° {z.Numero} — aucun ticket"
            : $"Z courant : n° {z.Numero} — {z.NbTickets} ticket(s), {z.TotalTtc:0.000} TND, TVA {z.TotalTva:0.000} TND";
    }

    private void SurClotureZ(object sender, RoutedEventArgs e)
    {
        var z = _bdd.RecapZCourant();
        if (z.NbTickets == 0)
        {
            MessageBox.Show(this, "Aucun ticket à clôturer.", "POSTEK — Clôture Z",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Récap TVA par taux (19/13/7/0 %) + totaux par mode de règlement.
        var detail = string.Join("\n", z.LignesTva.Select(l =>
            $"  TVA {l.TauxTva * 100:0.##} % — HT {l.TotalHt:0.000} — TVA {l.TotalTva:0.000} — TTC {l.TotalTtc:0.000}"));
        if (z.ReglementsParMode.Count > 0)
        {
            detail += "\n\nRèglements :\n" + string.Join("\n", z.ReglementsParMode.Select(m =>
                $"  {ModesReglement.Libelle(m.Mode)} : {m.Total:0.000} TND"));
        }

        var confirmation = MessageBox.Show(
            this,
            $"Clôture Z n° {z.Numero}\n" +
            $"{z.NbTickets} ticket(s) — Total TTC {z.TotalTtc:0.000} TND\n" +
            $"HT {z.TotalHt:0.000} — TVA {z.TotalTva:0.000}\n\n{detail}\n\nConfirmer la clôture ?",
            "POSTEK — Clôture Z",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmation != MessageBoxResult.Yes) return;

        _bdd.CloturerZ(z);
        ImprimerRapportZ(z);

        MessageBox.Show(
            this,
            $"Z n° {z.Numero} clôturé.\n" +
            $"{z.NbTickets} ticket(s) rattaché(s) — {z.TotalTtc:0.000} TND (TVA {z.TotalTva:0.000}).",
            "POSTEK — Clôture Z",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        RafraichirEtatZ();
    }
}
