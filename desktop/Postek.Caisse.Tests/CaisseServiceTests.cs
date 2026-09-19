using Postec.Caisse.Caisse;
using Xunit;

namespace Postec.Caisse.Tests;

/// <summary>
/// Tests du cœur métier caisse : TVA, remises, numérotation.
/// Conventions POSTEC : décimal au millime, arrondi AwayFromZero
/// (miroir C# de fiscal/comptabilite.py — jamais de flottant).
/// </summary>
public class CaisseServiceTests
{
    private static readonly Article Cafe =
        new("P005", "Café express", 1.200m, 0.190m);
    private static readonly Article Baguette =
        new("P001", "Baguette tradition", 0.250m, 0.070m);

    [Fact]
    public void TicketVide_TotalZero()
    {
        var caisse = new CaisseService();

        Assert.Equal(0m, caisse.TotalTtc);
        Assert.Equal(0m, caisse.TotalTva);
        Assert.Equal(0m, caisse.TotalHt);
        Assert.Equal(0, caisse.NbArticles);
    }

    [Fact]
    public void TotalTtc_SommeDesLignes()
    {
        var caisse = new CaisseService();
        caisse.Ajouter(Cafe, 2m);
        caisse.Ajouter(Baguette, 1m);

        Assert.Equal(2.650m, caisse.TotalTtc);
        Assert.Equal(3, caisse.NbArticles);
    }

    // ------------------------------------------------------------------
    // TVA (formule TTC × t/(1+t) — cf. fiscal/comptabilite.py tva_depuis_ttc)
    // ------------------------------------------------------------------
    [Fact]
    public void TvaLigne_TtcFoisTauxSurUnPlusTaux()
    {
        // Café 1,200 TTC à 19 % : TVA = 1,200 × 0,19/1,19 = 0,1916… → 0,192
        var ligne = new LigneTicket(Cafe, 1m, 0m);

        Assert.Equal(0.192m, ligne.TvaLigne);
    }

    [Fact]
    public void TotalTva_SommeParTaux()
    {
        var caisse = new CaisseService();
        caisse.Ajouter(Cafe, 2m);        // 2,400 TTC, TVA 7 % sur baguette plus bas
        caisse.Ajouter(Baguette, 4m);    // 1,000 TTC à 7 %

        // Café : 2 × 1,200 × 0,19/1,19 = 0,3832… → 0,383 (par ligne, × 2 lignes ?)
        // Non : fusion des lignes identiques → une ligne 2,400 → TVA 0,383.
        // Baguette : 4 × 0,250 = 1,000 → TVA = 1,000 × 0,07/1,07 = 0,0654… → 0,065.
        Assert.Equal(0.448m, caisse.TotalTva);
        Assert.Equal(3.400m, caisse.TotalTtc);
        Assert.Equal(2.952m, caisse.TotalHt);
    }

    [Fact]
    public void TvaLigne_TauxZero_DonneZero()
    {
        var exonere = new Article("P010", "Service non taxable", 5.000m, 0m);

        var ligne = new LigneTicket(exonere, 1m, 0m);

        Assert.Equal(0m, ligne.TvaLigne);
    }

    // ------------------------------------------------------------------
    // Remises
    // ------------------------------------------------------------------
    [Fact]
    public void Remise_ReduitLeTotalLigne()
    {
        // 2,500 − 10 % = 2,250
        var ligne = new LigneTicket(new Article("P006", "Sandwich thon", 2.500m, 0.190m), 1m, 10m);

        Assert.Equal(2.250m, ligne.TotalLigne);
    }

    [Fact]
    public void Remise_ArrondiAuMillime_AwayFromZero()
    {
        // 1,115 × 0,9 = 1,0035 → 1,004 (AwayFromZero), pas 1,003 (banker's)
        var ligne = new LigneTicket(new Article("PX", "Test", 1.115m, 0m), 1m, 10m);

        Assert.Equal(1.004m, ligne.TotalLigne);
    }

    [Fact]
    public void RemisesDifferentes_LignesSeparees_PuisFusionSiIdentiques()
    {
        var caisse = new CaisseService();

        caisse.Ajouter(Cafe, 1m, remisePct: 0m);
        caisse.Ajouter(Cafe, 1m, remisePct: 10m);
        Assert.Equal(2, caisse.Lignes.Count);   // remises ≠ → lignes distinctes

        caisse.Ajouter(Cafe, 1m, remisePct: 10m);
        Assert.Equal(2, caisse.Lignes.Count);   // même remise → fusion
        var fusionnee = caisse.Lignes.First(l => l.RemisePct == 10m);
        Assert.Equal(2m, fusionnee.Quantite);
    }

    [Fact]
    public void Remise_TvaCalculeeSurLeMontantRemise()
    {
        // 1,200 − 10 % = 1,080 TTC → TVA = 1,080 × 0,19/1,19 = 0,17252… → 0,172
        var ligne = new LigneTicket(Cafe, 1m, 10m);

        Assert.Equal(1.080m, ligne.TotalLigne);
        Assert.Equal(0.172m, ligne.TvaLigne);
    }

    // ------------------------------------------------------------------
    // Numérotation des tickets
    // ------------------------------------------------------------------
    [Fact]
    public void NumeroTicket_DemarreAUnEtAvance()
    {
        var caisse = new CaisseService();
        Assert.Equal(1, caisse.NumeroTicket);

        caisse.Ajouter(Cafe);
        var total = caisse.Encaisser();

        Assert.Equal(1.200m, total);
        Assert.Equal(2, caisse.NumeroTicket);
    }

    [Fact]
    public void DefinirNumeroTicket_RepriseApresRedemarrage()
    {
        var caisse = new CaisseService();
        caisse.DefinirNumeroTicket(518);   // dernier ticket persisté : 517

        Assert.Equal(518, caisse.NumeroTicket);

        caisse.Ajouter(Cafe);
        caisse.Encaisser();

        Assert.Equal(519, caisse.NumeroTicket);
    }

    [Fact]
    public void Encaisser_TicketVide_NeConsommePasDeNumero()
    {
        var caisse = new CaisseService();

        var total = caisse.Encaisser();

        Assert.Equal(0m, total);
        Assert.Equal(1, caisse.NumeroTicket);
    }

    [Fact]
    public void Encaisser_VideLeTicket()
    {
        var caisse = new CaisseService();
        caisse.Ajouter(Cafe, 2m);

        caisse.Encaisser();

        Assert.Empty(caisse.Lignes);
        Assert.Equal(0m, caisse.TotalTtc);
    }

    // ------------------------------------------------------------------
    // Divers
    // ------------------------------------------------------------------
    [Fact]
    public void Supprimer_RetireLaLigne()
    {
        var caisse = new CaisseService();
        caisse.Ajouter(Cafe, 2m);
        caisse.Ajouter(Baguette);

        caisse.Supprimer(caisse.Lignes[0]);

        Assert.Single(caisse.Lignes);
        Assert.Equal(0.250m, caisse.TotalTtc);
    }

    [Fact]
    public void TicketModifie_DeclencheAChaqueOperation()
    {
        var caisse = new CaisseService();
        var compte = 0;
        caisse.TicketModifie += () => compte++;

        caisse.Ajouter(Cafe);
        caisse.Ajouter(Cafe);      // fusion, déclenche quand même
        caisse.Supprimer(caisse.Lignes[0]);
        caisse.Vider();

        Assert.Equal(4, compte);
    }

    [Fact]
    public void CaisseViewModel_CommandesReflètentLeTicket()
    {
        var caisse = new CaisseService();
        var vue = new CaisseViewModel(caisse);
        var payerAppele = false;
        vue.PayerDemande += () => payerAppele = true;

        Assert.False(vue.PayerCommand.CanExecute(null));
        caisse.Ajouter(Cafe);
        Assert.True(vue.PayerCommand.CanExecute(null));

        vue.PayerCommand.Execute(null);
        Assert.True(payerAppele);
        vue.Dispose();
    }
}
