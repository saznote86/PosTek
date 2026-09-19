using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Postec.Caisse.Services;

/// <summary>
/// Publie le catalogue migre dans les tables utilisees par la caisse.
/// </summary>
public sealed class SynchronisationProduitsService
{
    private readonly string _cheminBase;

    public SynchronisationProduitsService(string cheminBase)
    {
        _cheminBase = cheminBase ?? throw new ArgumentNullException(nameof(cheminBase));
    }

    /// <summary>
    /// Synchronise les produits migres vers la table article.
    /// Les lignes existantes sont conservees et ne sont jamais remplacees.
    /// </summary>
    public ResultatSynchronisation SynchroniserProduits()
    {
        var resultat = new ResultatSynchronisation();

        try
        {
            using var connexion = OuvrirConnexion();
            using var transaction = connexion.BeginTransaction();
            var codesExistants = LireCodes(connexion, transaction, "article");

            using var lecture = connexion.CreateCommand();
            lecture.Transaction = transaction;
            lecture.CommandText = @"
                SELECT code, designation, prix_ttc, famille_code, taux_tva_code
                FROM produit";

            var produits = new List<(string Code, string Designation, string PrixTtc, string TauxTva, object FamilleCode)>();
            using (var lecteur = lecture.ExecuteReader())
            {
                while (lecteur.Read())
                {
                    try
                    {
                        var code = Texte(lecteur, 0);
                        if (string.IsNullOrWhiteSpace(code))
                        {
                            throw new InvalidOperationException("Le produit ne possede pas de code.");
                        }

                        produits.Add((
                            code,
                            TexteOuDefaut(lecteur, 1, code),
                            DecimalFormate(lecteur, 2),
                            TauxTva(Texte(lecteur, 4)),
                            ValeurOptionnelle(lecteur, 3)));
                    }
                    catch (Exception exception)
                    {
                        resultat.NbErreurs++;
                        resultat.Erreurs.Add($"Produit : {exception.Message}");
                    }
                }
            }

            foreach (var produit in produits)
            {
                if (!codesExistants.Add(produit.Code))
                {
                    resultat.NbIgnores++;
                    continue;
                }

                try
                {
                    using var insertion = connexion.CreateCommand();
                    insertion.Transaction = transaction;
                    insertion.CommandText = @"
                        INSERT INTO article(
                            code, designation, prix_ttc, taux_tva, famille_code, description)
                        VALUES($code, $designation, $prixTtc, $tauxTva, $familleCode, NULL)";
                    insertion.Parameters.AddWithValue("$code", produit.Code);
                    insertion.Parameters.AddWithValue("$designation", produit.Designation);
                    insertion.Parameters.AddWithValue("$prixTtc", produit.PrixTtc);
                    insertion.Parameters.AddWithValue("$tauxTva", produit.TauxTva);
                    insertion.Parameters.AddWithValue("$familleCode", produit.FamilleCode);
                    insertion.ExecuteNonQuery();
                    resultat.NbCrees++;
                }
                catch (Exception exception)
                {
                    resultat.NbErreurs++;
                    resultat.Erreurs.Add($"Produit : {exception.Message}");
                }
            }

            transaction.Commit();
        }
        catch (Exception exception)
        {
            resultat.NbErreurs++;
            resultat.Erreurs.Add(exception.Message);
        }

        return resultat;
    }

    /// <summary>
    /// Synchronise les familles migrees vers les categories de la caisse.
    /// Les lignes existantes sont conservees et ne sont jamais remplacees.
    /// </summary>
    public ResultatSynchronisation SynchroniserFamilles()
    {
        var resultat = new ResultatSynchronisation();

        try
        {
            using var connexion = OuvrirConnexion();
            using var transaction = connexion.BeginTransaction();
            var codesExistants = LireCodes(connexion, transaction, "famille_article");

            using var lecture = connexion.CreateCommand();
            lecture.Transaction = transaction;
            lecture.CommandText = "SELECT code, libelle FROM famille";

            var familles = new List<(string Code, string Libelle)>();
            using (var lecteur = lecture.ExecuteReader())
            {
                while (lecteur.Read())
                {
                    try
                    {
                        var code = Texte(lecteur, 0);
                        if (string.IsNullOrWhiteSpace(code))
                        {
                            throw new InvalidOperationException("La famille ne possede pas de code.");
                        }

                        familles.Add((code, TexteOuDefaut(lecteur, 1, code)));
                    }
                    catch (Exception exception)
                    {
                        resultat.NbErreurs++;
                        resultat.Erreurs.Add($"Famille : {exception.Message}");
                    }
                }
            }

            foreach (var famille in familles)
            {
                if (!codesExistants.Add(famille.Code))
                {
                    resultat.NbIgnores++;
                    continue;
                }

                try
                {
                    using var insertion = connexion.CreateCommand();
                    insertion.Transaction = transaction;
                    insertion.CommandText = @"
                        INSERT INTO famille_article(code, libelle)
                        VALUES($code, $libelle)";
                    insertion.Parameters.AddWithValue("$code", famille.Code);
                    insertion.Parameters.AddWithValue("$libelle", famille.Libelle);
                    insertion.ExecuteNonQuery();
                    resultat.NbCrees++;
                }
                catch (Exception exception)
                {
                    resultat.NbErreurs++;
                    resultat.Erreurs.Add($"Famille : {exception.Message}");
                }
            }

            transaction.Commit();
        }
        catch (Exception exception)
        {
            resultat.NbErreurs++;
            resultat.Erreurs.Add(exception.Message);
        }

        return resultat;
    }

    private SqliteConnection OuvrirConnexion()
    {
        var connexion = new SqliteConnection($"Data Source={_cheminBase}");
        connexion.Open();
        return connexion;
    }

    private static HashSet<string> LireCodes(
        SqliteConnection connexion,
        SqliteTransaction transaction,
        string table)
    {
        using var commande = connexion.CreateCommand();
        commande.Transaction = transaction;
        commande.CommandText = $"SELECT code FROM {table}";
        using var lecteur = commande.ExecuteReader();
        var codes = new HashSet<string>(StringComparer.Ordinal);
        while (lecteur.Read())
        {
            if (!lecteur.IsDBNull(0))
            {
                codes.Add(lecteur.GetString(0));
            }
        }

        return codes;
    }

    private static string Texte(SqliteDataReader lecteur, int index)
    {
        return lecteur.IsDBNull(index)
            ? ""
            : Convert.ToString(lecteur.GetValue(index), CultureInfo.InvariantCulture) ?? "";
    }

    private static string TexteOuDefaut(SqliteDataReader lecteur, int index, string defaut)
    {
        var valeur = Texte(lecteur, index).Trim();
        return valeur.Length == 0 ? defaut : valeur;
    }

    private static object ValeurOptionnelle(SqliteDataReader lecteur, int index)
    {
        var valeur = Texte(lecteur, index).Trim();
        return valeur.Length == 0 ? DBNull.Value : valeur;
    }

    private static string DecimalFormate(SqliteDataReader lecteur, int index)
    {
        if (lecteur.IsDBNull(index))
        {
            return "0.000";
        }

        var valeur = Convert.ToDecimal(lecteur.GetValue(index), CultureInfo.InvariantCulture);
        return valeur.ToString("0.000", CultureInfo.InvariantCulture);
    }

    private static string TauxTva(string? code)
    {
        var valeur = (code ?? "").Trim();
        if (valeur.EndsWith(".0", StringComparison.Ordinal))
        {
            valeur = valeur[..^2];
        }

        return valeur switch
        {
            "1" => "0.190",
            "2" => "0.130",
            "3" => "0.070",
            "4" => "0.000",
            _ => TauxTvaNumerique(valeur)
        };
    }

    private static string TauxTvaNumerique(string valeur)
    {
        if (!decimal.TryParse(valeur.Replace(',', '.'), NumberStyles.Number,
                CultureInfo.InvariantCulture, out var taux))
        {
            return "0.190";
        }

        if (taux > 1)
        {
            taux /= 100;
        }

        return taux.ToString("0.000", CultureInfo.InvariantCulture);
    }
}

public sealed class ResultatSynchronisation
{
    public int NbCrees { get; set; }
    public int NbIgnores { get; set; }
    public int NbErreurs { get; set; }
    public List<string> Erreurs { get; set; } = new();
}