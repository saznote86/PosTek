using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Services;

public sealed class ParametresPeripheriquesViewModel : INotifyPropertyChanged
{
    private readonly SystemInfoService _systeme;
    private readonly SerialPortService _portsService;
    private readonly PrinterService _imprimantesService;
    private string _port = "COM1";
    private int _baudRate = 9600;
    private string _message = "";

    public ParametresPeripheriquesViewModel(SystemInfoService systeme, SerialPortService portsService,
        PrinterService imprimantesService)
    {
        _systeme = systeme;
        _portsService = portsService;
        _imprimantesService = imprimantesService;
        TesterPortCommand = new RelayCommand(TesterPort);
        OuvrirTiroirCommand = new RelayCommand(OuvrirTiroir);
        EtatTiroirCommand = new RelayCommand(VerifierTiroir);
        RetourCommand = new RelayCommand(Retour);
        Charger();
    }

    public ObservableCollection<InfoLigne> Informations { get; } = new();
    public IReadOnlyList<string> Ports => _portsService.ListerPorts();
    public IReadOnlyList<string> Imprimantes { get; private set; } = Array.Empty<string>();
    public string Port { get => _port; set { _port = value; OnPropertyChanged(); } }
    public int BaudRate { get => _baudRate; set { _baudRate = value; OnPropertyChanged(); } }
    public string? ImprimanteParDefaut { get; private set; }
    public string Message { get => _message; private set { _message = value; OnPropertyChanged(); } }
    public ICommand TesterPortCommand { get; }
    public ICommand OuvrirTiroirCommand { get; }
    public ICommand EtatTiroirCommand { get; }
    public ICommand RetourCommand { get; }
    public event Action? RetourDemande;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Charger()
    {
        try
        {
            var info = _systeme.Lire();
            Informations.Add(new("VERSIONS", info.Version));
            Informations.Add(new("NOM", Environment.MachineName));
            Informations.Add(new("OS", info.Os));
            Informations.Add(new("Proc ID", info.Processus));
            Informations.Add(new("Mémoire", info.Memoire));
            Informations.Add(new("Disque", info.Disque));
            Imprimantes = _imprimantesService.GetInstalledPrinters();
            ImprimanteParDefaut = _imprimantesService.GetDefaultPrinter();
            OnPropertyChanged(nameof(Imprimantes));
            OnPropertyChanged(nameof(ImprimanteParDefaut));
        }
        catch (Exception ex) { Message = $"Informations système indisponibles : {ex.Message}"; }
    }

    private void TesterPort()
    {
        try { Message = _portsService.TestPort(Port, BaudRate); }
        catch (Exception ex) { Message = $"Port COM inaccessible : {ex.Message}"; }
    }

    private void OuvrirTiroir()
    {
        try { Message = _portsService.OpenCashDrawer(Port); }
        catch (Exception ex) { Message = $"Ouverture du tiroir impossible : {ex.Message}"; }
    }

    private void VerifierTiroir()
    {
        try { Message = _portsService.CheckDrawerStatus(Port); }
        catch (Exception ex) { Message = $"Etat du tiroir indisponible : {ex.Message}"; }
    }

    public void Retour() => RetourDemande?.Invoke();
    private void OnPropertyChanged([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}

public sealed record InfoLigne(string Code, string Valeur);
