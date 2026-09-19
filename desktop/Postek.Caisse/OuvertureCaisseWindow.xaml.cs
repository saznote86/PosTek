using System.Windows;
using Postec.Caisse.Caisse;

namespace Postec.Caisse;

public partial class OuvertureCaisseWindow : Window
{
    private readonly OuvertureCaisseViewModel _vue;

    public OuvertureCaisseWindow(BaseDonnees bdd, UtilisateurSession utilisateur)
    {
        InitializeComponent();
        _vue = new OuvertureCaisseViewModel(bdd, utilisateur);
        DataContext = _vue;
    }

    private void SurOuvrir(object sender, RoutedEventArgs e)
    {
        if (_vue.EstOuverte || _vue.Ouvrir())
            DialogResult = true;
    }
}
