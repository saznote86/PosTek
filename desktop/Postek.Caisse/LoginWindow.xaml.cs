using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Postec.Caisse.Caisse;
using Postec.Caisse.Services;

namespace Postec.Caisse;

public partial class LoginWindow : Window
{
    private readonly BaseDonnees _bdd;
    private readonly LoginViewModel _viewModel;
    private bool _dispose;

    public UtilisateurSession? Session => _viewModel.Session;
    public LoginWindow(string cheminBase) : this(new BaseDonnees(cheminBase)) { }

    private LoginWindow(BaseDonnees bdd)
    {
        _bdd = bdd;
        if (_bdd.CompterUtilisateurs() == 0) _bdd.GarantirAdministrateurParDefaut();
        _viewModel = new LoginViewModel(_bdd, new AuditService(_bdd.CheminFichier, () => Session));
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.ConnexionReussie += SurConnexionReussie;
        _viewModel.AnnulationDemandee += SurAnnulation;
        Closing += SurClosing;
        Loaded += SurLoaded;
    }

    private void SurLoaded(object? sender, RoutedEventArgs e) => PwdMotDePasse.Focus();
    private void SurChargement(object? sender, RoutedEventArgs e) => PwdMotDePasse.Focus();
    private void SurConnexionReussie() { if (!IsVisible || _dispose) return; DialogResult = true; }
    private void SurAnnulation() { if (!IsVisible || _dispose) return; DialogResult = false; }

    private void SurMotDePasseKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        _viewModel.ValiderCommand.Execute(null);
    }

    private void SurClosing(object? sender, CancelEventArgs e)
    {
        if (_dispose) return;
        _dispose = true;
        _viewModel.ConnexionReussie -= SurConnexionReussie;
        _viewModel.AnnulationDemandee -= SurAnnulation;
        _viewModel.Dispose();
        DataContext = null;
        Closing -= SurClosing;
        _bdd.Dispose();
    }
}