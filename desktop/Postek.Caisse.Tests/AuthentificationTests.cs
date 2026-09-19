using Microsoft.Data.Sqlite;
using System.IO;
using Postec.Caisse.Caisse;
using Xunit;

namespace Postec.Caisse.Tests;

public sealed class AuthentificationTests : IDisposable
{
    private readonly string _chemin = Path.Combine(Path.GetTempPath(), $"postek_auth_{Guid.NewGuid():N}.db");
    private readonly BaseDonnees _bdd;

    public AuthentificationTests()
    {
        _bdd = new BaseDonnees(_chemin);
    }

    [Fact]
    public void MotDePasse_EstHacheEtVerifieParPBKDF2()
    {
        var empreinte = AuthentificationService.HacherMotDePasse("secret-POSTEC");

        Assert.StartsWith("PBKDF2-SHA256$", empreinte);
        Assert.DoesNotContain("secret-POSTEC", empreinte);
        Assert.True(AuthentificationService.VerifierMotDePasse("secret-POSTEC", empreinte));
        Assert.False(AuthentificationService.VerifierMotDePasse("mauvais", empreinte));
    }

    [Fact]
    public void Authentifier_RetourneLeRoleEtJournaliseLaConnexion()
    {
        _bdd.AjouterUtilisateur("alice", "Alice", "Caissier", "secret");

        var session = _bdd.Authentifier("alice", "secret");

        Assert.NotNull(session);
        Assert.Equal("Caissier", session!.Role);
        Assert.False(session.EstAdministrateur);
        using var cnx = new SqliteConnection($"Data Source={_chemin}");
        cnx.Open();
        using var cmd = cnx.CreateCommand();
        cmd.CommandText = "SELECT action, utilisateur_id FROM AuditLog";
        using var lecteur = cmd.ExecuteReader();
        Assert.True(lecteur.Read());
        Assert.Equal("Connexion", lecteur.GetString(0));
        Assert.Equal(session.Id, lecteur.GetInt64(1));
    }

    [Fact]
    public void AuditLog_ConserveLesActionsDeSession()
    {
        _bdd.AjouterUtilisateur("admin", "Admin", "Administrateur", "secret");
        var session = _bdd.Authentifier("admin", "secret");
        _bdd.JournaliserAudit(session, "Annulation vente", "Ticket 12");
        _bdd.JournaliserAudit(session, "Déconnexion");

        using var cnx = new SqliteConnection($"Data Source={_chemin}");
        cnx.Open();
        using var cmd = cnx.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM AuditLog WHERE utilisateur_id = $id";
        cmd.Parameters.AddWithValue("$id", session!.Id);
        Assert.Equal(3L, (long)cmd.ExecuteScalar()!);
    }

    [Fact]
    public void GestionUtilisateurs_ListeModifieEtSupprime()
    {
        _bdd.AjouterUtilisateur("alice", "Alice", "Caissier", "secret");
        var utilisateur = Assert.Single(_bdd.ListerUtilisateurs());
        Assert.Equal("Caissier", utilisateur.Role);

        _bdd.ModifierUtilisateur(utilisateur.Id, "Alice Admin", "Administrateur", true, "nouveau");
        var modifie = Assert.Single(_bdd.ListerUtilisateurs());
        Assert.Equal("Administrateur", modifie.Role);
        Assert.Equal("Alice Admin", modifie.NomAffiche);
        Assert.NotNull(_bdd.Authentifier("alice", "nouveau"));

        _bdd.SupprimerUtilisateur(utilisateur.Id);
        Assert.Empty(_bdd.ListerUtilisateurs());
    }

    [Fact]
    public void ChargerUtilisateursRecents_Retourne10Maximum()
    {
        for (var index = 0; index < 12; index++)
            _bdd.EnregistrerConnexionReussie($"user-{index}");

        Assert.Equal(10, _bdd.ListerHistoriqueConnexions().Count);
    }

    [Fact]
    public void EnregistrerConnexionReussie_IncrementCompteur()
    {
        _bdd.EnregistrerConnexionReussie("alice");
        _bdd.EnregistrerConnexionReussie("alice");

        var connexion = Assert.Single(_bdd.ListerHistoriqueConnexions());
        Assert.Equal(2, connexion.NombreConnexions);
    }

    [Fact]
    public void EnregistrerConnexionReussie_CreeNouveauRecordSansMotDePasse()
    {
        _bdd.AjouterUtilisateur("alice", "Alice", "Caissier", "secret");
        _bdd.EnregistrerConnexionReussie("alice");

        Assert.Equal("alice", Assert.Single(_bdd.ListerHistoriqueConnexions()).NomUtilisateur);
        using var cnx = new SqliteConnection($"Data Source={_chemin}");
        cnx.Open();
        using var cmd = cnx.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('HistoriqueConnexions') WHERE name LIKE '%passe%'";
        Assert.Equal(0L, (long)cmd.ExecuteScalar()!);
    }

    [Fact]
    public void EffacerHistoriqueConnexions_DesactiveToutesLesEntrees()
    {
        _bdd.EnregistrerConnexionReussie("alice");
        _bdd.EnregistrerConnexionReussie("bob");
        _bdd.EnregistrerConnexionReussie("alice");

        _bdd.EffacerHistoriqueConnexions();

        Assert.Empty(_bdd.ListerHistoriqueConnexions());
        using var cnx = new SqliteConnection($"Data Source={_chemin}");
        cnx.Open();
        using var cmd = cnx.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM HistoriqueConnexions WHERE EstActif = 1";
        Assert.Equal(0L, (long)cmd.ExecuteScalar()!);
    }

    [Fact]
    public void LoginViewModel_AfficheHistoriqueApresTroisConnexions()
    {
        _bdd.AjouterUtilisateur("alice", "Alice", "Caissier", "secret");
        _bdd.AjouterUtilisateur("bob", "Bob", "Caissier", "secret");
        var viewModel = new LoginViewModel(_bdd);

        viewModel.NomUtilisateurSaisi = "alice";
        Assert.True(viewModel.SeConnecter("secret"));
        viewModel.NomUtilisateurSaisi = "bob";
        Assert.True(viewModel.SeConnecter("secret"));
        viewModel.NomUtilisateurSaisi = "alice";
        Assert.True(viewModel.SeConnecter("secret"));

        Assert.Equal(2, viewModel.UtilisateursRecents.Count);
        Assert.Equal(2, viewModel.UtilisateursRecents.Single(x => x.NomUtilisateur == "alice").NombreConnexions);
    }

    public void Dispose()
    {
        _bdd.Dispose();
        try { File.Delete(_chemin); } catch { }
    }
}
