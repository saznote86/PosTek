using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Services;

public sealed class MotsDePasseViewModel : INotifyPropertyChanged
{
    private readonly UsbKeyService _usb;
    private string _lecteur = "";
    private string _secret = "";
    private string _message = "";

    public MotsDePasseViewModel(UsbKeyService usb)
    {
        _usb = usb;
        EcrireCommand = new RelayCommand(Ecrire);
        EffacerCommand = new RelayCommand(Effacer);
        ValiderCommand = new RelayCommand(() => FermerDemandee?.Invoke());
        ActualiserLecteurs();
    }

    public ObservableCollection<string> Lecteurs { get; } = new();
    public string Lecteur { get => _lecteur; set { _lecteur = value; OnPropertyChanged(); } }
    public string Secret { get => _secret; set { _secret = value; OnPropertyChanged(); } }
    public string Message { get => _message; private set { _message = value; OnPropertyChanged(); } }
    public ICommand EcrireCommand { get; }
    public ICommand EffacerCommand { get; }
    public ICommand ValiderCommand { get; }
    public event Action? FermerDemandee;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void ActualiserLecteurs()
    {
        try
        {
            foreach (var lecteur in _usb.GetUsbDrives()) Lecteurs.Add(lecteur);
            if (Lecteurs.Count > 0) Lecteur = Lecteurs[0];
        }
        catch (Exception ex) { Message = $"Lecteurs USB indisponibles : {ex.Message}"; }
    }

    private void Ecrire()
    {
        try
        {
            _usb.WritePasswordToUsb(Lecteur, string.IsNullOrEmpty(Secret) ? "POSTEC" : Secret);
            Message = "Mot de passe écrit sur la clé USB.";
        }
        catch (Exception ex) { Message = $"Ecriture USB impossible : {ex.Message}"; }
    }

    private void Effacer()
    {
        try { _usb.DeletePasswordFromUsb(Lecteur); Message = "Mot de passe USB effacé."; }
        catch (Exception ex) { Message = $"Effacement USB impossible : {ex.Message}"; }
    }

    private void OnPropertyChanged([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}
