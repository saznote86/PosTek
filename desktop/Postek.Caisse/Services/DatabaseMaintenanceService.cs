using Microsoft.Data.Sqlite;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Postec.Caisse.Services;

public sealed class DatabaseMaintenanceService
{
    public static event Action? TablesVidees;

    private static readonly HashSet<string> TablesMetierAutorisees = new(StringComparer.OrdinalIgnoreCase)
    {
        "article", "produit", "famille_article", "famille", "client", "Clients",
        "fournisseur", "Fournisseurs", "mouvement", "Mouvements", "facture",
        "ticket", "ticket_ligne", "reglement_ticket", "cloture_z", "cloture_z_tva",
        "SessionsCaisse", "RapportsZ", "Familles", "client_extras", "vente",
        "ligne_vente", "session_caisse", "rapport_z", "historique_connexions", "HistoriqueConnexions"
    };

    public static IReadOnlyList<string> TablesMetierParDefaut => new[]
    {
        "ticket_ligne", "reglement_ticket", "ticket", "cloture_z_tva", "cloture_z",
        "RapportsZ", "SessionsCaisse", "mouvement", "Mouvements", "facture",
        "article", "produit", "famille_article", "famille", "client_extras",
        "client", "Clients", "fournisseur", "Fournisseurs", "Familles",
        "ligne_vente",
        "HistoriqueConnexions"
    };

    public Task RebuildIndexesAsync(string chemin) => Task.Run(() => Reindexer(chemin));
    public Task ReparerIndexAsync(string chemin) => RebuildIndexesAsync(chemin);

    public string CreerBackup(string cheminBase, string suffixe)
    {
        if (!File.Exists(cheminBase))
            throw new FileNotFoundException("Base SQLite introuvable.", cheminBase);

        var dossier = Path.Combine(Path.GetDirectoryName(cheminBase) ?? AppContext.BaseDirectory,
            "donnees", "backups");
        Directory.CreateDirectory(dossier);
        var horodatage = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var destination = Path.Combine(dossier, $"postec_backup_{suffixe}_{horodatage}.db");
        File.Copy(cheminBase, destination, overwrite: false);
        return destination;
    }

    public void ViderTablesMetier(string chemin, IEnumerable<string> tables)
    {
        var liste = tables.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (liste.Any(table => !TablesMetierAutorisees.Contains(table)))
            throw new ArgumentException("La liste contient une table non autorisÃ©e.", nameof(tables));

        using var cnx = new SqliteConnection($"Data Source={chemin}");
        cnx.Open();
        using var fk = cnx.CreateCommand();
        fk.CommandText = "PRAGMA foreign_keys = OFF;";
        fk.ExecuteNonQuery();
        using var tx = cnx.BeginTransaction();
        try
        {
            foreach (var table in liste)
            {
                using var existe = cnx.CreateCommand();
                existe.Transaction = tx;
                existe.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $table";
                existe.Parameters.AddWithValue("$table", table);
                if (Convert.ToInt32(existe.ExecuteScalar()) == 0) continue;

                using var delete = cnx.CreateCommand();
                delete.Transaction = tx;
                delete.CommandText = $"DELETE FROM [{table}];";
                delete.ExecuteNonQuery();
            }

            using var sequences = cnx.CreateCommand();
            sequences.Transaction = tx;
            sequences.CommandText = "DELETE FROM sqlite_sequence WHERE name IN (" +
                string.Join(",", liste.Select((_, index) => $"$table{index}")) + ");";
            for (var index = 0; index < liste.Count; index++)
                sequences.Parameters.AddWithValue($"$table{index}", liste[index]);
            sequences.ExecuteNonQuery();
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
        finally
        {
            using var reactiver = cnx.CreateCommand();
            reactiver.CommandText = "PRAGMA foreign_keys = ON;";
            reactiver.ExecuteNonQuery();
        }

        TablesVidees?.Invoke();
    }

    public Task ViderTablesAsync(string chemin, IEnumerable<string> tables) =>
        Task.Run(() => ViderTablesMetier(chemin, tables));

    public IReadOnlyDictionary<string, long> CompterDonneesMetier(string chemin)
    {
        using var cnx = new SqliteConnection($"Data Source={chemin}");
        cnx.Open();
        var resultats = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in TablesMetierParDefaut)
        {
            using var cmd = cnx.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $table";
            cmd.Parameters.AddWithValue("$table", table);
            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0) continue;
            cmd.Parameters.Clear();
            cmd.CommandText = $"SELECT COUNT(*) FROM [{table}];";
            resultats[table] = Convert.ToInt64(cmd.ExecuteScalar());
        }
        return resultats;
    }

    public void InitialiserParametresParDefaut(string chemin)
    {
        using var cnx = new SqliteConnection($"Data Source={chemin}");
        cnx.Open();
        using var tx = cnx.BeginTransaction();
        try
        {
            foreach (var (code, valeur) in new[]
            {
                ("TVA_DEFAUT", "0.190"), ("TIMBRE_FISCAL", "1.000"),
                ("FORMAT_TICKET", "80mm"), ("IMPRIMANTE", "")
            })
            {
                using var cmd = cnx.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO Parametres(code, valeur) VALUES($code, $valeur) " +
                                   "ON CONFLICT(code) DO UPDATE SET valeur = excluded.valeur;";
                cmd.Parameters.AddWithValue("$code", code);
                cmd.Parameters.AddWithValue("$valeur", valeur);
                cmd.ExecuteNonQuery();
            }

            foreach (var (code, libelle, ordre) in new[]
            {
                ("DIVERS", "Divers", 1), ("BOISSONS", "Boissons", 2),
                ("ALIMENTATION", "Alimentation", 3)
            })
            {
                using var cmd = cnx.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT OR IGNORE INTO famille_article(code, libelle, ordre) " +
                                   "VALUES($code, $libelle, $ordre);";
                cmd.Parameters.AddWithValue("$code", code);
                cmd.Parameters.AddWithValue("$libelle", libelle);
                cmd.Parameters.AddWithValue("$ordre", ordre);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void Reindexer(string chemin)
    {
        using var cnx = new SqliteConnection($"Data Source={chemin}");
        cnx.Open();
        using var cmd = cnx.CreateCommand();
        cmd.CommandText = "REINDEX;";
        cmd.ExecuteNonQuery();
    }

    public void Vacuum(string chemin)
    {
        using var cnx = new SqliteConnection($"Data Source={chemin}");
        cnx.Open();
        using var cmd = cnx.CreateCommand();
        cmd.CommandText = "VACUUM;";
        cmd.ExecuteNonQuery();
    }

    public bool Integrite(string chemin)
    {
        using var cnx = new SqliteConnection($"Data Source={chemin}");
        cnx.Open();
        using var cmd = cnx.CreateCommand();
        cmd.CommandText = "PRAGMA integrity_check;";
        return string.Equals(cmd.ExecuteScalar()?.ToString(), "ok", StringComparison.OrdinalIgnoreCase);
    }

    public void RemiseAZ(string chemin)
    {
        using var cnx = new SqliteConnection($"Data Source={chemin}");
        cnx.Open();
        using var tx = cnx.BeginTransaction();
        using var cmd = cnx.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
            DELETE FROM ticket_ligne;
            DELETE FROM reglement_ticket;
            DELETE FROM ticket;
            DELETE FROM cloture_z_tva;
            DELETE FROM cloture_z;
            DELETE FROM RapportsZ;
            DELETE FROM SessionsCaisse;";
        cmd.ExecuteNonQuery();
        tx.Commit();
    }
}

