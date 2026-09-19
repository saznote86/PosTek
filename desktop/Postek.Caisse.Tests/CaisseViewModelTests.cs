using System.Globalization;
using System.IO;
using Postec.Caisse.Caisse;
using Xunit;

namespace Postec.Caisse.Tests;

public sealed class CaisseViewModelTests : IDisposable
{
    private readonly string _chemin = Path.Combine(Path.GetTempPath(), $"postek_caisse_vm_{Guid.NewGuid():N}.db");

    public CaisseViewModelTests()
    {
        using var bdd = new BaseDonnees(_chemin);
        bdd.AjouterFamille(new FamilleArticle("BOISSONS", "Boissons", 1, "#3B82F6"));
        bdd.AjouterFamille(new FamilleArticle("BOULANGERIE", "Boulangerie", 2, "#F59E0B"));
        bdd.AjouterArticle(new Article("CAF", "Café express", 1.200m, 0.190m), "BOISSONS");
        bdd.AjouterArticle(new Article("EAU", "Eau 1L", 0.500m, 0.190m), "BOISSONS");
        bdd.AjouterArticle(new Article("BAG", "Baguette", 0.250m, 0.070m), "BOULANGERIE");
    }

    public void Dispose()
    {
        try { File.Delete(_chemin); } catch { }
    }

    [Fact]
    public void CaisseViewModel_ChargeProduitsAvecFamille()
    {
        using var bdd = new BaseDonnees(_chemin);
        using var vm = new CaisseViewModel(new CaisseService());

        vm.ChargerProduits(bdd);

        Assert.Equal(3, vm.ProduitsFiltres.Count);
        Assert.Contains(vm.ProduitsFiltres, article => article.FamilleCode == "BOISSONS");
    }

    [Fact]
    public void CaisseViewModel_FiltreParFamille()
    {
        using var bdd = new BaseDonnees(_chemin);
        using var vm = new CaisseViewModel(new CaisseService());
        vm.ChargerProduits(bdd);
        var boissons = vm.Familles.Single(famille => famille.Code == "BOISSONS");

        vm.FiltrerParFamilleCommand.Execute(boissons);

        Assert.Equal(2, vm.ProduitsFiltres.Count);
        Assert.All(vm.ProduitsFiltres, article => Assert.Equal("BOISSONS", article.FamilleCode));
    }

    [Fact]
    public void CaisseViewModel_RechercheProduit()
    {
        using var bdd = new BaseDonnees(_chemin);
        using var vm = new CaisseViewModel(new CaisseService());
        vm.ChargerProduits(bdd);

        vm.RechercheText = "café";
        vm.FiltrerProduits();

        var article = Assert.Single(vm.ProduitsFiltres);
        Assert.Equal("CAF", article.Code);
    }

    [Fact]
    public void CaisseViewModel_AjouterAuPanier()
    {
        var caisse = new CaisseService();
        using var vm = new CaisseViewModel(caisse);
        var article = new Article("CAF", "Café", 1.200m, 0.190m);
        vm.ArticleDemandee += articleAjoute => caisse.Ajouter(articleAjoute);

        vm.AjouterArticleCommand.Execute(article);

        Assert.Single(vm.Lignes);
        Assert.Equal("CAF", vm.Lignes[0].Article.Code);
    }

    [Fact]
    public void CaisseViewModel_CalculerTotaux()
    {
        var caisse = new CaisseService();
        using var vm = new CaisseViewModel(caisse);
        caisse.Ajouter(new Article("CAF", "Café", 1.200m, 0.190m));

        Assert.Equal(1.008m, vm.SousTotalHT);
        Assert.Equal(0.192m, vm.TotalTva);
        Assert.Equal(1.000m, vm.TimbreFiscal);
        Assert.Equal(2.200m, vm.TotalTTC);
    }

    [Fact]
    public void CaisseViewModel_SupprimerLigne()
    {
        var caisse = new CaisseService();
        using var vm = new CaisseViewModel(caisse);
        caisse.Ajouter(new Article("CAF", "Café", 1.200m, 0.190m));
        var ligne = Assert.Single(vm.Lignes);

        vm.SupprimerLigneCommand.Execute(ligne);

        Assert.Empty(vm.Lignes);
        Assert.Equal(0m, vm.TotalTtc);
        Assert.Equal(0m, vm.TotalTTC);
    }
}