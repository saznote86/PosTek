using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
        NumberPressedCommand = new RelayCommandParametre(param => Appuyer(param?.ToString() ?? ""));
        DataContext = this;
        PreviewKeyDown += SurTouchePhysique;
    }

    public ICommand NumberPressedCommand { get; }

    private void Appuyer(string touche)
    {
        if (touche == "C")
        {
            ChampMotDePasse.Clear();
            return;
        }
        if (touche is "," or ".")
        {
            if (!ChampMotDePasse.Password.Contains(',')) ChampMotDePasse.Password += ',';
            return;
        }
        if (touche.Length == 1 && char.IsDigit(touche[0]))
            ChampMotDePasse.Password += touche;
    }

    private void SurTouchePhysique(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key >= Key.D0 && e.Key <= Key.D9)
        {
            Appuyer(((int)e.Key - (int)Key.D0).ToString());
            e.Handled = true;
        }
        else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
        {
            Appuyer(((int)e.Key - (int)Key.NumPad0).ToString());
            e.Handled = true;
        }
        else if (e.Key == Key.Decimal || e.Key == Key.OemComma)
        {
            Appuyer(",");
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            SurValider(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SurAnnuler(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }
    private void SurAnnuler(object sender, RoutedEventArgs e) => CancelRequested?.Invoke();
    private void SurValider(object sender, RoutedEventArgs e) => ValidateRequested?.Invoke(ChampMotDePasse.Password);
    private void SurUtilisateurSelectionne(object sender, SelectionChangedEventArgs e)
    {
        if (ChampIdentifiant.SelectedItem is HistoriqueConnexion)
            ChampMotDePasse.Focus();
    }
    public event Action? CancelRequested;
    public event Action<string>? ValidateRequested;
}
