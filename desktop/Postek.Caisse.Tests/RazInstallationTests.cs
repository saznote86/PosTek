using System.IO;
using Microsoft.Data.Sqlite;
using Postec.Caisse.Caisse;
using Postec.Caisse.Services;
using Xunit;

namespace Postec.Caisse.Tests;

public sealed class RazInstallationTests : IDisposable
{
    private readonly string _chemin = Path.Combine(
        Path.GetTempPath(), $"postek_raz_{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        try { File.Delete(_chemin); } catch { }
    }

    [Fact]
    public void ViderTablesMetier_VideLesDonneesEtConserveLeCompteEtLesParametres()
    {
        using (var bdd = new BaseDonnees(_chemin))
        {
            bdd.AjouterArticle(new("P1", "Article", 1m, .19m));
            bdd.AjouterUtilisateur("admin", "Admin", "Administrateur", "secret");
            bdd.EnregistrerConnexionReussie("admin");
            using var cmd = Connexion().CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS produit (code TEXT PRIMARY KEY, designation TEXT NOT NULL);" +
                              "INSERT INTO produit(code, designation) VALUES('P1', 'Article');" +
                              "INSERT INTO Parametres(code, valeur) VALUES('TVA_DEFAUT', '0.190')";
            cmd.ExecuteNonQuery();
        }

        var maintenance = new DatabaseMaintenanceService();
        maintenance.ViderTablesMetier(_chemin, DatabaseMaintenanceService.TablesMetierParDefaut);
        using (var bddApresRaz = new BaseDonnees(_chemin))
            bddApresRaz.CreerOuReinitialiserAdministrateur("admin", true);

        using var connexion = Connexion();
        Assert.Equal(0L, Compter(connexion, "article"));
        Assert.Equal(0L, Compter(connexion, "produit"));
        Assert.Equal(1L, Compter(connexion, "Utilisateurs"));
        Assert.Equal(1L, Compter(connexion, "Parametres"));
        Assert.Equal(0L, Compter(connexion, "HistoriqueConnexions"));
        using var bddApres = new BaseDonnees(_chemin);
        Assert.NotNull(bddApres.Authentifier("admin", "admin"));
        Assert.Null(bddApres.Authentifier("admin", "secret"));
    }

    [Fact]
    public void ViderTablesMetier_ReinitialiseLaSequenceDesSessions()
    {
        using (var bdd = new BaseDonnees(_chemin))
        {
            bdd.AjouterUtilisateur("admin", "Admin", "Administrateur", "secret");
            var admin = bdd.Authentifier("admin", "secret")!;
            bdd.CreerSessionCaisseInitialeFermee(admin.Id);
        }

        var maintenance = new DatabaseMaintenanceService();
        maintenance.ViderTablesMetier(_chemin, DatabaseMaintenanceService.TablesMetierParDefaut);
        using var bddApres = new BaseDonnees(_chemin);
        bddApres.CreerSessionCaisseInitialeFermee(1);
        using var connexion = Connexion();
        Assert.Equal(1L, connexion.ExecuteScalar("SELECT id FROM SessionsCaisse"));
    }

    [Fact]
    public void ViderTablesMetier_RefuseUneTableNonAutoriseeSansEffacer()
    {
        using (var bdd = new BaseDonnees(_chemin))
            bdd.AjouterArticle(new("P1", "Article", 1m, .19m));

        var maintenance = new DatabaseMaintenanceService();
        Assert.Throws<ArgumentException>(() => maintenance.ViderTablesMetier(
            _chemin, new[] { "article", "Utilisateurs" }));

        using var connexion = Connexion();
        Assert.Equal(1L, Compter(connexion, "article"));
    }

    [Fact]
    public void PremiereInstallation_CreeAdminAvecChangementObligatoire()
    {
        using (var bdd = new BaseDonnees(_chemin))
        {
            var maintenance = new DatabaseMaintenanceService();
            maintenance.InitialiserParametresParDefaut(_chemin);
            var admin = bdd.CreerOuReinitialiserAdministrateur("admin", true);
            bdd.CreerSessionCaisseInitialeFermee(admin.Id);
        }

        using var connexion = Connexion();
        var flag = Convert.ToInt32(connexion.ExecuteScalar(
            "SELECT DoitChangerMotDePasse FROM Utilisateurs WHERE identifiant = 'admin'"));
        Assert.Equal(1, flag);
        Assert.Equal(4L, Compter(connexion, "Parametres"));
        Assert.Equal(1L, Compter(connexion, "SessionsCaisse"));
    }

    [Fact]
    public void PremiereInstallation_EstIdempotentePourAdmin()
    {
        using var bdd = new BaseDonnees(_chemin);
        bdd.CreerOuReinitialiserAdministrateur("admin", true);
        bdd.CreerOuReinitialiserAdministrateur("admin", true);

        using var connexion = Connexion();
        Assert.Equal(1L, Compter(connexion, "Utilisateurs"));
        Assert.NotNull(bdd.Authentifier("admin", "admin"));
    }

    [Fact]
    public void Connexion_AdminSansHistorique_EstVisibleEtRecree()
    {
        using var bdd = new BaseDonnees(_chemin);
        bdd.AjouterUtilisateur("caissier", "Caissier", "Caissier", "secret");

        var admin = bdd.GarantirAdministrateurParDefaut();
        var utilisateurs = bdd.ListerHistoriqueConnexions();

        Assert.Equal("admin", admin.Identifiant);
        Assert.Contains(utilisateurs, utilisateur => utilisateur.NomUtilisateur == "admin");
        Assert.NotNull(bdd.Authentifier("admin", "123456"));
    }

    private SqliteConnection Connexion()
    {
        var connexion = new SqliteConnection($"Data Source={_chemin}");
        connexion.Open();
        return connexion;
    }

    private static long Compter(SqliteConnection connexion, string table) =>
        Convert.ToInt64(connexion.ExecuteScalar($"SELECT COUNT(*) FROM [{table}]"));
}

internal static class SqliteConnectionExtensions
{
    public static object? ExecuteScalar(this SqliteConnection connexion, string sql)
    {
        using var command = connexion.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }
}
