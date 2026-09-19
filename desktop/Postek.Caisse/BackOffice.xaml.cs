using System.Windows;
using System.Windows.Controls;
using Postec.Caisse.Caisse;
using Postec.Caisse.Services;
using Postec.Caisse.Views;

namespace Postec.Caisse;

public partial class BackOffice : Window
{
    private readonly BaseDonnees _bdd;
    private UtilisateurAdmin? _selection;

    public BackOffice()
    {
        InitializeComponent();
        _bdd = new BaseDonnees(App.CheminBase);
        if (App.SessionCourante?.EstAdministrateur != true)
        {
            MessageBox.Show(this, "Accès réservé aux administrateurs.", "POSTEC",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        ChargerDonnees();
        BoutonCloture.Visibility = _bdd.SessionCaisseOuverte() is null
            ? Visibility.Collapsed : Visibility.Visible;
        PreviewKeyDown += SurRaccourci;
    }

    private void ChargerDonnees()
    {
        ListeUtilisateurs.ItemsSource = _bdd.ListerUtilisateurs();
        ListeAudit.ItemsSource = _bdd.ListerAudit();
    }

    private void SurSelectionUtilisateur(object sender, SelectionChangedEventArgs e)
    {
        _selection = ListeUtilisateurs.SelectedItem as UtilisateurAdmin;
        if (_selection is null) return;
        ChampIdentifiant.Text = _selection.Identifiant;
        ChampNom.Text = _selection.NomAffiche;
        ChampRole.SelectedIndex = _selection.Role == "Administrateur" ? 0 : 1;
        ChampMotDePasse.Clear();
    }

    private void SurNouveau(object sender, RoutedEventArgs e)
    {
        _selection = null;
        ListeUtilisateurs.SelectedItem = null;
        ChampIdentifiant.Clear();
        ChampNom.Clear();
        ChampRole.SelectedIndex = 1;
        ChampMotDePasse.Clear();
        ChampIdentifiant.Focus();
    }

    private void SurEnregistrer(object sender, RoutedEventArgs e)
    {
        var identifiant = ChampIdentifiant.Text.Trim();
        var nom = ChampNom.Text.Trim();
        var role = (ChampRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Caissier";
        var motDePasse = ChampMotDePasse.Password;
        if (string.IsNullOrWhiteSpace(identifiant) || string.IsNullOrWhiteSpace(nom))
        {
            MessageBox.Show(this, "Identifiant et nom affiché sont obligatoires.", "POSTEC",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (_selection is null)
            {
                if (string.IsNullOrEmpty(motDePasse))
                {
                    MessageBox.Show(this, "Un mot de passe est obligatoire pour un nouvel utilisateur.", "POSTEC",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _bdd.AjouterUtilisateur(identifiant, nom, role, motDePasse);
                _bdd.JournaliserAudit(App.SessionCourante, "Création utilisateur", identifiant);
            }
            else
            {
                _bdd.ModifierUtilisateur(_selection.Id, nom, role, _selection.Actif,
                    string.IsNullOrEmpty(motDePasse) ? null : motDePasse);
                _bdd.JournaliserAudit(App.SessionCourante, "Modification utilisateur", identifiant);
            }
            ChargerDonnees();
            SurNouveau(this, new RoutedEventArgs());
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            MessageBox.Show(this, "Cet identifiant existe déjà.", "POSTEC",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "POSTEC", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SurSupprimer(object sender, RoutedEventArgs e)
    {
        if (_selection is null) return;
        if (_selection.Id == App.SessionCourante?.Id)
        {
            MessageBox.Show(this, "Vous ne pouvez pas supprimer votre propre compte.", "POSTEC",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show(this, $"Supprimer « {_selection.Identifiant} » ?", "POSTEC",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _bdd.SupprimerUtilisateur(_selection.Id);
        _bdd.JournaliserAudit(App.SessionCourante, "Suppression utilisateur", _selection.Identifiant);
        ChargerDonnees();
        SurNouveau(this, new RoutedEventArgs());
    }

    private void SurActualiserAudit(object sender, RoutedEventArgs e) => ChargerDonnees();

    private void SurEffacerHistorique(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this,
                "Effacer l'historique des connexions affiché à l'écran de login ?\n\n" +
                "Les comptes et mots de passe ne sont pas touchés ; les compteurs repartent de zéro.",
                "POSTEC", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        _bdd.EffacerHistoriqueConnexions();
        _bdd.JournaliserAudit(App.SessionCourante, "Effacement historique connexions");
        ChargerDonnees();
    }

    private void SurEtats(object sender, RoutedEventArgs e) => AfficherPage("États fiscaux");
    private void SurComptabilite(object sender, RoutedEventArgs e) => AfficherPage("Comptabilité");

    private void SurAdministration(object sender, RoutedEventArgs e)
    {
        var administration = new Views.AdministrationView { Owner = this };
        administration.ShowDialog();
        ChargerDonnees();
    }

    private void SurUtilisateurs(object sender, RoutedEventArgs e)
    {
        PageUtilisateurs.Visibility = Visibility.Visible;
        PageIndisponible.Visibility = Visibility.Collapsed;
        ChargerDonnees();
    }

    private void SurCloture(object sender, RoutedEventArgs e) => OuvrirCloture();

    private void SurMigration(object sender, RoutedEventArgs e)
    {
        var fenetre = new FenetreMigration { Owner = this };
        fenetre.ShowDialog();
        ChargerDonnees();
    }

    private void SurRaccourci(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F8 && BoutonCloture.Visibility == Visibility.Visible)
        {
            e.Handled = true;
            OuvrirCloture();
        }
    }

    private void OuvrirCloture()
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
        BoutonCloture.Visibility = _bdd.SessionCaisseOuverte() is null
            ? Visibility.Collapsed : Visibility.Visible;
        ChargerDonnees();
    }

    private void AfficherPage(string titre)
    {
        PageUtilisateurs.Visibility = Visibility.Collapsed;
        PageIndisponible.Visibility = Visibility.Visible;
        TitrePage.Text = titre;
    }

    private void SurFermer(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _bdd.Dispose();
        base.OnClosed(e);
    }
}
