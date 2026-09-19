using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Services;

public sealed class ReindexerViewModel : INotifyPropertyChanged
{
    private readonly DatabaseMaintenanceService _maintenance;
    private string _chemin;
    private string _message = "Prêt à réindexer.";
    private bool _estEnCours;

    public ReindexerViewModel(DatabaseMaintenanceService maintenance, string chemin)
    {
        _maintenance = maintenance;
        _chemin = chemin;
        ReindexerCommand = new RelayCommand(Reindexer);
        FermerCommand = new RelayCommand(Fermer);
    }

    public string Chemin { get => _chemin; set { _chemin = value; OnPropertyChanged(); } }
    public string Message { get => _message; private set { _message = value; OnPropertyChanged(); } }
    public bool EstEnCours { get => _estEnCours; private set { _estEnCours = value; OnPropertyChanged(); } }
    public ICommand ReindexerCommand { get; }
    public ICommand FermerCommand { get; }
    public event Action? FermerDemandee;
    public event PropertyChangedEventHandler? PropertyChanged;

    private async void Reindexer()
    {
        if (EstEnCours) return;
        EstEnCours = true;
        Message = "Réindexation en cours...";
        try
        {
            await _maintenance.RebuildIndexesAsync(Chemin);
            Message = "Réindexation terminée avec succès.";
        }
        catch (Exception ex) { Message = $"Réindexation impossible : {ex.Message}"; }
        finally { EstEnCours = false; }
    }

    public void Fermer() => FermerDemandee?.Invoke();
    private void OnPropertyChanged([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}
