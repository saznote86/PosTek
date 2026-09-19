using System.Windows;
using System.Windows.Controls;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class MenuGestionView : UserControl
{
    private readonly NavigationService _navigation;
    private readonly Postec.Caisse.Services.MenuGestionViewModel _viewModel;
    public event Action? RetourRequested;

    public MenuGestionView() : this(null)
    {
    }

    public MenuGestionView(NavigationService? navigation)
    {
        InitializeComponent();
        _navigation = navigation ?? new NavigationService(new ContentControl());
        _navigation.DestinationDemandee += SurDestination;
        _viewModel = new Postec.Caisse.Services.MenuGestionViewModel(
            _navigation, new DatabaseMaintenanceService());
        _viewModel.RetourDemandee += SurRetourDemande;
        DataContext = _viewModel;
    }

    private void SurRetourDemande()
    {
        if (RetourRequested is not null)
            RetourRequested();
        else
            Window.GetWindow(this)?.Close();
    }

    private void SurDestination(string destination)
    {
        switch (destination)
        {
            case "ParametresView":
                var parametres = new ParametresView();
                parametres.AbandonRequested += () => _navigation.Retour();
                OuvrirVue(parametres);
                break;
            case "MotsDePasseModal":
                new MotsDePasseModal { Owner = Window.GetWindow(this) }.ShowDialog();
                break;
            case "OutilsView":
                var outils = new OutilsView();
                outils.CloseRequested += () => _navigation.Retour();
                OuvrirVue(outils);
                break;
            case "ProduitsView":
                OuvrirVue(new ProduitsView(_navigation));
                break;
            case "FamillesView":
                var familles = new FamillesView();
                familles.RetourDemandee += () => _navigation.Retour();
                OuvrirVue(familles);
                break;
            case "VendeursView":
                OuvrirVue(new VendeursView());
                break;
            case "ClientsView":
                OuvrirVue(new ClientsView());
                break;
            case "ComptabiliteView":
                OuvrirVue(new ComptabiliteView());
                break;
            case "HistoriqueTicketsView":
                OuvrirVue(new HistoriqueTicketsView());
                break;
            case "VentilationsTarifsView":
                OuvrirVue(new VentilationsTarifsView());
                break;
            case "ClaviersTactilesView":
                OuvrirVue(new ClaviersTactilesView());
                break;
            case "ProgrammationAvanceeView":
                OuvrirVue(new ProgrammationAvanceeView());
                break;
            case "ReglageDateHeureView":
                OuvrirVue(new ReglageDateHeureView());
                break;
            case "CloturesView":
                MessageBox.Show(Window.GetWindow(this), "La vue des clôtures est disponible depuis le menu Clôture.", "PosTec", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
            case "ReindexerTermine":
                MessageBox.Show(Window.GetWindow(this), "Réindexation terminée.", "PosTec", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
            default:
                var message = destination.StartsWith("ErreurMaintenance:", StringComparison.Ordinal)
                    ? destination["ErreurMaintenance:".Length..]
                    : $"L'écran « {destination} » n'est pas encore disponible.";
                MessageBox.Show(Window.GetWindow(this), message, "PosTec", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
        }
    }

    private void OuvrirVue(UserControl vue)
    {
        if (vue.DataContext is GestionStubViewModel stub)
            stub.RetourDemandee += () => _navigation.Retour();
        if (vue.DataContext is ClientsViewModel clients)
            clients.RetourDemandee += () => _navigation.Retour();
        if (vue.DataContext is VendeursViewModel vendeurs)
            vendeurs.RetourDemandee += () => _navigation.Retour();
        if (vue.DataContext is ProduitsViewModel produits)
            produits.RetourDemandee += () => _navigation.Retour();
        _navigation.Naviguer(vue);
    }
}
