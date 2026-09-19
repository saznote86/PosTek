using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Services;

public sealed class OutilsViewModel : INotifyPropertyChanged
{
    private readonly DatabaseMaintenanceService _maintenance;
    private readonly SystemInfoService _systeme;
    private readonly PrinterService _imprimantes;
    private readonly SerialPortService _ports;
    private readonly UsbKeyService _usb;
    private string _message = "";

    public OutilsViewModel(DatabaseMaintenanceService maintenance,
        SystemInfoService? systeme = null, PrinterService? imprimantes = null,
        SerialPortService? ports = null, UsbKeyService? usb = null)
    {
        _maintenance = maintenance;
        _systeme = systeme ?? new SystemInfoService();
        _imprimantes = imprimantes ?? new PrinterService();
        _ports = ports ?? new SerialPortService();
        _usb = usb ?? new UsbKeyService();
        ActiverLicenceCommand = new RelayCommand(ActiverLicence);
        OuvrirReindexerCommand = new RelayCommand(() => ReindexerDemandee?.Invoke());
        OuvrirMotsDePasseCommand = new RelayCommand(() => MotsDePasseDemandee?.Invoke());
        PremiereInstallationCommand = new RelayCommand(PremiereInstallation);
        EffacerDonneesCommand = new RelayCommand(EffacerDonnees);
        RemiseAZCommand = new RelayCommand(RemiseAZ);
        HorlogeCommand = new RelayCommand(AfficherHorloge);
        ReparerIndexCommand = new RelayCommand(ReparerIndex);
        TestPortsCommand = new RelayCommand(TestPorts);
        ConsolidationCommand = new RelayCommand(Consolider);
        RetourCommand = new RelayCommand(Retour);
    }

    public ICommand ActiverLicenceCommand { get; }
    public ICommand OuvrirReindexerCommand { get; }
    public ICommand OuvrirMotsDePasseCommand { get; }
    public ICommand PremiereInstallationCommand { get; }
    public ICommand EffacerDonneesCommand { get; }
    public ICommand RemiseAZCommand { get; }
    public ICommand HorlogeCommand { get; }
    public ICommand ReparerIndexCommand { get; }
    public ICommand TestPortsCommand { get; }
    public ICommand ConsolidationCommand { get; }
    public ICommand RetourCommand { get; }
    public ICommand LicenceCommand => ActiverLicenceCommand;
    public ICommand ReindexerCommand => OuvrirReindexerCommand;
    public ICommand MotsDePasseCommand => OuvrirMotsDePasseCommand;
    public ICommand RAZCommand => RemiseAZCommand;
    public string Message { get => _message; private set { _message = value; OnPropertyChanged(); } }

    /// <summary>
    /// Message neutre affiché après une première installation : aucun
    /// identifiant ni mot de passe en clair ; le compte administrateur est
    /// créé avec changement de mot de passe obligatoire à la première
    /// connexion (PBKDF2), ce qui suffit à la prise en main.
    /// </summary>
    public static string MessageFinPremiereInstallation =>
        "Première installation terminée.\n\nConnectez-vous avec le compte administrateur ; " +
        "un changement de mot de passe vous sera demandé à la première ouverture de session.";
    public Func<bool>? ConfirmationRemiseAZ { get; set; }
    public Func<string?>? DemanderCodeRAZ { get; set; }
    public Func<bool>? ConfirmationPremiereInstallation { get; set; }
    public event Action<string>? NotificationDemandee;
    public event Action? ReindexerDemandee;
    public event Action? MotsDePasseDemandee;
    public event Action? RetourDemande;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void RemiseAZ()
    {
        Message = "";
        if (ConfirmationRemiseAZ is not null && !ConfirmationRemiseAZ()) return;
        if (!string.Equals(DemanderCodeRAZ?.Invoke(), "RAZ", StringComparison.Ordinal)) return;
        try
        {
            var backup = _maintenance.CreerBackup(App.CheminBase, "avant_raz");
            _maintenance.ViderTablesMetier(App.CheminBase, DatabaseMaintenanceService.TablesMetierParDefaut);
            using (var bdd = new BaseDonnees(App.CheminBase))
            {
                var administrateur = bdd.CreerOuReinitialiserAdministrateur("admin", true);
                bdd.JournaliserAudit(administrateur, "RAZ effectuée", backup);
            }
            JournaliserOperation("RAZ effectuée", backup);
            Message = $"Remise à zéro effectuée. Backup : {backup}";
            NotificationDemandee?.Invoke($"Remise à zéro effectuée.\n\nBackup conservé dans :\n{backup}");
        }
        catch (Exception ex) { Message = $"Remise à zéro impossible : {ex.Message}"; }
    }

    private void ActiverLicence() => Message = "La licence POSTEC est active sur ce poste.";

    private void PremiereInstallation()
    {
        try
        {
            var compteurs = _maintenance.CompterDonneesMetier(App.CheminBase);
            if (compteurs.Values.Any(compteur => compteur > 0) &&
                ConfirmationPremiereInstallation is not null && !ConfirmationPremiereInstallation()) return;
            if (ConfirmationRemiseAZ is not null && !ConfirmationRemiseAZ()) return;

            var backup = _maintenance.CreerBackup(App.CheminBase, "avant_installation");
            _maintenance.ViderTablesMetier(App.CheminBase, DatabaseMaintenanceService.TablesMetierParDefaut);
            _maintenance.InitialiserParametresParDefaut(App.CheminBase);
            using var bdd = new BaseDonnees(App.CheminBase);
            var administrateur = bdd.CreerOuReinitialiserAdministrateur("admin", true);
            bdd.CreerSessionCaisseInitialeFermee(administrateur.Id);
            bdd.JournaliserAudit(administrateur, "Première installation effectuée", backup);
            Message = $"Première installation terminée. Backup : {backup}";
            NotificationDemandee?.Invoke(MessageFinPremiereInstallation);
        }
        catch (Exception exception) { Message = $"Première installation impossible : {exception.Message}"; }
    }

    private void EffacerDonnees()
    {
        if (ConfirmationRemiseAZ is not null && !ConfirmationRemiseAZ()) return;
        try
        {
            _maintenance.RemiseAZ(App.CheminBase);
            Message = "Données effacées. Les utilisateurs et la licence ont été conservés.";
        }
        catch (Exception exception) { Message = $"Effacement impossible : {exception.Message}"; }
    }

    private static void JournaliserOperation(string operation, string backup)
    {
        var dossier = Path.Combine(Path.GetDirectoryName(App.CheminBase) ?? AppContext.BaseDirectory, "donnees");
        Directory.CreateDirectory(dossier);
        var chemin = Path.Combine(dossier, "raz_log.txt");
        File.AppendAllText(chemin,
            $"{DateTime.UtcNow:o} - {operation} par {App.SessionCourante?.Identifiant ?? "systeme"}. Backup : {backup}{Environment.NewLine}");
    }

    private void AfficherHorloge()
    {
        try
        {
            Process.Start(new ProcessStartInfo("timedate.cpl") { UseShellExecute = true });
            Message = "Réglage de l'horloge système ouvert.";
        }
        catch (Exception exception) { Message = $"Réglage de l'horloge impossible : {exception.Message}"; }
    }

    private void ReparerIndex()
    {
        try
        {
            _maintenance.Reindexer(App.CheminBase);
            Message = "Index réparés avec succès.";
        }
        catch (Exception exception) { Message = $"Réparation impossible : {exception.Message}"; }
    }

    private void TestPorts()
    {
        var port = _ports.ListerPorts().FirstOrDefault() ?? "COM1";
        var imprimante = _imprimantes.GetDefaultPrinter();
        var usb = _usb.ListerLecteurs().Count;
        Message = $"Ports : {_ports.Tester(port)} | Imprimante : "
            + $"{(string.IsNullOrWhiteSpace(imprimante) ? "aucune" : imprimante)} | USB : {usb}";
    }

    private void Consolider()
    {
        try
        {
            Message = _maintenance.Integrite(App.CheminBase)
                ? $"Base intègre. Système : {_systeme.Lire().Os}."
                : "La base présente une incohérence.";
        }
        catch (Exception exception) { Message = $"Consolidation impossible : {exception.Message}"; }
    }

    public void Retour() => RetourDemande?.Invoke();
    private void OnPropertyChanged([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}
