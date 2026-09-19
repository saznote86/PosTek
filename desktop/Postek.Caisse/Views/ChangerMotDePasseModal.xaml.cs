using System.Windows;
using System.Windows.Controls;
using Postec.Caisse.Caisse;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class ChangerMotDePasseModal : Window
{
    public ChangerMotDePasseModal(BaseDonnees bdd, UtilisateurSession utilisateur)
    {
        InitializeComponent();
        var vue = new ChangerMotDePasseViewModel(bdd, utilisateur);
        vue.MotDePasseChange += () => DialogResult = true;
        vue.AnnulationDemandee += () => DialogResult = false;
        DataContext = vue;
    }

    private void SurNouveauMotDePasseChange(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChangerMotDePasseViewModel vue && sender is PasswordBox champ)
            vue.NouveauMotDePasse = champ.Password;
    }

    private void SurConfirmationChange(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChangerMotDePasseViewModel vue && sender is PasswordBox champ)
            vue.Confirmation = champ.Password;
    }
}
