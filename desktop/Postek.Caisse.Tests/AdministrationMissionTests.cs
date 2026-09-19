using System.IO;
using Microsoft.Data.Sqlite;
using Postec.Caisse.Caisse;
using Postec.Caisse.Services;
using Xunit;

namespace Postec.Caisse.Tests;

/// <summary>
/// Mission « corrections critiques + Administration » : invalidation des
/// ViewModels Produits/Clients/Vendeurs/Caisse après une RAZ (événement
/// TablesVidees), désabonnement idempotent via Dispose et message neutre
/// sans identifiants après première installation.
/// </summary>
public sealed class AdministrationMissionTests : IDisposable
{
    private readonly string _chemin = Path.Combine(Path.GetTempPath(), $"postek_admin_{Guid.NewGuid():N}.db");
    private readonly DatabaseMaintenanceService _maintenance = new();

    public AdministrationMissionTests()
    {
        using var bdd = new BaseDonnees(_chemin);
        bdd.AjouterArticle(new Article("P001", "Abricot", 1.200m, 0.190m));
        // Garantit la présence du compte admin (comme au démarrage réel de
        // l'app) pour le test VendeursViewModel, qui liste Utilisateurs.
        bdd.GarantirAdministrateurParDefaut();
    }

    public void Dispose()
    {
        try { File.Delete(_chemin); } catch { }
    }

    [Fact]
    public void ProduitsViewModel_SeRecharge_ApresTablesVidees()
    {
        var vm = new ProduitsViewModel(_chemin, utilisateurCourant: () => null);
        Assert.Single(vm.Articles);

        _maintenance.ViderTablesMetier(_chemin, new[] { "article" });

        Assert.Empty(vm.Articles);
        vm.Dispose();
    }

    [Fact]
    public void ClientsViewModel_SeRecharge_ApresTablesVidees()
    {
        InsererClient("CLI1", "Client Test");
        var vm = new ClientsViewModel(_chemin);
        Assert.Single(vm.Clients);

        _maintenance.ViderTablesMetier(_chemin, new[] { "Clients" });

        Assert.Empty(vm.Clients);
        vm.Dispose();
    }

    [Fact]
    public void VendeursViewModel_SeRecharge_ApresTablesVidees()
    {
        var vm = new VendeursViewModel(_chemin);
        Assert.Single(vm.Vendeurs);

        // Modification derrière le dos du ViewModel : un utilisateur
        // supplémentaire est inséré, puis l'événement RAZ force le
        // rechargement. Utilisateurs n'est pas vidé (comptes conservés),
        // donc la liste passe de 1 à 2 lignes : preuve du rechargement.
        InsererUtilisateur("vendeur2");
        _maintenance.ViderTablesMetier(_chemin, new[] { "Clients" });

        Assert.Equal(2, vm.Vendeurs.Count);
        vm.Dispose();
    }

    [Fact]
    public void CaisseViewModel_NotteTablesVidees()
    {
        var caisse = new CaisseService();
        using var vm = new CaisseViewModel(caisse);
        var notifie = false;
        vm.ProduitsRecharges += () => notifie = true;

        _maintenance.ViderTablesMetier(_chemin, new[] { "article" });

        Assert.True(notifie);
    }

    [Fact]
    public void Dispose_Desabonne_TablesVidees()
    {
        var vm = new ProduitsViewModel(_chemin, utilisateurCourant: () => null);
        Assert.Single(vm.Articles);
        vm.Dispose();

        // Après Dispose, l'événement ne doit plus déclencher de rechargement :
        // la table est vidée en base mais la collection reste inchangée.
        _maintenance.ViderTablesMetier(_chemin, new[] { "article" });

        Assert.Single(vm.Articles);
    }

    [Fact]
    public void MessageFinPremiereInstallation_NeReveleAucunIdentifiant()
    {
        var message = OutilsViewModel.MessageFinPremiereInstallation;

        Assert.Contains("administrateur", message);
        Assert.DoesNotContain("Mot de passe :", message);
        Assert.DoesNotContain("admin\n", message);
        Assert.DoesNotContain("admin/", message);
    }

    private void InsererClient(string code, string nom)
    {
        using var connexion = new SqliteConnection($"Data Source={_chemin}");
        connexion.Open();
        using var commande = connexion.CreateCommand();
        commande.CommandText = @"INSERT INTO Clients(code, nom, solde) VALUES($code, $nom, '0.000')";
        commande.Parameters.AddWithValue("$code", code);
        commande.Parameters.AddWithValue("$nom", nom);
        commande.ExecuteNonQuery();
    }

    private void InsererUtilisateur(string identifiant)
    {
        using var connexion = new SqliteConnection($"Data Source={_chemin}");
        connexion.Open();
        using var commande = connexion.CreateCommand();
        commande.CommandText = @"INSERT INTO Utilisateurs(identifiant, nom_affiche, role, mot_de_passe, cree_le, actif)
                                 VALUES($identifiant, $nom, 'Caissier', 'x', $date, 1)";
        commande.Parameters.AddWithValue("$identifiant", identifiant);
        commande.Parameters.AddWithValue("$nom", identifiant);
        commande.Parameters.AddWithValue("$date", DateTime.UtcNow.ToString("o"));
        commande.ExecuteNonQuery();
    }
}
