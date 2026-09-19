using Microsoft.Data.Sqlite;
using System.IO;
using Postec.Caisse.Caisse;
using Xunit;

namespace Postec.Caisse.Tests;

public sealed class GestionCaisseTests : IDisposable
{
    private readonly string _chemin = Path.Combine(Path.GetTempPath(), $"postek_session_{Guid.NewGuid():N}.db");
    private readonly BaseDonnees _bdd;

    public GestionCaisseTests() => _bdd = new BaseDonnees(_chemin);

    [Fact]
    public void OuvertureCaisse_EnregistreFondsUtilisateurEtAudit()
    {
        _bdd.AjouterUtilisateur("alice", "Alice", "Caissier", "secret");
        var utilisateur = _bdd.Authentifier("alice", "secret")!;

        var session = _bdd.OuvrirSessionCaisse(utilisateur, 150.500m);

        Assert.True(session.EstOuverte);
        Assert.Equal(utilisateur.Id, session.UtilisateurId);
        Assert.Equal(150.500m, session.FondsInitial);
        Assert.Equal(session.Id, _bdd.SessionCaisseOuverte()!.Id);

        using var cnx = new SqliteConnection($"Data Source={_chemin}");
        cnx.Open();
        using var cmd = cnx.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM AuditLog WHERE action = 'Ouverture caisse'";
        Assert.Equal(1L, (long)cmd.ExecuteScalar()!);
    }

    [Fact]
    public void ViewModelOuverture_RefuseMontantInvalideEtAccepteMontantValide()
    {
        _bdd.AjouterUtilisateur("bob", "Bob", "Caissier", "secret");
        var utilisateur = _bdd.Authentifier("bob", "secret")!;
        var vue = new OuvertureCaisseViewModel(_bdd, utilisateur)
        {
            FondsInitialTexte = "abc"
        };

        Assert.False(vue.Ouvrir());
        Assert.False(vue.EstOuverte);
        Assert.Contains("valide", vue.Message);

        vue.FondsInitialTexte = "25,750";
        Assert.True(vue.Ouvrir());
        Assert.True(vue.EstOuverte);
        Assert.Equal(25.750m, vue.Session!.FondsInitial);
    }

    [Fact]
    public void UneSeuleSessionPeutEtreOuverte()
    {
        _bdd.AjouterUtilisateur("alice", "Alice", "Caissier", "secret");
        _bdd.AjouterUtilisateur("admin", "Admin", "Administrateur", "secret");
        var alice = _bdd.Authentifier("alice", "secret")!;
        var admin = _bdd.Authentifier("admin", "secret")!;
        _bdd.OuvrirSessionCaisse(alice, 10m);

        Assert.Throws<InvalidOperationException>(() => _bdd.OuvrirSessionCaisse(admin, 20m));
    }

    public void Dispose()
    {
        _bdd.Dispose();
        try { File.Delete(_chemin); } catch { }
    }
}
