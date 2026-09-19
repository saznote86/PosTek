using System.Windows;
using System.Windows.Input;
using Postec.Caisse.Caisse;
using Postec.Caisse.Views;
using Postec.Caisse.Services;

namespace Postec.Caisse;

public partial class AccueilWindow : Window
{
    private readonly NavigationService _navigation;
    private readonly AccueilView _accueil;

    public AccueilWindow()
    {
        InitializeComponent();
        _navigation = new NavigationService(HoteNavigation);
        _accueil = new AccueilView();
        _accueil.NavigationRequested += SurNavigation;
        HoteNavigation.Content = _accueil;
        PreviewKeyDown += SurRaccourci;
        InputBindings.Add(new KeyBinding(
            new RelayCommand(() => SurNavigation("caisse")),
            new KeyGesture(Key.F1)));
    }

    private void SurNavigation(string destination)
    {
        switch (destination)
        {
            case "caisse":
                new MainWindow { Owner = this }.ShowDialog();
                break;
            case "gestion":
                var gestion = new MenuGestionView(_navigation);
                gestion.RetourRequested += () => _navigation.Retour(_accueil);
                _navigation.Naviguer(gestion);
                break;
            case "outils":
                var outils = new OutilsView();
                outils.CloseRequested += () => _navigation.Retour(_accueil);
                _navigation.Naviguer(outils);
                break;
            case "licence":
                MessageBox.Show(this, "Licence POSTEC active sur ce poste.", "POSTEC", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
            case "quitter":
                Close();
                break;
        }
    }

    private void SurRaccourci(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F1) { e.Handled = true; SurNavigation("caisse"); }
        else if (e.Key == System.Windows.Input.Key.F2) { e.Handled = true; SurNavigation("gestion"); }
        else if (e.Key == System.Windows.Input.Key.F3) { e.Handled = true; SurNavigation("outils"); }
        else if (e.Key == System.Windows.Input.Key.F4) { e.Handled = true; SurNavigation("licence"); }
        else if (e.Key == System.Windows.Input.Key.F5) { e.Handled = true; SurNavigation("quitter"); }
    }
}
