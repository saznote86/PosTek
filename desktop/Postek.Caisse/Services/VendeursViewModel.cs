using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Services;

public sealed record VendeurLigne(long Id, string Code, string Nom, string Prenom, string Role, bool Actif);
public sealed record VendeurEdition(string Code, string Nom, string Prenom, string MotDePasse, string Role, bool Actif);

public sealed class VendeursViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly string _cheminBase;
    private readonly AuditService _audit;
    private readonly Func<bool> _confirmerSuppression;
    private readonly Dispatcher _dispatcher;
    private VendeurLigne? _selection;
    private string _message = "";

    public VendeursViewModel(string cheminBase, Action? retour = null, AuditService? audit = null, Func<bool>? confirmerSuppression = null)
    {
        _cheminBase = cheminBase; _audit = audit ?? new AuditService(cheminBase); _confirmerSuppression = confirmerSuppression ?? (() => true);
        _dispatcher = Dispatcher.CurrentDispatcher;
        DatabaseMaintenanceService.TablesVidees += SurTablesVidees;
        Vendeurs = new ObservableCollection<VendeurLigne>();
        ChargerCommand = new RelayCommand(Charger); NouveauCommand = new RelayCommand(() => EditionDemandee?.Invoke(null));
        ModifierCommand = new RelayCommand(() => EditionDemandee?.Invoke(VendeurSelectionne), () => VendeurSelectionne is not null);
        SupprimerCommand = new RelayCommand(Supprimer, () => VendeurSelectionne is not null);
        RetourCommand = new RelayCommand(() => { retour?.Invoke(); RetourDemandee?.Invoke(); }); Charger();
    }
    public ObservableCollection<VendeurLigne> Vendeurs { get; }
    public VendeurLigne? VendeurSelectionne { get => _selection; set { _selection = value; OnPropertyChanged(); Actualiser(); } }
    public string Message { get => _message; private set { _message = value; OnPropertyChanged(); } }
    public ICommand ChargerCommand { get; } public ICommand NouveauCommand { get; } public ICommand ModifierCommand { get; }
    public ICommand SupprimerCommand { get; } public ICommand RetourCommand { get; }
    public event Action<VendeurLigne?>? EditionDemandee; public event Action? RetourDemandee; public event PropertyChangedEventHandler? PropertyChanged;

    public void Charger()
    {
        Vendeurs.Clear(); using var c = Ouvrir(); using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT id, identifiant, nom_affiche, '', role, actif FROM Utilisateurs ORDER BY nom_affiche";
        using var r = cmd.ExecuteReader(); while (r.Read()) Vendeurs.Add(new VendeurLigne(r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetInt64(5) != 0));
        Message = $"{Vendeurs.Count} vendeur(s) chargé(s).";
    }
    public void Creer(VendeurEdition v)
    {
        Valider(v, true); using var c = Ouvrir(); using var cmd = c.CreateCommand();
        cmd.CommandText = "INSERT INTO Utilisateurs(identifiant,nom_affiche,role,mot_de_passe,cree_le,actif) VALUES($code,$nom,$role,$mdp,$date,$actif)";
        Ajouter(cmd, v); cmd.ExecuteNonQuery(); _audit.EnregistrerAudit("CreationVendeur", $"Code={v.Code}, Nom={v.Nom}, Role={v.Role}"); Charger(); Message = "Vendeur créé.";
    }
    public void Modifier(VendeurLigne original, VendeurEdition v)
    {
        Valider(v, false); using var c = Ouvrir(); using var cmd = c.CreateCommand();
        cmd.CommandText = string.IsNullOrWhiteSpace(v.MotDePasse)
            ? "UPDATE Utilisateurs SET identifiant=$code,nom_affiche=$nom,role=$role,actif=$actif WHERE id=$id"
            : "UPDATE Utilisateurs SET identifiant=$code,nom_affiche=$nom,role=$role,actif=$actif,mot_de_passe=$mdp,DoitChangerMotDePasse=0 WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", original.Id); cmd.Parameters.AddWithValue("$code", v.Code.Trim()); cmd.Parameters.AddWithValue("$nom", (v.Prenom + " " + v.Nom).Trim()); cmd.Parameters.AddWithValue("$role", v.Role); cmd.Parameters.AddWithValue("$actif", v.Actif ? 1 : 0);
        if (!string.IsNullOrWhiteSpace(v.MotDePasse)) cmd.Parameters.AddWithValue("$mdp", AuthentificationService.HacherMotDePasse(v.MotDePasse));
        cmd.ExecuteNonQuery(); _audit.EnregistrerAudit("ModificationVendeur", $"Code={v.Code}, Nom={v.Nom}, Role={v.Role}"); Charger(); Message = "Vendeur modifié.";
    }
    public void Supprimer()
    {
        if (VendeurSelectionne is null || !_confirmerSuppression()) return; var v = VendeurSelectionne; using var c = Ouvrir(); using var cmd = c.CreateCommand(); cmd.CommandText = "UPDATE Utilisateurs SET actif=0 WHERE id=$id"; cmd.Parameters.AddWithValue("$id", v.Id); cmd.ExecuteNonQuery(); _audit.EnregistrerAudit("SuppressionVendeur", $"Code={v.Code}, Nom={v.Nom}"); Charger(); Message = "Vendeur désactivé.";
    }
    private SqliteConnection Ouvrir() { var c = new SqliteConnection($"Data Source={_cheminBase}"); c.Open(); return c; }
    private static void Valider(VendeurEdition v, bool nouveau) { if (string.IsNullOrWhiteSpace(v.Code) || string.IsNullOrWhiteSpace(v.Nom)) throw new ArgumentException("Code et nom obligatoires."); if (nouveau && string.IsNullOrWhiteSpace(v.MotDePasse)) throw new ArgumentException("Mot de passe obligatoire."); if (v.Role is not ("Administrateur" or "Caissier")) throw new ArgumentException("Rôle invalide."); }
    private static void Ajouter(SqliteCommand cmd, VendeurEdition v) { cmd.Parameters.AddWithValue("$code", v.Code.Trim()); cmd.Parameters.AddWithValue("$nom", (v.Prenom + " " + v.Nom).Trim()); cmd.Parameters.AddWithValue("$role", v.Role); cmd.Parameters.AddWithValue("$mdp", AuthentificationService.HacherMotDePasse(v.MotDePasse)); cmd.Parameters.AddWithValue("$date", DateTime.UtcNow.ToString("o")); cmd.Parameters.AddWithValue("$actif", v.Actif ? 1 : 0); }
    private void Actualiser() { (ModifierCommand as RelayCommand)?.RaiseCanExecuteChanged(); (SupprimerCommand as RelayCommand)?.RaiseCanExecuteChanged(); }

    private void SurTablesVidees()
    {
        // La RAZ peut être déclenchée depuis un thread de fond : marshaler
        // le rechargement sur le thread du ViewModel sans jamais bloquer
        // l'émetteur (BeginInvoke, pas Invoke).
        if (_dispatcher.CheckAccess()) Charger();
        else _dispatcher.BeginInvoke(Charger);
    }

    public void Dispose() => DatabaseMaintenanceService.TablesVidees -= SurTablesVidees;

    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}