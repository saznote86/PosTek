using System.IO;
using Microsoft.Data.Sqlite;
using Postec.Caisse.Services;
using Xunit;

namespace Postec.Caisse.Tests;

public sealed class SynchronisationProduitsServiceTests : IDisposable
{
    private readonly string _chemin = Path.Combine(
        Path.GetTempPath(), $"postek_sync_{Guid.NewGuid():N}.db");

    public SynchronisationProduitsServiceTests()
    {
        using var connexion = new SqliteConnection($"Data Source={_chemin}");
        connexion.Open();
        using var commande = connexion.CreateCommand();
        commande.CommandText = @"
            PRAGMA foreign_keys = ON;
            CREATE TABLE famille (code TEXT PRIMARY KEY, libelle TEXT NOT NULL);
            CREATE TABLE famille_article (
                code TEXT PRIMARY KEY, libelle TEXT NOT NULL,
                ordre INTEGER NOT NULL DEFAULT 0,
                couleur TEXT NOT NULL DEFAULT '#3B82F6');
            CREATE TABLE produit (
                code TEXT PRIMARY KEY, designation TEXT NOT NULL,
                code_barres TEXT UNIQUE, prix_ttc NUMERIC,
                famille_code TEXT REFERENCES famille(code),
                taux_tva_code TEXT);
            CREATE TABLE article (
                code TEXT PRIMARY KEY, designation TEXT NOT NULL,
                prix_ttc TEXT NOT NULL, taux_tva TEXT NOT NULL,
                famille_code TEXT REFERENCES famille_article(code),
                description TEXT);
            INSERT INTO famille(code, libelle) VALUES ('F1', 'Boissons');
            INSERT INTO produit(code, designation, prix_ttc, famille_code, taux_tva_code)
                VALUES ('P1', 'Cafe', 1.2, 'F1', '1');
            INSERT INTO article(code, designation, prix_ttc, taux_tva, famille_code)
                VALUES ('P_EXISTANT', 'A conserver', '9.999', '0.190', NULL);";
        commande.ExecuteNonQuery();
    }

    public void Dispose()
    {
        try { File.Delete(_chemin); } catch { }
    }

    [Fact]
    public void Synchronisation_EstIdempotenteEtConserveLesArticles()
    {
        var service = new SynchronisationProduitsService(_chemin);

        var familles = service.SynchroniserFamilles();
        var produits = service.SynchroniserProduits();
        var secondeExecution = service.SynchroniserProduits();

        Assert.Equal(1, familles.NbCrees);
        Assert.Equal(1, produits.NbCrees);
        Assert.Equal(0, produits.NbIgnores);
        Assert.Equal(0, produits.NbErreurs);
        Assert.Equal(0, secondeExecution.NbCrees);
        Assert.Equal(1, secondeExecution.NbIgnores);

        using var connexion = new SqliteConnection($"Data Source={_chemin}");
        connexion.Open();
        using var commande = connexion.CreateCommand();
        commande.CommandText = "SELECT designation, prix_ttc, taux_tva, famille_code FROM article WHERE code = 'P1'";
        using var lecteur = commande.ExecuteReader();
        Assert.True(lecteur.Read());
        Assert.Equal("Cafe", lecteur.GetString(0));
        Assert.Equal("1.200", lecteur.GetString(1));
        Assert.Equal("0.190", lecteur.GetString(2));
        Assert.Equal("F1", lecteur.GetString(3));
    }
}