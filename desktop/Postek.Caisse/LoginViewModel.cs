using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Postec.Caisse.Caisse;
using Postec.Caisse.Services;

namespace Postec.Caisse;

/// <summary>ViewModel du login : aucune fenêtre n'est conservée après sa fermeture.</summary>
public sealed class LoginViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly BaseDonnees _bdd;
    private readonly AuditService _audit;
    private bool _dispose;
    private string _utilisateurSelectionne = "";
    private string _motDePasse = "";
    private string _messageErreur = "";
    private bool _estEnCours;

    public LoginViewModel(BaseDonnees bdd, AuditService? audit = null)
    {
        _bdd = bdd ?? throw new ArgumentNullException(nameof(bdd));
        _audit = audit ?? new AuditService(bdd.CheminFichier, () => null);
        ValiderCommand = new RelayCommand(() => _ = Valider(), () => !EstEnCours);
        AnnulerCommand = new RelayCommand(Annuler, () => !EstEnCours);
        ChiffreCommand = new RelayCommandParametre(param => AjouterChiffre(param?.ToString() ?? ""), _ => !EstEnCours);
        EffacerCommand = new RelayCommand(Effacer, () => !EstEnCours);
        SeConnecterCommand = new RelayCommandParametre(param => SeConnecter(param?.ToString() ?? ""));
        NumberPressedCommand = ChiffreCommand;
        ChargerUtilisateurs();
    }

    public ObservableCollection<string> Utilisateurs { get; } = new();
    public ObservableCollection<HistoriqueConnexion> UtilisateursRecents { get; } = new();
    public string UtilisateurSelectionne { get => _utilisateurSelectionne; set { if (_utilisateurSelectionne == value) return; _utilisateurSelectionne = value ?? ""; OnPropertyChanged(); } }
    public string NomUtilisateurSaisi { get => UtilisateurSelectionne; set => UtilisateurSelectionne = value; }
    public string MotDePasse { get => _motDePasse; set { if (_motDePasse == value) return; _motDePasse = value ?? ""; MessageErreur = ""; OnPropertyChanged(); } }
    public string MessageErreur { get => _messageErreur; private set { if (_messageErreur == value) return; _messageErreur = value; OnPropertyChanged(); OnPropertyChanged(nameof(Erreur)); } }
    public string? Erreur => string.IsNullOrEmpty(MessageErreur) ? null : MessageErreur;
    public bool EstEnCours { get => _estEnCours; private set { if (_estEnCours == value) return; _estEnCours = value; OnPropertyChanged(); RaiseCommands(); } }
    public UtilisateurSession? Session { get; private set; }
    public UtilisateurSession? UtilisateurCourant => Session;

    public ICommand ValiderCommand { get; }
    public ICommand AnnulerCommand { get; }
    public ICommand ChiffreCommand { get; }
    public ICommand EffacerCommand { get; }
    public ICommand SeConnecterCommand { get; }
    public ICommand NumberPressedCommand { get; }
    public event Action? ConnexionReussie;
    public event Action? AnnulationDemandee;
    public event Action<string>? ToucheNumeriqueDemandee;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void ChargerUtilisateurs()
    {
        ThrowIfDisposed();
        Utilisateurs.Clear(); UtilisateursRecents.Clear();
        foreach (var utilisateur in _bdd.ListerUtilisateurs().Select(x => x.Identifiant).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x)) Utilisateurs.Add(utilisateur);
        foreach (var recent in _bdd.ListerHistoriqueConnexions()) UtilisateursRecents.Add(recent);
        if (Utilisateurs.Count == 1) UtilisateurSelectionne = Utilisateurs[0];
    }

    public void AjouterChiffre(string chiffre)
    {
        ThrowIfDisposed();
        if (string.IsNullOrEmpty(chiffre)) return;
        if (chiffre is "." or "," || chiffre.Length == 1 && char.IsDigit(chiffre[0])) MotDePasse += chiffre;
        ToucheNumeriqueDemandee?.Invoke(chiffre);
        MessageErreur = "";
    }

    public void Effacer() { ThrowIfDisposed(); MotDePasse = ""; MessageErreur = ""; }
    public bool SeConnecter(string motDePasse) { MotDePasse = motDePasse ?? ""; return Valider(); }

    public bool Valider()
    {
        ThrowIfDisposed(); MessageErreur = "";
        if (string.IsNullOrWhiteSpace(UtilisateurSelectionne) || string.IsNullOrEmpty(MotDePasse)) { MessageErreur = "Saisissez un identifiant et un mot de passe."; return false; }
        EstEnCours = true;
        try
        {
            var identifiant = UtilisateurSelectionne.Trim();
            Session = _bdd.Authentifier(identifiant, MotDePasse);
            if (Session is null)
            {
                _audit.EnregistrerAudit(identifiant, "Tentative de connexion échouée", $"DateHeure={DateTime.UtcNow:O}");
                MotDePasse = "";
                MessageErreur = "Identifiant ou mot de passe incorrect.";
                return false;
            }
            _bdd.EnregistrerConnexionReussie(identifiant);
            _audit.EnregistrerAudit(identifiant, "Connexion réussie", $"DateHeure={DateTime.UtcNow:O}");
            ChargerUtilisateurs(); ConnexionReussie?.Invoke(); return true;
        }
        finally { EstEnCours = false; }
    }

    public void Annuler() { if (!_dispose) AnnulationDemandee?.Invoke(); }
    private void RaiseCommands() { (ValiderCommand as RelayCommand)?.RaiseCanExecuteChanged(); (AnnulerCommand as RelayCommand)?.RaiseCanExecuteChanged(); (ChiffreCommand as RelayCommandParametre)?.RaiseCanExecuteChanged(); (EffacerCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
    private void ThrowIfDisposed() { if (_dispose) throw new ObjectDisposedException(nameof(LoginViewModel)); }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    public void Dispose() { if (_dispose) return; _dispose = true; ConnexionReussie = null; AnnulationDemandee = null; ToucheNumeriqueDemandee = null; Utilisateurs.Clear(); UtilisateursRecents.Clear(); }
}