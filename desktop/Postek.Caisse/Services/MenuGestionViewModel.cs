using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Services;

public sealed class MenuGestionViewModel : INotifyPropertyChanged
{
    private readonly NavigationService _navigation;
    private readonly DatabaseMaintenanceService _dbMaintenance;

    public MenuGestionViewModel(NavigationService navigation, DatabaseMaintenanceService dbMaintenance)
    {
        _navigation = navigation;
        _dbMaintenance = dbMaintenance;
        ParametresCommand = Naviguer("ParametresView");
        ProgrammationAvanceeCommand = Naviguer("ProgrammationAvanceeView");
        ClaviersTactilesCommand = Naviguer("ClaviersTactilesView");
        ProduitsCommand = Naviguer("ProduitsView");
        FamillesCommand = Naviguer("FamillesView");
        VendeursCommand = Naviguer("VendeursView");
        MotDePasseCommand = Naviguer("MotsDePasseModal");
        VentilationsTarifsCommand = Naviguer("VentilationsTarifsView");
        ClientsCommand = Naviguer("ClientsView");
        ComptabiliteCommand = Naviguer("ComptabiliteView");
        HistoriqueTicketsCommand = Naviguer("HistoriqueTicketsView");
        CloturesCommand = Naviguer("CloturesView");
        OutilsCommand = Naviguer("OutilsView");
        ReindexerCommand = new RelayCommand(Reindexer);
        ReglageDateHeureCommand = Naviguer("ReglageDateHeureView");
        RetourCommand = new RelayCommand(() =>
        {
            if (RetourDemandee is not null) RetourDemandee();
            else _navigation.Retour();
        });
    }

    public ICommand ParametresCommand { get; }
    public ICommand ProgrammationAvanceeCommand { get; }
    public ICommand ClaviersTactilesCommand { get; }
    public ICommand ProduitsCommand { get; }
    public ICommand FamillesCommand { get; }
    public ICommand VendeursCommand { get; }
    public ICommand MotDePasseCommand { get; }
    public ICommand VentilationsTarifsCommand { get; }
    public ICommand ClientsCommand { get; }
    public ICommand ComptabiliteCommand { get; }
    public ICommand HistoriqueTicketsCommand { get; }
    public ICommand CloturesCommand { get; }
    public ICommand OutilsCommand { get; }
    public ICommand ReindexerCommand { get; }
    public ICommand ReglageDateHeureCommand { get; }
    public ICommand RetourCommand { get; }
    public event Action? RetourDemandee;

    public event PropertyChangedEventHandler? PropertyChanged;

    private ICommand Naviguer(string destination) =>
        new RelayCommand(() => _navigation.NaviguerVers(destination));

    private async void Reindexer()
    {
        try
        {
            await _dbMaintenance.RebuildIndexesAsync(App.CheminBase);
            _navigation.NaviguerVers("ReindexerTermine");
        }
        catch (Exception exception)
        {
            _navigation.NaviguerVers($"ErreurMaintenance:{exception.Message}");
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}