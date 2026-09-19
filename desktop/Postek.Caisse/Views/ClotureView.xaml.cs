using System.Windows;
using System.Windows.Controls;
using Postec.Caisse.Caisse;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class ClotureView : UserControl
{
    private readonly BaseDonnees _bdd;
    private readonly ClotureViewModel _vue;

    public ClotureView()
    {
        InitializeComponent();
        _bdd = new BaseDonnees(App.CheminBase);
        var utilisateur = App.SessionCourante ?? throw new InvalidOperationException("Utilisateur non connecté.");
        _vue = new ClotureViewModel(_bdd, utilisateur)
        {
            Confirmation = () => MessageBox.Show(Window.GetWindow(this),
                "Confirmer la clôture de la caisse ? Cette action est irréversible.",
                "POSTEC - Clôture", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes,
        };
        _vue.ClotureReussie += rapport =>
        {
            MessageBox.Show(Window.GetWindow(this), $"Clôture réussie. Rapport Z n°{rapport.NumeroZ}.",
                "POSTEC", MessageBoxButton.OK, MessageBoxImage.Information);
            CloseRequested?.Invoke();
        };
        _vue.AnnulerDemandee += () => CloseRequested?.Invoke();
        DataContext = _vue;
    }

    public event Action? CloseRequested;

    public void DisposeBase() => _bdd.Dispose();
}
