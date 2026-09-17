using System.IO;
using Postek.Caisse.Caisse;
using Xunit;

namespace Postek.Caisse.Tests;

/// <summary>
/// Tests de persistance : numérotation continue au redémarrage, round-trip
/// ticket, clôture Z (numérotation sans trou, rattachement des tickets).
/// Chaque test utilise une base SQLite temporaire isolée.
/// </summary>
public class BaseDonneesTests : IDisposable
{
    private readonly string _chemin =
        Path.Combine(Path.GetTempPath(), $"postek_test_{Guid.NewGuid():N}.db");

    private readonly BaseDonnees _bdd;

    public BaseDonneesTests()
    {
        _bdd = new BaseDonnees(_chemin);
        _bdd.AjouterArticle(new("P005", "Café express", 1.200m, 0.190m));
        _bdd.AjouterArticle(new("P001", "Baguette tradition", 0.250m, 0.070m));
    }

    public void Dispose()
    {
        _bdd.Dispose();
        try { File.Delete(_chemin); } catch { /* best effort */ }
    }

    private static CaisseService CaisseAvecUnArticle()
    {
        var caisse = new CaisseService();
        caisse.Ajouter(new("P005", "Café express", 1.200m, 0.190m), 2m);
        return caisse;
    }

    // ------------------------------------------------------------------
    // Numérotation persistée
    // ------------------------------------------------------------------
    [Fact]
    public void DernierNumeroTicket_BaseNeuve_RetourneZero()
    {
        Assert.Equal(0, _bdd.DernierNumeroTicket());
    }

    [Fact]
    public void Numerotation_ReprendApresRedemarrage()
    {
        // "Session 1" : deux tickets encaissés.
        var caisse = CaisseAvecUnArticle();
        _bdd.EnregistrerTicket(caisse.NumeroTicket, caisse);
        caisse.Encaisser();
        _bdd.EnregistrerTicket(caisse.NumeroTicket, caisse);
        caisse.Encaisser();

        // "Redémarrage" : nouvelle base ouverte sur le même fichier.
        using var bdd2 = new BaseDonnees(_chemin);
        var caisse2 = new CaisseService();
        caisse2.DefinirNumeroTicket(bdd2.DernierNumeroTicket() + 1);

        Assert.Equal(3, caisse2.NumeroTicket);
    }

    // ------------------------------------------------------------------
    // Round-trip ticket
    // ------------------------------------------------------------------
    [Fact]
    public void TicketEnregistre_QuRoundTrip_Fidele()
    {
        var caisse = new CaisseService();
        caisse.Ajouter(new("P005", "Café express", 1.200m, 0.190m), 2m);
        caisse.Ajouter(new("P001", "Baguette tradition", 0.250m, 0.070m), 1m);
        var numero = caisse.NumeroTicket;

        _bdd.EnregistrerTicket(numero, caisse);

        Assert.Equal(2.650m, _bdd.TotalDunTicket(numero));
        var lignes = _bdd.LignesDunTicket(numero);
        Assert.Equal(2, lignes.Count);
        Assert.Equal("Café express", lignes[0].Designation);
        Assert.Equal(2m, lignes[0].Quantite);
        Assert.Equal(2.400m, lignes[0].TotalLigne);
    }

    // ------------------------------------------------------------------
    // Clôture Z
    // ------------------------------------------------------------------
    [Fact]
    public void RecapZCourant_AgresseLesTicketsNonClotures()
    {
        var caisse = CaisseAvecUnArticle();          // 2,400 TTC à 19 %
        _bdd.EnregistrerTicket(caisse.NumeroTicket, caisse);
        caisse.Encaisser();

        var z = _bdd.RecapZCourant();

        Assert.Equal(1, z.Numero);                   // première clôture
        Assert.Equal(1, z.NbTickets);
        Assert.Equal(2.400m, z.TotalTtc);
        var ligne = Assert.Single(z.LignesTva);
        Assert.Equal(0.190m, ligne.TauxTva);
        Assert.Equal(0.383m, ligne.TotalTva);        // 2,400 × 0,19/1,19 = 0,3832… → 0,383
        Assert.Equal(2.017m, ligne.TotalHt);
    }

    [Fact]
    public void CloturerZ_RattacheLesTickets_EtNumerotationContinue()
    {
        var caisse = CaisseAvecUnArticle();
        _bdd.EnregistrerTicket(caisse.NumeroTicket, caisse);
        caisse.Encaisser();

        var z1 = _bdd.RecapZCourant();
        _bdd.CloturerZ(z1);

        // Période suivante vide : le récap pointe sur le Z suivant.
        var z2 = _bdd.RecapZCourant();
        Assert.Equal(2, z2.Numero);
        Assert.Equal(0, z2.NbTickets);

        // Numérotation non continue refusée.
        var zFausse = z2 with { Numero = 5 };
        Assert.Throws<InvalidOperationException>(() => _bdd.CloturerZ(zFausse));
    }

    [Fact]
    public void CloturerZ_SansTickets_Refuse()
    {
        var z = _bdd.RecapZCourant();   // aucun ticket

        Assert.Throws<InvalidOperationException>(() => _bdd.CloturerZ(z));
    }

    [Fact]
    public void TicketsClotures_NeSontPlusDansLaPeriodeCourante()
    {
        var caisse = CaisseAvecUnArticle();
        _bdd.EnregistrerTicket(caisse.NumeroTicket, caisse);
        caisse.Encaisser();

        _bdd.CloturerZ(_bdd.RecapZCourant());

        var z = _bdd.RecapZCourant();
        Assert.Equal(0, z.NbTickets);
        Assert.Empty(z.LignesTva);
    }

    // ------------------------------------------------------------------
    // Multi-règlement : persistance + récap Z par mode
    // ------------------------------------------------------------------
    [Fact]
    public void Reglements_RoundTrip_Fidele()
    {
        var caisse = CaisseAvecUnArticle();          // 2,400 TTC
        var reglements = new Reglement[]
        {
            new(ModeReglement.Especes, 5.000m),
            new(ModeReglement.CarteBancaire, 0.500m),
        };

        _bdd.EnregistrerTicket(caisse.NumeroTicket, caisse, reglements);

        var relus = _bdd.ReglementsDunTicket(caisse.NumeroTicket);
        Assert.Equal(2, relus.Count);
        Assert.Equal(ModeReglement.Especes, relus[0].Mode);
        Assert.Equal(5.000m, relus[0].Montant);
        Assert.Equal(ModeReglement.CarteBancaire, relus[1].Mode);
        Assert.Equal(0.500m, relus[1].Montant);
    }

    [Fact]
    public void TicketSansReglement_Lu_Vide()
    {
        var caisse = CaisseAvecUnArticle();
        _bdd.EnregistrerTicket(caisse.NumeroTicket, caisse);

        Assert.Empty(_bdd.ReglementsDunTicket(caisse.NumeroTicket));
    }

    [Fact]
    public void RecapZParMode_AgresseLesTicketsNonClotures()
    {
        // Ticket 1 : 2,400 en espèces (reçu 5,000).
        var c1 = CaisseAvecUnArticle();
        _bdd.EnregistrerTicket(c1.NumeroTicket, c1,
            new Reglement[] { new(ModeReglement.Especes, 5.000m) });
        c1.Encaisser();

        // Ticket 2 : 2,400 partagé CB 1,000 + espèces 1,400.
        var c2 = new CaisseService();
        c2.DefinirNumeroTicket(2);
        c2.Ajouter(new("P005", "Café express", 1.200m, 0.190m), 2m);
        _bdd.EnregistrerTicket(c2.NumeroTicket, c2,
            new Reglement[]
            {
                new(ModeReglement.CarteBancaire, 1.000m),
                new(ModeReglement.Especes, 1.400m),
            });
        c2.Encaisser();

        var parMode = _bdd.RecapParModeNonClotures();

        Assert.Equal(2, parMode.Count);
        // SortedDictionary ordonne par enum (Especes < CarteBancaire).
        Assert.Equal(ModeReglement.Especes, parMode[0].Mode);
        Assert.Equal(6.400m, parMode[0].Total);
        Assert.Equal(ModeReglement.CarteBancaire, parMode[1].Mode);
        Assert.Equal(1.000m, parMode[1].Total);
    }

    [Fact]
    public void RecapZCourant_ExposeLesReglementsParMode()
    {
        var caisse = CaisseAvecUnArticle();
        _bdd.EnregistrerTicket(caisse.NumeroTicket, caisse,
            new Reglement[] { new(ModeReglement.TicketRestaurant, 2.400m) });
        caisse.Encaisser();

        var z = _bdd.RecapZCourant();

        var ligne = Assert.Single(z.ReglementsParMode);
        Assert.Equal(ModeReglement.TicketRestaurant, ligne.Mode);
        Assert.Equal(2.400m, ligne.Total);
        Assert.Equal(z.TotalTtc, z.TotalRegle);      // cohérence ventes/règlements
    }

    [Fact]
    public void Reglements_Clotures_NeSontPlusDansLaPeriode()
    {
        var caisse = CaisseAvecUnArticle();
        _bdd.EnregistrerTicket(caisse.NumeroTicket, caisse,
            new Reglement[] { new(ModeReglement.Avoir, 2.400m) });
        caisse.Encaisser();

        _bdd.CloturerZ(_bdd.RecapZCourant());

        Assert.Empty(_bdd.RecapParModeNonClotures());
    }

    // ------------------------------------------------------------------
    // Encaissement mixte espèces + CB avec rendu monnaie (bout en bout :
    // cœur métier → persistance → impression, fidèle à OuvrirPaiement).
    // ------------------------------------------------------------------
    [Fact]
    public void EncaissementMixte_EspecesEtCb_AvecRendu_PersisteEtImprime()
    {
        // ----- Parcours UI : café ×2 (2,400 à 19 %) + croissant (0,600 à 13 %).
        var caisse = new CaisseService();
        caisse.Ajouter(new("P005", "Café express", 1.200m, 0.190m), 2m);
        caisse.Ajouter(new("P002", "Croissant", 0.600m, 0.130m), 1m);
        Assert.Equal(3.000m, caisse.TotalTtc);

        // CB d'abord : 1,000 (plafonné au reste à payer, ne rend jamais monnaie).
        caisse.Regler(ModeReglement.CarteBancaire, 1.000m);
        Assert.Equal(2.000m, caisse.ResteAPayer);

        // Espèces ensuite : 2,500 reçues pour 2,000 dus → trop perçu 0,500.
        caisse.Regler(ModeReglement.Especes, 2.500m);
        Assert.True(caisse.EstSolde);
        Assert.Equal(-0.500m, caisse.ResteAPayer);
        Assert.Equal(0.500m, caisse.MonnaieARendre);   // plafonné aux espèces reçues

        // ----- Validation : persistance AVANT vidage (ordre de OuvrirPaiement).
        var numero = caisse.NumeroTicket;
        var reglements = caisse.Reglements.ToList();
        _bdd.EnregistrerTicket(numero, caisse, reglements);
        var total = caisse.Encaisser();

        Assert.Equal(3.000m, total);
        Assert.Equal(0, caisse.Reglements.Count);      // vidée pour le ticket suivant

        // ----- En base : total, deux règlements distincts espèces + CB.
        Assert.Equal(3.000m, _bdd.TotalDunTicket(numero));
        var relus = _bdd.ReglementsDunTicket(numero);
        Assert.Equal(2, relus.Count);
        Assert.Equal(ModeReglement.CarteBancaire, relus[0].Mode);
        Assert.Equal(1.000m, relus[0].Montant);
        Assert.Equal(ModeReglement.Especes, relus[1].Mode);
        Assert.Equal(2.500m, relus[1].Montant);

        // ----- Ticket imprimé : les deux modes + la ligne RENDU.
        var donnees = new DonneesTicket
        {
            NumeroTicket = numero,
            TotalTtc = total,
            Lignes = _bdd.LignesDunTicket(numero)
                .Select(l => new LigneTicketImpression(
                    l.Designation, l.Quantite, l.PrixTtc, l.TotalLigne))
                .ToList(),
            RecapTva = _bdd.RecapTvaDunTicket(numero)
                .Select(r => new LigneRecapTvaImpression(r.Taux * 100, r.Ht, r.Tva))
                .ToList(),
            Reglements = relus.Select(r => new LigneReglementImpression(
                    ModesReglement.LibelleTicket(r.Mode), r.Montant))
                .ToList(),
            MonnaieRendue = 0.500m,                        // trop perçu du ticket validé
        };
        var texte = TicketEscPosTestRunner.DecodePublique(
            TicketEscPos.Generer(donnees));

        Assert.Contains("CB", texte);
        Assert.Contains("1,000", texte);
        Assert.Contains("ESPECES", texte);
        Assert.Contains("2,500", texte);
        Assert.Contains("RENDU", texte);
        Assert.Contains("0,500", texte);

        // ----- Clôture Z : agrégats par mode conformes, ventes = règlements.
        var z = _bdd.RecapZCourant();
        Assert.Equal(1, z.NbTickets);
        Assert.Equal(3.000m, z.TotalTtc);
        Assert.Equal(2, z.ReglementsParMode.Count);
        Assert.Equal(ModeReglement.Especes, z.ReglementsParMode[0].Mode);
        Assert.Equal(2.500m, z.ReglementsParMode[0].Total);
        Assert.Equal(ModeReglement.CarteBancaire, z.ReglementsParMode[1].Mode);
        Assert.Equal(1.000m, z.ReglementsParMode[1].Total);
        // Les règlements stockent les montants REÇUS : la somme dépasse le TTC
        // du trop perçu en espèces — rendu = 0,500 ici.
        Assert.Equal(3.500m, z.TotalRegle);
        Assert.Equal(3.000m, z.TotalRegle - 0.500m);   // TTC + rendu = reçu
        // Rendu dérivé de la période : ce que l'imprimé Z affichera.
        Assert.Equal(0.500m, z.MonnaieRendue);
    }

    [Fact]
    public void RecapZ_SansRendu_MonnaieRendueNulle()
    {
        var caisse = CaisseAvecUnArticle();          // 2,400 réglés au juste prix
        _bdd.EnregistrerTicket(caisse.NumeroTicket, caisse,
            new Reglement[] { new(ModeReglement.Especes, 2.400m) });
        caisse.Encaisser();

        var z = _bdd.RecapZCourant();

        Assert.Equal(0m, z.MonnaieRendue);           // rien n'est rendu
    }
}

/// <summary>Pont vers le décodeur de test privé de TicketEscPosTests.</summary>
public static class TicketEscPosTestRunner
{
    public static string DecodePublique(byte[] flux) =>
        TicketEscPosTests.Decode(flux).Texte;
}
