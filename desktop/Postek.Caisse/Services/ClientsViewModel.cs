using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Services;

public sealed record ClientLigne(string Code, string Nom, string RaisonSociale, string Telephone,
    string MatriculeFiscal, decimal Solde);

public sealed record ClientEdition(string Code, string Nom, string RaisonSociale = "",
    string Adresse = "", string Ville = "", string CodePostal = "", string Telephone = "",
    string Email = "", string MatriculeFiscal = "", string RegimeTva = "Normal",
    decimal LimiteCredit = 0m);

public sealed class ClientsViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly string _cheminBase;
    private readonly AuditService _audit;
    private readonly Func<bool> _confirmerSuppression;
    private readonly Dispatcher _dispatcher;
    private ClientLigne? _selection;
    private string _message = "";

    public ClientsViewModel(string cheminBase, Action? retour = null, AuditService? audit = null,
        Func<bool>? confirmerSuppression = null)
    {
        _cheminBase = cheminBase;
        _audit = audit ?? new AuditService(cheminBase);
        _confirmerSuppression = confirmerSuppression ?? (() => true);
        _dispatcher = Dispatcher.CurrentDispatcher;
        DatabaseMaintenanceService.TablesVidees += SurTablesVidees;
        Clients = new ObservableCollection<ClientLigne>();
        ChargerCommand = new RelayCommand(Charger);
        NouveauCommand = new RelayCommand(() => EditionDemandee?.Invoke(null));
        ModifierCommand = new RelayCommand(() => EditionDemandee?.Invoke(ClientSelectionne), () => ClientSelectionne is not null);
        SupprimerCommand = new RelayCommand(Supprimer, () => ClientSelectionne is not null);
        RetourCommand = new RelayCommand(() => { retour?.Invoke(); RetourDemandee?.Invoke(); });
        Charger();
    }

    public ObservableCollection<ClientLigne> Clients { get; }
    public ClientLigne? ClientSelectionne { get => _selection; set { _selection = value; OnPropertyChanged(); ActualiserCommandes(); } }
    public string Message { get => _message; private set { _message = value; OnPropertyChanged(); } }
    public ICommand ChargerCommand { get; }
    public ICommand NouveauCommand { get; }
    public ICommand ModifierCommand { get; }
    public ICommand SupprimerCommand { get; }
    public ICommand RetourCommand { get; }
    public event Action<ClientLigne?>? EditionDemandee;
    public event Action? RetourDemandee;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void Charger()
    {
        Clients.Clear();
        using var cnx = Ouvrir();
        using var cmd = cnx.CreateCommand();
        cmd.CommandText = "SELECT code, nom, COALESCE(raison_sociale,''), COALESCE(telephone,''), COALESCE(matricule_fiscal,''), solde FROM Clients ORDER BY nom";
        using var r = cmd.ExecuteReader();
        while (r.Read()) Clients.Add(new ClientLigne(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), Decimal(r.GetString(5))));
        Message = $"{Clients.Count} client(s) chargé(s).";
    }

    public void Creer(ClientEdition client)
    {
        Valider(client);
        using var cnx = Ouvrir();
        using var cmd = cnx.CreateCommand();
        cmd.CommandText = @"INSERT INTO Clients(code,nom,raison_sociale,adresse,ville,code_postal,telephone,email,matricule_fiscal,regime_tva,limite_credit)
                            VALUES($code,$nom,$raison,$adresse,$ville,$postal,$telephone,$email,$mf,$regime,$limite)";
        Ajouter(cmd, client); cmd.ExecuteNonQuery();
        _audit.EnregistrerAudit("CreationClient", $"Code={client.Code}, Nom={client.Nom}");
        Charger(); Message = "Client créé.";
    }

    public void Modifier(ClientLigne original, ClientEdition client)
    {
        Valider(client);
        using var cnx = Ouvrir();
        using var cmd = cnx.CreateCommand();
        cmd.CommandText = @"UPDATE Clients SET code=$code,nom=$nom,raison_sociale=$raison,adresse=$adresse,ville=$ville,code_postal=$postal,
                            telephone=$telephone,email=$email,matricule_fiscal=$mf,regime_tva=$regime,limite_credit=$limite WHERE code=$ancien";
        Ajouter(cmd, client); cmd.Parameters.AddWithValue("$ancien", original.Code); cmd.ExecuteNonQuery();
        _audit.EnregistrerAudit("ModificationClient", $"Code={client.Code}, Nom={client.Nom}");
        Charger(); Message = "Client modifié.";
    }

    public void Supprimer()
    {
        if (ClientSelectionne is null || !_confirmerSuppression()) return;
        var client = ClientSelectionne;
        using var cnx = Ouvrir(); using var cmd = cnx.CreateCommand();
        cmd.CommandText = "DELETE FROM Clients WHERE code=$code"; cmd.Parameters.AddWithValue("$code", client.Code); cmd.ExecuteNonQuery();
        _audit.EnregistrerAudit("SuppressionClient", $"Code={client.Code}, Nom={client.Nom}");
        Charger(); Message = "Client supprimé.";
    }

    private SqliteConnection Ouvrir() { var c = new SqliteConnection($"Data Source={_cheminBase}"); c.Open(); return c; }
    private static decimal Decimal(string value) => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
    private static void Valider(ClientEdition c) { if (string.IsNullOrWhiteSpace(c.Code) || string.IsNullOrWhiteSpace(c.Nom)) throw new ArgumentException("Code et nom obligatoires."); if (c.LimiteCredit < 0) throw new ArgumentException("La limite de crédit ne peut pas être négative."); }
    private static void Ajouter(SqliteCommand cmd, ClientEdition c)
    {
        cmd.Parameters.AddWithValue("$code", c.Code.Trim()); cmd.Parameters.AddWithValue("$nom", c.Nom.Trim()); cmd.Parameters.AddWithValue("$raison", c.RaisonSociale.Trim());
        cmd.Parameters.AddWithValue("$adresse", c.Adresse.Trim()); cmd.Parameters.AddWithValue("$ville", c.Ville.Trim()); cmd.Parameters.AddWithValue("$postal", c.CodePostal.Trim());
        cmd.Parameters.AddWithValue("$telephone", c.Telephone.Trim()); cmd.Parameters.AddWithValue("$email", c.Email.Trim()); cmd.Parameters.AddWithValue("$mf", c.MatriculeFiscal.Trim());
        cmd.Parameters.AddWithValue("$regime", c.RegimeTva.Trim()); cmd.Parameters.AddWithValue("$limite", c.LimiteCredit.ToString("0.000", CultureInfo.InvariantCulture));
    }
    private void ActualiserCommandes() { (ModifierCommand as RelayCommand)?.RaiseCanExecuteChanged(); (SupprimerCommand as RelayCommand)?.RaiseCanExecuteChanged(); }

    private void SurTablesVidees()
    {
        // La RAZ peut être déclenchée depuis un thread de fond : marshaler
        // le rechargement sur le thread du ViewModel sans jamais bloquer
        // l'émetteur (BeginInvoke, pas Invoke).
        if (_dispatcher.CheckAccess()) Charger();
        else _dispatcher.BeginInvoke(Charger);
    }

    public void Dispose() => DatabaseMaintenanceService.TablesVidees -= SurTablesVidees;

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}