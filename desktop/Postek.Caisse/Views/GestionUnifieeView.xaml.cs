using System.Windows.Controls;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

/// <summary>
/// Hôte de l'onglet Gestion de l'Administration : possède son propre
/// NavigationService et amorce la pile avec le menu Gestion, de sorte que
/// chaque sous-vue (Produits, Clients, Familles...) s'ouvre dans l'onglet
/// et que « Retour » ramène au menu sans quitter l'Administration.
/// </summary>
public partial class GestionUnifieeView : UserControl
{
    private readonly NavigationService _navigation;

    public GestionUnifieeView()
    {
        InitializeComponent();
        _navigation = new NavigationService(HoteNavigation);
        var menu = new MenuGestionView(_navigation);
        // Retour depuis le menu racine : quitter l'Administration.
        menu.RetourRequested += () => RetourDemandee?.Invoke();
        _navigation.Naviguer(menu);
    }

    public event Action? RetourDemandee;
}
