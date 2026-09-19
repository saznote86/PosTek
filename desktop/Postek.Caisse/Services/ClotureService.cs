using Postec.Caisse.Caisse;
using System.Globalization;

namespace Postec.Caisse.Services;

public sealed class ClotureService
{
    private readonly BaseDonnees _bdd;
    private readonly UtilisateurSession _utilisateur;

    public ClotureService(BaseDonnees bdd, UtilisateurSession utilisateur)
    {
        _bdd = bdd;
        _utilisateur = utilisateur;
    }

    public RecapZ LireRecapitulatif() => _bdd.RecapZCourant();

    public static decimal CalculerEcart(decimal fondsInitial, decimal fondsFinal, decimal totalEspeces) =>
        decimal.Round(fondsFinal - fondsInitial - totalEspeces, 3, MidpointRounding.AwayFromZero);

    public int GetDernierNumeroZ() => _bdd.DernierNumeroZ();

    public RapportZ GenererCloture(RapportZ rapport)
    {
        var session = _bdd.SessionCaisseOuverte()
            ?? throw new InvalidOperationException("La caisse n'est pas ouverte.");
        if (rapport.FondsFinal < 0m)
            throw new ArgumentOutOfRangeException(nameof(rapport.FondsFinal), "Le fonds final ne peut pas être négatif.");
        if (string.IsNullOrWhiteSpace(rapport.SignatureCaissier))
            throw new InvalidOperationException("La signature du caissier est obligatoire.");

        var recap = LireRecapitulatif();
        if (recap.NbTickets == 0)
            throw new InvalidOperationException("Aucun ticket à clôturer.");

        rapport.NumeroZ = recap.Numero;
        rapport.DateCloture = DateTime.Now;
        rapport.HeureOuverture = session.OuverteLe;
        rapport.UtilisateurId = _utilisateur.Id.ToString(CultureInfo.InvariantCulture);
        rapport.FondsInitial = session.FondsInitial;
        rapport.TotalVentesTTC = recap.TotalTtc;
        rapport.TotalTva = recap.TotalTva;
        rapport.TotalEspeces = TotalMode(recap, ModeReglement.Especes);
        rapport.TotalCB = TotalMode(recap, ModeReglement.CarteBancaire);
        rapport.TotalCheque = TotalMode(recap, ModeReglement.Cheque);
        rapport.TotalTicketResto = TotalMode(recap, ModeReglement.TicketRestaurant);
        rapport.TotalAvoir = TotalMode(recap, ModeReglement.Avoir);
        rapport.MonnaieRendue = recap.MonnaieRendue;
        rapport.EcartCaisse = CalculerEcart(rapport.FondsInitial, rapport.FondsFinal, rapport.TotalEspeces);

        _bdd.CloturerZ(recap);
        _bdd.EnregistrerRapportZ(rapport, session, _utilisateur);
        return rapport;
    }

    public void Effectuer(RecapZ recap) => _bdd.CloturerZ(recap);

    public void ImprimerRapportZ(RapportZ rapport, RecapZ recap)
    {
        var donnees = new DonneesRapportZ
        {
            EnTete = App.BrandCourant.Nom,
            Numero = rapport.NumeroZ,
            DateHeure = rapport.DateCloture,
            NbTickets = recap.NbTickets,
            TotalTtc = rapport.TotalVentesTTC,
            TotalHt = recap.TotalHt,
            TotalTva = recap.TotalTva,
            Caisse = rapport.CaisseId,
            Caissier = _utilisateur.NomAffiche,
            HeureOuverture = rapport.HeureOuverture,
            HeureFermeture = rapport.DateCloture.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("fr-FR")),
            FondsInitial = rapport.FondsInitial,
            TotalEspeces = rapport.TotalEspeces,
            FondsFinal = rapport.FondsFinal,
            EcartCaisse = rapport.EcartCaisse,
            SignatureCaissier = rapport.SignatureCaissier,
            RecapTva = recap.LignesTva.Select(l => new LigneRecapTvaImpression(l.TauxTva * 100m, l.TotalHt, l.TotalTva)).ToList(),
            Reglements = recap.ReglementsParMode.Select(l => new LigneReglementImpression(ModesReglement.LibelleTicket(l.Mode), l.Total)).ToList(),
            MonnaieRendue = rapport.MonnaieRendue,
            Pied = $"Clôturé par {_utilisateur.NomAffiche} le {rapport.DateCloture:dd/MM/yyyy} à {rapport.DateCloture:HH:mm}",
        };
        var resultat = ImprimanteBrute.Envoyer(TicketEscPos.GenererRapportZ(donnees), App.ThemeCourant.Imprimante);
        if (!resultat.Succes)
            throw new InvalidOperationException(resultat.Erreur ?? "Impression du rapport Z impossible.");
    }

    private static decimal TotalMode(RecapZ recap, ModeReglement mode) =>
        recap.ReglementsParMode.FirstOrDefault(l => l.Mode == mode)?.Total ?? 0m;
}
