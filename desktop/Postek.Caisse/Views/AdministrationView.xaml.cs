using System.Windows;

namespace Postec.Caisse.Views;

/// <summary>
/// Fenêtre d'administration unifiée : remplace les accès séparés
/// Gestion / Outils / Périphériques par une seule fenêtre à trois onglets.
/// Les sous-vues réutilisent les ViewModels existants (aucune logique
/// métier dupliquée).
/// </summary>
public partial class AdministrationView : Window
{
    public AdministrationView()
    {
        InitializeComponent();
        OngletGestion.RetourDemandee += Close;
        OngletOutils.RetourDemandee += Close;
    }
}
