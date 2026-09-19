using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Services;

public sealed class ChangerMotDePasseViewModel : INotifyPropertyChanged
{
    private readonly BaseDonnees _bdd;
    private readonly UtilisateurSession _utilisateur;
    private string _nouveauMotDePasse = "";
    private string _confirmation = "";
    private string _message = "";

    public ChangerMotDePasseViewModel(BaseDonnees bdd, UtilisateurSession utilisateur)
    {
        _bdd = bdd;
        _utilisateur = utilisateur;
        ValiderCommand = new RelayCommand(Valider);
        AnnulerCommand = new RelayCommand(() => AnnulationDemandee?.Invoke());
    }

    public string NouveauMotDePasse { get => _nouveauMotDePasse; set { _nouveauMotDePasse = value; OnPropertyChanged(); } }
    public string Confirmation { get => _confirmation; set { _confirmation = value; OnPropertyChanged(); } }
    public string Message { get => _message; private set { _message = value; OnPropertyChanged(); } }
    public ICommand ValiderCommand { get; }
    public ICommand AnnulerCommand { get; }
    public event Action? MotDePasseChange;
    public event Action? AnnulationDemandee;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Valider()
    {
        if (NouveauMotDePasse.Length < 6)
        {
            Message = "Le nouveau mot de passe doit contenir au moins 6 caractères.";
            return;
        }
        if (!string.Equals(NouveauMotDePasse, Confirmation, StringComparison.Ordinal))
        {
            Message = "Les mots de passe ne correspondent pas.";
            return;
        }

        _bdd.DefinirMotDePasse(_utilisateur.Id, NouveauMotDePasse);
        _bdd.JournaliserAudit(_utilisateur, "Changement obligatoire du mot de passe");
        MotDePasseChange?.Invoke();
    }

    private void OnPropertyChanged([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}
