using System.Windows;
using System.Windows.Controls;
using System.Linq;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class ProduitsView : UserControl
{
    private readonly NavigationService _navigation;

    public ProduitsView(NavigationService? navigation = null)
    {
        InitializeComponent();
        _navigation = navigation ?? new NavigationService(new ContentControl());

        DataContext = new ProduitsViewModel(
            App.CheminBase,
            retour: SurRetourCommande,
            ouvrirEditeur: OuvrirEditeurProduit,
            confirmerSuppression: ConfirmerSuppression);
    }

    private void OuvrirEditeurProduit(ArticleLigne? article, Action<ArticleEdition?>? suite)
    {
        var modale = new ArticleModale(article) { Owner = Window.GetWindow(this) };
        try
        {
            suite?.Invoke(modale.ShowDialog() == true ? modale.Article : null);
        }
        finally
        {
            modale.Close();
        }
    }

    private bool ConfirmerSuppression()
    {
        if (DataContext is not ProduitsViewModel vm || vm.ArticleSelectionne is null)
            return false;

        return MessageBox.Show(
            Window.GetWindow(this),
            $"Confirmer la suppression de l'article {vm.ArticleSelectionne.Designation} ?\n\nCette action est irréversible.",
            "POSTEC",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private void SurGererFamilles(object sender, RoutedEventArgs e)
    {
        var fenetre = new Window
        {
            Title = "POSTEC - Gestion des familles",
            Content = new FamillesView(),
            Owner = Window.GetWindow(this),
            Width = 800,
            Height = 600,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        fenetre.ShowDialog();
        if (DataContext is ProduitsViewModel modele)
        {
            modele.ChargerFamilles();
            modele.ChargerArticles();
        }
    }

    private void SurRetourCommande() => _navigation.Retour();
}