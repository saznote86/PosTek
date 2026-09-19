using System.IO;
using Microsoft.Data.Sqlite;
using Postec.Caisse.Caisse;
using Postec.Caisse.Services;
using Xunit;

namespace Postec.Caisse.Tests;

public sealed class ProduitsViewModelTests : IDisposable
{
    private readonly string _chemin = Path.Combine(Path.GetTempPath(), $"postek_produits_{Guid.NewGuid():N}.db");

    public ProduitsViewModelTests()
    {
        using var bdd = new BaseDonnees(_chemin);
        bdd.AjouterArticle(new Article("P002", "Zeste", 2.345m, 0.190m));
        bdd.AjouterArticle(new Article("P001", "Abricot", 1.200m, 0.070m));
    }

    public void Dispose()
    {
        try { File.Delete(_chemin); } catch { }
    }

    [Fact]
    public void ChargerArticles_TrieParDesignation()
    {
        var vm = new ProduitsViewModel(_chemin);
        Assert.Equal(2, vm.Articles.Count);
        Assert.Equal("Abricot", vm.Articles[0].Designation);
        Assert.Equal(2.345m, vm.Articles[1].PrixTtc);
    }

    [Fact]
    public void CreerArticle_InsereDansLaBase()
    {
        var vm = new ProduitsViewModel(_chemin);
        vm.CreerArticle(new ArticleEdition("P003", "Brioche", 3.456m));
        Assert.Contains(vm.Articles, article => article.Code == "P003" && article.PrixTtc == 3.456m);
    }

    [Fact]
    public void ModifierArticle_MetAJourLaLigne()
    {
        var vm = new ProduitsViewModel(_chemin);
        var original = vm.Articles[0];
        vm.ModifierArticle(original, new ArticleEdition(original.Code, "Abricot bio", 1.555m));
        Assert.Contains(vm.Articles, article => article.Designation == "Abricot bio" && article.PrixTtc == 1.555m);
    }

    [Fact]
    public void SupprimerArticle_SupprimeLaSelection()
    {
        var vm = new ProduitsViewModel(_chemin, confirmerSuppression: () => true) { ArticleSelectionne = null };
        vm.ArticleSelectionne = vm.Articles[0];
        vm.SupprimerArticle();
        Assert.Single(vm.Articles);
    }

    [Fact]
    public void SupprimerArticle_RefuseSansConfirmation()
    {
        var vm = new ProduitsViewModel(_chemin, confirmerSuppression: () => false)
        {
            ArticleSelectionne = null,
        };
        vm.ArticleSelectionne = vm.Articles[0];

        vm.SupprimerArticle();

        Assert.Equal(2, vm.Articles.Count);
    }

    [Fact]
    public void CreerArticle_EnregistreAudit()
    {
        var vm = new ProduitsViewModel(_chemin, utilisateurCourant: () => null);
        vm.CreerArticle(new ArticleEdition("P003", "Brioche", 3.456m));

        using var bdd = new BaseDonnees(_chemin);
        Assert.Contains(bdd.ListerAudit(), x => x.Action == "CreationProduit" && x.Details!.Contains("Code=P003"));
    }

    [Fact]
    public void ModifierArticle_EnregistreAudit()
    {
        var vm = new ProduitsViewModel(_chemin, utilisateurCourant: () => null);
        var original = vm.Articles[0];
        vm.ModifierArticle(original, new ArticleEdition(original.Code, "Abricot bio", 1.555m));

        using var bdd = new BaseDonnees(_chemin);
        Assert.Contains(bdd.ListerAudit(), x => x.Action == "ModificationProduit" && x.Details!.Contains("Nom=Abricot bio"));
    }

    [Fact]
    public void SupprimerArticle_EnregistreAudit()
    {
        var vm = new ProduitsViewModel(_chemin, confirmerSuppression: () => true, utilisateurCourant: () => null)
        {
            ArticleSelectionne = null,
        };
        vm.ArticleSelectionne = vm.Articles[0];
        vm.SupprimerArticle();

        using var bdd = new BaseDonnees(_chemin);
        Assert.Contains(bdd.ListerAudit(), x => x.Action == "SuppressionProduit" && x.Details!.Contains("Code=P001"));
    }

    [Fact]
    public void CreerArticle_AvecFamilleEtImprimante_PersisteLesNouveauxChamps()
    {
        var vm = new ProduitsViewModel(_chemin, utilisateurCourant: () => null);
        vm.CreerArticle(new ArticleEdition("P004", "Pizza", 8.500m, 0.190m, "PIZZAS", 2));

        using var connexion = new SqliteConnection($"Data Source={_chemin}");
        connexion.Open();
        using var commande = connexion.CreateCommand();
        commande.CommandText = "SELECT famille_code, taux_tva, imprimante_id FROM article WHERE code = 'P004'";
        using var lecture = commande.ExecuteReader();
        Assert.True(lecture.Read());
        Assert.Equal("PIZZAS", lecture.GetString(0));
        Assert.Equal("0.190", lecture.GetString(1));
        Assert.Equal(2, lecture.GetInt32(2));
    }

    [Fact]
    public void CreerArticle_AvecImage_PersisteLeCheminEtLeRecharge()
    {
        var cheminImage = Path.Combine(Path.GetTempPath(), "produit.png");
        var vm = new ProduitsViewModel(_chemin, utilisateurCourant: () => null);

        vm.CreerArticle(new ArticleEdition("P006", "Image", 4.500m, CheminImage: cheminImage));

        var article = new ProduitsViewModel(_chemin).Articles.Single(x => x.Code == "P006");
        Assert.Equal(cheminImage, article.CheminImage);
    }

    [Fact]
    public void CreerArticle_SansFamille_PersisteNull()
    {
        var vm = new ProduitsViewModel(_chemin);
        vm.CreerArticle(new ArticleEdition("P005", "Eau", 1.000m, 0.000m));

        using var connexion = new SqliteConnection($"Data Source={_chemin}");
        connexion.Open();
        using var commande = connexion.CreateCommand();
        commande.CommandText = "SELECT famille_code, imprimante_id FROM article WHERE code = 'P005'";
        using var lecture = commande.ExecuteReader();
        Assert.True(lecture.Read());
        Assert.True(lecture.IsDBNull(0));
        Assert.True(lecture.IsDBNull(1));
    }

    [Fact]
    public void ChargerArticles_AvecFamilleEtImprimante_ExposeLesLibelles()
    {
        using (var connexion = new SqliteConnection($"Data Source={_chemin}"))
        {
            connexion.Open();
            using var commande = connexion.CreateCommand();
            commande.CommandText = "UPDATE article SET famille_code = 'PIZZAS', imprimante_id = 2 WHERE code = 'P002'";
            commande.ExecuteNonQuery();
        }

        var article = new ProduitsViewModel(_chemin).Articles.Single(x => x.Code == "P002");
        Assert.Equal("Pizzas", article.NomFamille);
        Assert.Equal("Imprimante Cuisine", article.NomImprimante);
    }

    [Fact]
    public void Migration_InsereLesTauxTvaNacefEtLesImprimantes()
    {
        using var connexion = new SqliteConnection($"Data Source={_chemin}");
        connexion.Open();
        using var commande = connexion.CreateCommand();
        commande.CommandText = "SELECT COUNT(*) FROM taux_tva";
        Assert.Equal(4L, (long)commande.ExecuteScalar()!);
        commande.CommandText = "SELECT COUNT(*) FROM imprimante WHERE active = 1";
        Assert.Equal(3L, (long)commande.ExecuteScalar()!);
    }

    [Fact]
    public void Routage_GroupeLesLignesParImprimante()
    {
        var caisse = new Article("SODA", "Soda", 2m, 0.190m) { ImprimanteId = 1 };
        var cuisine = new Article("PIZZA", "Pizza", 8m, 0.190m) { ImprimanteId = 2 };
        var groupes = CaisseService.GrouperParImprimante(new[]
        {
            new LigneTicket(caisse, 1m, 0m), new LigneTicket(cuisine, 1m, 0m),
        });

        Assert.Equal(2, groupes.Count);
        Assert.Equal("SODA", Assert.Single(groupes[1]).Article.Code);
        Assert.Equal("PIZZA", Assert.Single(groupes[2]).Article.Code);
    }
}