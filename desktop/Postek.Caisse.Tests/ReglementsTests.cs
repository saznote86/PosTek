using Postec.Caisse.Caisse;
using Xunit;

namespace Postec.Caisse.Tests;

/// <summary>
/// Tests du multi-règlement : soldé/reste à payer, fusion par mode, rendu
/// monnaie sur les espèces uniquement, blocage de l'encaissement non soldé,
/// vidage du ticket, et codes stables de persistance.
/// </summary>
public class ReglementsTests
{
    private static readonly Article Cafe =
        new("P005", "Café express", 1.200m, 0.190m);

    private static CaisseService CaisseAvecCafe(decimal qte = 2m)
    {
        var caisse = new CaisseService();
        caisse.Ajouter(Cafe, qte);      // 2,400 TTC
        return caisse;
    }

    // ------------------------------------------------------------------
    // États du ticket
    // ------------------------------------------------------------------
    [Fact]
    public void TicketVide_ResteAPayerEgalAZero()
    {
        var caisse = new CaisseService();

        Assert.Equal(0m, caisse.ResteAPayer);
        Assert.True(caisse.EstSolde);
    }

    [Fact]
    public void ResteAPayer_DiminueAvecLesReglements()
    {
        var caisse = CaisseAvecCafe();          // 2,400

        caisse.Regler(ModeReglement.Especes, 1.000m);
        Assert.Equal(1.400m, caisse.ResteAPayer);

        caisse.Regler(ModeReglement.CarteBancaire, 0.400m);
        Assert.Equal(1.000m, caisse.ResteAPayer);

        caisse.Regler(ModeReglement.Especes, 1.000m);
        Assert.Equal(0m, caisse.ResteAPayer);
        Assert.True(caisse.EstSolde);
    }

    // ------------------------------------------------------------------
    // Fusion par mode
    // ------------------------------------------------------------------
    [Fact]
    public void Regler_MemeMode_Fusionne()
    {
        var caisse = CaisseAvecCafe();

        caisse.Regler(ModeReglement.Especes, 0.500m);
        caisse.Regler(ModeReglement.Especes, 0.700m);

        var reglement = Assert.Single(caisse.Reglements);
        Assert.Equal(ModeReglement.Especes, reglement.Mode);
        Assert.Equal(1.200m, reglement.Montant);
    }

    [Fact]
    public void Regler_ModesDifferents_ResteSepares()
    {
        var caisse = CaisseAvecCafe();

        caisse.Regler(ModeReglement.Especes, 1.000m);
        caisse.Regler(ModeReglement.CarteBancaire, 1.400m);

        Assert.Equal(2, caisse.Reglements.Count);
        Assert.Equal(2.400m, caisse.TotalRegle);
    }

    // ------------------------------------------------------------------
    // Rendu monnaie : espèces uniquement
    // ------------------------------------------------------------------
    [Fact]
    public void Monnaie_EspecesSeules_TropPercuRendu()
    {
        var caisse = CaisseAvecCafe();          // 2,400
        caisse.Regler(ModeReglement.Especes, 5.000m);

        Assert.Equal(2.600m, caisse.MonnaieARendre);
        Assert.True(caisse.EstSolde);
    }

    [Fact]
    public void Monnaie_AucuneSurCB_Seul()
    {
        var caisse = CaisseAvecCafe();          // 2,400

        // CB au-dessus du total : plafonné au reste par l'UI, mais le cœur
        // doit rester sain : trop perçu sans espèces → rien n'est "rendu"
        // (monnaie impossible en CB).
        caisse.Regler(ModeReglement.CarteBancaire, 3.000m);

        Assert.Equal(0m, caisse.MonnaieARendre);
        Assert.True(caisse.EstSolde);
    }

    [Fact]
    public void Monnaie_Mixte_PlafonneeAuxEspecesRecues()
    {
        var caisse = CaisseAvecCafe();          // 2,400
        caisse.Regler(ModeReglement.CarteBancaire, 2.000m);
        caisse.Regler(ModeReglement.Especes, 1.000m);   // trop perçu 0,600

        Assert.Equal(0.600m, caisse.MonnaieARendre);

        // Un second versement espèces : le rendu suit le trop perçu,
        // plafonné au total des espèces reçues.
        caisse.Regler(ModeReglement.Especes, 0.200m);   // espèces 1,200, trop perçu 0,800
        Assert.Equal(0.800m, caisse.MonnaieARendre);
    }

    // ------------------------------------------------------------------
    // Garde-fous
    // ------------------------------------------------------------------
    [Fact]
    public void Regler_MontantNegatifOuNul_Refuse()
    {
        var caisse = CaisseAvecCafe();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => caisse.Regler(ModeReglement.Especes, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => caisse.Regler(ModeReglement.Especes, -1m));
    }

    [Fact]
    public void Regler_SansArticle_Refuse()
    {
        var caisse = new CaisseService();

        Assert.Throws<InvalidOperationException>(
            () => caisse.Regler(ModeReglement.Especes, 1m));
    }

    [Fact]
    public void Encaisser_TicketNonSolde_Refuse()
    {
        var caisse = CaisseAvecCafe();          // 2,400
        caisse.Regler(ModeReglement.Especes, 1.000m);

        Assert.Throws<InvalidOperationException>(() => caisse.Encaisser());

        // Le numéro n'est pas consommé et le ticket reste en l'état.
        Assert.Equal(1, caisse.NumeroTicket);
        Assert.Single(caisse.Reglements);
    }

    [Fact]
    public void Encaisser_SansReglement_SoldeDEnfait_EnEspecesImplicites()
    {
        // Compatibilité : encaisser sans rien saisir = règlement comptant.
        var caisse = CaisseAvecCafe();

        Assert.Equal(2.400m, caisse.Encaisser());
        Assert.Equal(2, caisse.NumeroTicket);
    }

    [Fact]
    public void SupprimerReglement_RecalculeLeReste()
    {
        var caisse = CaisseAvecCafe();
        caisse.Regler(ModeReglement.Especes, 2.400m);
        caisse.Regler(ModeReglement.CarteBancaire, 0.500m);

        // Trop perçu 0,500 (espèces) : supprimer la CB le ramène à 0.
        var cb = caisse.Reglements.First(r => r.Mode == ModeReglement.CarteBancaire);
        caisse.SupprimerReglement(cb);
        Assert.Equal(0m, caisse.ResteAPayer);

        // Supprimer aussi les espèces : le ticket redevient entièrement dû.
        caisse.SupprimerReglement(caisse.Reglements.Single());
        Assert.Equal(2.400m, caisse.ResteAPayer);
        Assert.False(caisse.EstSolde);
    }

    // ------------------------------------------------------------------
    // Encaissement + vidage
    // ------------------------------------------------------------------
    [Fact]
    public void Encaisser_Solde_VideReglementsEtAvanceLeNumero()
    {
        var caisse = CaisseAvecCafe();
        caisse.Regler(ModeReglement.Especes, 5.000m);

        var total = caisse.Encaisser();

        Assert.Equal(2.400m, total);
        Assert.Empty(caisse.Reglements);
        Assert.Equal(0m, caisse.TotalRegle);
        Assert.Equal(2, caisse.NumeroTicket);

        // Les règlements du ticket encaissé restent lisibles (persistance/impression).
        var reglement = Assert.Single(caisse.DerniersReglements);
        Assert.Equal(ModeReglement.Especes, reglement.Mode);
        Assert.Equal(5.000m, reglement.Montant);
    }

    [Fact]
    public void Vider_EffaceAussiLesReglements()
    {
        var caisse = CaisseAvecCafe();
        caisse.Regler(ModeReglement.Especes, 1m);

        caisse.Vider();

        Assert.Empty(caisse.Lignes);
        Assert.Empty(caisse.Reglements);
        Assert.Equal(0m, caisse.ResteAPayer);
    }

    [Fact]
    public void TicketModifie_DeclencheSurReglement()
    {
        var caisse = CaisseAvecCafe();
        var compte = 0;
        caisse.TicketModifie += () => compte++;

        caisse.Regler(ModeReglement.Especes, 1m);
        caisse.SupprimerReglement(caisse.Reglements[0]);
        caisse.Vider();

        Assert.Equal(3, compte);
    }

    // ------------------------------------------------------------------
    // Codes stables (persistance)
    // ------------------------------------------------------------------
    [Fact]
    public void CodesReglement_AllerRetour()
    {
        foreach (var mode in Enum.GetValues<ModeReglement>())
        {
            Assert.Equal(mode, ModesReglement.DepuisCode(ModesReglement.Code(mode)));
        }
    }

    [Fact]
    public void CodesReglement_ValeursStables()
    {
        Assert.Equal("especes", ModesReglement.Code(ModeReglement.Especes));
        Assert.Equal("cb", ModesReglement.Code(ModeReglement.CarteBancaire));
        Assert.Equal("cheque", ModesReglement.Code(ModeReglement.Cheque));
        Assert.Equal("ticket_resto", ModesReglement.Code(ModeReglement.TicketRestaurant));
        Assert.Equal("avoir", ModesReglement.Code(ModeReglement.Avoir));
    }

    [Fact]
    public void DepuisCode_Inconnu_Refuse()
    {
        Assert.Throws<ArgumentException>(() => ModesReglement.DepuisCode("bitcoin"));
    }
}
