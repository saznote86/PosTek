using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Postec.Caisse.Caisse;

public sealed record SessionCaisse(
    long Id,
    long UtilisateurId,
    string OuverteLe,
    decimal FondsInitial,
    string? FermeeLe,
    decimal? FondsFinal,
    decimal? Ecart,
    long? RapportZId,
    string Statut)
{
    public bool EstOuverte => Statut == "Ouverte";
}

public sealed record RapportZCaisse(
    long Id,
    int NumeroZ,
    long SessionId,
    long UtilisateurId,
    string CreeLe,
    decimal TotalEspeces,
    decimal TotalAutres,
    decimal TotalRemises,
    decimal TotalTva,
    decimal FondsInitial,
    decimal FondsFinal,
    decimal Ecart,
    string SignatureCaissier);

public sealed class RapportZ
{
    public int Id { get; set; }
    public DateTime DateCloture { get; set; }
    public string CaisseId { get; set; } = "1";
    public string UtilisateurId { get; set; } = "";
    public decimal FondsInitial { get; set; }
    public decimal FondsFinal { get; set; }
    public decimal EcartCaisse { get; set; }
    public decimal TotalVentesTTC { get; set; }
    public decimal TotalTva { get; set; }
    public decimal TotalEspeces { get; set; }
    public decimal TotalCB { get; set; }
    public decimal TotalCheque { get; set; }
    public decimal TotalTicketResto { get; set; }
    public decimal TotalAvoir { get; set; }
    public decimal MonnaieRendue { get; set; }
    public string SignatureCaissier { get; set; } = "";
    public int NumeroZ { get; set; }
    public string? HeureOuverture { get; set; }
}

/// <summary>Etat observable de l'ouverture de caisse pour l'écran de démarrage.</summary>
public sealed class OuvertureCaisseViewModel : INotifyPropertyChanged
{
    private readonly BaseDonnees _bdd;
    private readonly UtilisateurSession _utilisateur;
    private string _fondsInitialTexte = "0,000";
    private SessionCaisse? _session;

    public OuvertureCaisseViewModel(BaseDonnees bdd, UtilisateurSession utilisateur)
    {
        _bdd = bdd;
        _utilisateur = utilisateur;
        _session = _bdd.SessionCaisseOuverte();
    }

    public string FondsInitialTexte
    {
        get => _fondsInitialTexte;
        set { _fondsInitialTexte = value; OnPropertyChanged(); }
    }

    public SessionCaisse? Session => _session;
    public bool EstOuverte => _session?.EstOuverte == true;
    public string Statut => EstOuverte ? "Ouverte" : "Fermée";
    public string Caissier => _utilisateur.NomAffiche;
    public string Message { get; private set; } = "";

    public bool Ouvrir()
    {
        Message = "";
        if (!decimal.TryParse(FondsInitialTexte.Replace(',', '.'), NumberStyles.Number,
                              CultureInfo.InvariantCulture, out var fonds) || fonds < 0m)
        {
            Message = "Saisissez un fonds initial valide.";
            OnPropertyChanged(nameof(Message));
            return false;
        }

        try
        {
            _session = _bdd.OuvrirSessionCaisse(_utilisateur, decimal.Round(fonds, 3, MidpointRounding.AwayFromZero));
            OnPropertyChanged(nameof(Session));
            OnPropertyChanged(nameof(EstOuverte));
            OnPropertyChanged(nameof(Statut));
            OnPropertyChanged(nameof(Message));
            return true;
        }
        catch (Exception ex)
        {
            Message = ex.Message;
            OnPropertyChanged(nameof(Message));
            return false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? nom = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nom));
}