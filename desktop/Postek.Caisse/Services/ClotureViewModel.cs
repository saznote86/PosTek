using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Services;

public sealed class ClotureViewModel : INotifyPropertyChanged
{
    private readonly ClotureService _service;
    private readonly UtilisateurSession _utilisateur;
    private string _fondsFinalTexte = "";
    private decimal _fondsFinal;
    private bool _fondsFinalValide;
    private string _signatureCaissier = "";
    private string _message = "";

    public ClotureViewModel(BaseDonnees bdd, UtilisateurSession utilisateur)
    {
        _utilisateur = utilisateur;
        _service = new ClotureService(bdd, utilisateur);
        Session = bdd.SessionCaisseOuverte();
        RecapZ = _service.LireRecapitulatif();
        CalculerEcartCommand = new RelayCommand(CalculerEcart);
        ValiderClotureCommand = new RelayCommand(ValiderCloture);
        AnnulerClotureCommand = new RelayCommand(() => AnnulerDemandee?.Invoke());
        EffacerSignatureCommand = new RelayCommand(() => SignatureCaissier = "");
        ToucheFondsCommand = new RelayCommandParametre(param => AppuyerFonds(param?.ToString() ?? ""));
    }

    public RecapZ RecapZ { get; }
    public string Caissier => _utilisateur.NomAffiche;
    public string FondsInitial => (Session?.FondsInitial ?? 0m).ToString("0.000", CultureInfo.GetCultureInfo("fr-FR"));
    public SessionCaisse? Session { get; }
    public string FondsFinalTexte
    {
        get => _fondsFinalTexte;
        set
        {
            _fondsFinalTexte = value;
            if (decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var montant))
            {
                _fondsFinal = decimal.Round(montant, 3, MidpointRounding.AwayFromZero);
                _fondsFinalValide = _fondsFinal >= 0m;
            }
            else _fondsFinalValide = false;
            CalculerEcart();
            OnPropertyChanged();
        }
    }

    public decimal FondsFinal => _fondsFinal;
    public decimal EcartCaisse { get; private set; }
    public string EcartLibelle => EcartCaisse == 0m ? "Caisse juste" :
        EcartCaisse > 0m ? $"Excédent : +{EcartCaisse:0.000} DT" : $"Déficit : {EcartCaisse:0.000} DT";
    public Brush EcartCouleur => EcartCaisse == 0m ? Brushes.ForestGreen : EcartCaisse > 0m ? Brushes.DarkOrange : Brushes.Firebrick;
    public string SignatureCaissier
    {
        get => _signatureCaissier;
        set { _signatureCaissier = value; OnPropertyChanged(); }
    }
    public string Message { get => _message; private set { _message = value; OnPropertyChanged(); } }
    public ICommand CalculerEcartCommand { get; }
    public ICommand ValiderClotureCommand { get; }
    public ICommand AnnulerClotureCommand { get; }
    public ICommand EffacerSignatureCommand { get; }
    public ICommand ToucheFondsCommand { get; }
    public Func<bool>? Confirmation { get; set; }
    public event Action<RapportZ>? ClotureReussie;
    public event Action? AnnulerDemandee;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void CalculerEcart()
    {
        EcartCaisse = Session is null ? 0m : ClotureService.CalculerEcart(Session.FondsInitial, FondsFinal, TotalEspeces);
        OnPropertyChanged(nameof(FondsFinal));
        OnPropertyChanged(nameof(EcartCaisse));
        OnPropertyChanged(nameof(EcartLibelle));
        OnPropertyChanged(nameof(EcartCouleur));
    }

    private decimal TotalEspeces => RecapZ.ReglementsParMode.FirstOrDefault(m => m.Mode == ModeReglement.Especes)?.Total ?? 0m;

    private void ValiderCloture()
    {
        Message = "";
        if (Session is null || !Session.EstOuverte) { Message = "La caisse n'est pas ouverte."; return; }
        if (!_fondsFinalValide || string.IsNullOrWhiteSpace(FondsFinalTexte)) { Message = "Saisissez un fonds final valide."; return; }
        if (string.IsNullOrWhiteSpace(SignatureCaissier)) { Message = "La signature du caissier est obligatoire."; return; }
        if (Confirmation is not null && !Confirmation()) return;

        try
        {
            var rapport = _service.GenererCloture(new RapportZ
            {
                FondsFinal = FondsFinal,
                SignatureCaissier = SignatureCaissier.Trim(),
                CaisseId = "1",
            });
            _service.ImprimerRapportZ(rapport, RecapZ);
            Message = $"Clôture réussie : Z n°{rapport.NumeroZ}.";
            ClotureReussie?.Invoke(rapport);
        }
        catch (Exception ex) { Message = ex.Message; }
    }

    private void AppuyerFonds(string touche)
    {
        if (touche == "Effacer") FondsFinalTexte = "";
        else if (touche == "Retour") FondsFinalTexte = FondsFinalTexte.Length == 0 ? "" : FondsFinalTexte[..^1];
        else if (touche is "," or ".")
        {
            if (!FondsFinalTexte.Contains(',') && !FondsFinalTexte.Contains('.')) FondsFinalTexte += ",";
        }
        else if (touche.Length == 1 && char.IsDigit(touche[0])) FondsFinalTexte += touche;
    }

    private void OnPropertyChanged([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}
