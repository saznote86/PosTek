using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;
using Postec.Caisse.Caisse;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class AccueilView : UserControl
{
    public AccueilView()
    {
        InitializeComponent();
        var modele = new AccueilViewModel();
        DataContext = modele;
        modele.NavigationRequested += destination => NavigationRequested?.Invoke(destination);
    }

    private void SurCaisse(object sender, RoutedEventArgs e) => NavigationRequested?.Invoke("caisse");
    private void SurGestion(object sender, RoutedEventArgs e) => NavigationRequested?.Invoke("gestion");
    private void SurOutils(object sender, RoutedEventArgs e) => NavigationRequested?.Invoke("outils");
    private void SurLicence(object sender, RoutedEventArgs e) => NavigationRequested?.Invoke("licence");

    public event Action<string>? NavigationRequested;
}

public sealed class AccueilViewModel
{
    private readonly SaintsDuJourService _saints = new();
    public string Version => "V 1.0.0 PRO";
    public string NomPoste => "Poste N°2";
    public DateTime DateDuJour => DateTime.Now;
    public string DateTexte => DateDuJour.ToString("dddd dd/MM/yyyy", CultureInfo.GetCultureInfo("fr-FR"));
    public string SaintsTexte => _saints.Formater(DateTime.Now);
    public ICommand CaisseCommand { get; }
    public ICommand GestionCommand { get; }
    public ICommand OutilsCommand { get; }
    public ICommand InfoLicenceCommand { get; }
    public ICommand QuitterCommand { get; }
    public event Action<string>? NavigationRequested;

    public AccueilViewModel()
    {
        CaisseCommand = new RelayCommand(() => NavigationRequested?.Invoke("caisse"));
        GestionCommand = new RelayCommand(() => NavigationRequested?.Invoke("gestion"));
        OutilsCommand = new RelayCommand(() => NavigationRequested?.Invoke("outils"));
        InfoLicenceCommand = new RelayCommand(() => NavigationRequested?.Invoke("licence"));
        QuitterCommand = new RelayCommand(() => NavigationRequested?.Invoke("quitter"));
    }
}
