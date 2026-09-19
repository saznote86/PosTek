using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Services;

public sealed class GestionStubViewModel : INotifyPropertyChanged
{
    private string _message;

    public GestionStubViewModel(string titre, params string[] colonnes)
    {
        Titre = titre;
        Colonnes = colonnes;
        Lignes = new ObservableCollection<GestionLigne>();
        _message = $"Gestion {titre.ToLowerInvariant()} prête.";
        NouveauCommand = new RelayCommand(() => Message = $"Nouvelle ligne {titre.ToLowerInvariant()}.");
        ModifierCommand = new RelayCommand(() => Message = $"Sélection {titre.ToLowerInvariant()} modifiée.");
        SupprimerCommand = new RelayCommand(() => Message = $"Sélection {titre.ToLowerInvariant()} supprimée.");
        RetourCommand = new RelayCommand(() => RetourDemandee?.Invoke());
    }

    public string Titre { get; }
    public IReadOnlyList<string> Colonnes { get; }
    public ObservableCollection<GestionLigne> Lignes { get; }
    public string Message { get => _message; private set { _message = value; OnPropertyChanged(); } }
    public ICommand NouveauCommand { get; }
    public ICommand ModifierCommand { get; }
    public ICommand SupprimerCommand { get; }
    public ICommand RetourCommand { get; }
    public event Action? RetourDemandee;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}

public sealed record GestionLigne(string Code, string Libelle, string Valeur);
