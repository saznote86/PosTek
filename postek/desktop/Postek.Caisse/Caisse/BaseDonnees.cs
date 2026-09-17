using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Postek.Caisse.Caisse;

/// <summary>
/// Accès SQLite : catalogue, tickets encaissés, compteur de tickets.
/// Conventions : snake_case, décimaux stockés en TEXTE (jamais de flottant),
/// montants au millime — miroir C# du noyau fiscal Python (fiscal/comptabilite.py).
/// </summary>
public sealed class BaseDonnees : IDisposable
{
    private readonly SqliteConnection _cnx;

    public BaseDonnees(string cheminFichier)
    {
        var connexion = $"Data Source={cheminFichier}";
        _cnx = new SqliteConnection(connexion);
        _cnx.Open();
        CreerSchema();
    }

    private void CreerSchema()
    {
        Executer("""
            CREATE TABLE IF NOT EXISTS article (
                code        TEXT PRIMARY KEY,
                designation TEXT NOT NULL,
                prix_ttc    TEXT NOT NULL,   -- décimal stocké en texte (pas de perte)
                taux_tva    TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ticket (
                numero      INTEGER PRIMARY KEY,
                date_heure  TEXT NOT NULL,   -- ISO-8601 UTC
                total_ttc   TEXT NOT NULL,
                total_ht    TEXT NOT NULL,
                total_tva   TEXT NOT NULL,
                z_numero    INTEGER          -- n° de clôture Z (NULL = période en cours)
            );

            CREATE TABLE IF NOT EXISTS ticket_ligne (
                ticket_numero INTEGER NOT NULL REFERENCES ticket(numero),
                position      INTEGER NOT NULL,
                code_article  TEXT NOT NULL,
                designation    TEXT NOT NULL,
                prix_ttc      TEXT NOT NULL,
                taux_tva      TEXT NOT NULL,
                quantite      TEXT NOT NULL,
                remise_pct    TEXT NOT NULL,
                total_ligne   TEXT NOT NULL,
                tva_ligne     TEXT NOT NULL,
                PRIMARY KEY (ticket_numero, position)
            );

            CREATE TABLE IF NOT EXISTS reglement_ticket (
                ticket_numero INTEGER NOT NULL REFERENCES ticket(numero),
                position      INTEGER NOT NULL,
                mode          TEXT NOT NULL,        -- code stable (especes, cb, cheque…)
                montant       TEXT NOT NULL,
                PRIMARY KEY (ticket_numero, position)
            );

            CREATE TABLE IF NOT EXISTS cloture_z (
                numero        INTEGER PRIMARY KEY,  -- numérotation continue, sans trou
                date_heure    TEXT NOT NULL,        -- ISO-8601 UTC
                nb_tickets    INTEGER NOT NULL,
                total_ttc     TEXT NOT NULL,
                total_ht      TEXT NOT NULL,
                total_tva     TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS cloture_z_tva (
                z_numero      INTEGER NOT NULL REFERENCES cloture_z(numero),
                taux_tva      TEXT NOT NULL,
                total_ht      TEXT NOT NULL,
                total_tva     TEXT NOT NULL,
                total_ttc     TEXT NOT NULL,
                PRIMARY KEY (z_numero, taux_tva)
            );
            """);

        // Base de démonstration créée avant l'ajout de z_numero.
        AjouterColonneSiAbsent("ticket", "z_numero", "INTEGER");
        AjouterColonneSiAbsent("ticket_ligne", "tva_ligne", "TEXT NOT NULL DEFAULT '0.000'");
    }

    private void Executer(string sql)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>Migration douce : ajoute une colonne si la table existe déjà sans elle.</summary>
    private void AjouterColonneSiAbsent(string table, string colonne, string type)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info($t) WHERE name = $c";
        cmd.Parameters.AddWithValue("$t", table);
        cmd.Parameters.AddWithValue("$c", colonne);
        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
            Executer($"ALTER TABLE {table} ADD COLUMN {colonne} {type}");
    }

    private static string D(decimal v) => v.ToString("0.000", CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------
    // Articles
    // ------------------------------------------------------------------
    public IReadOnlyList<Article> ListerArticles()
    {
        var liste = new List<Article>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "SELECT code, designation, prix_ttc, taux_tva FROM article ORDER BY designation";
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            liste.Add(new Article(
                lecteur.GetString(0),
                lecteur.GetString(1),
                decimal.Parse(lecteur.GetString(2), CultureInfo.InvariantCulture),
                decimal.Parse(lecteur.GetString(3), CultureInfo.InvariantCulture)));
        }
        return liste;
    }

    public void AjouterArticle(Article a)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO article(code, designation, prix_ttc, taux_tva) VALUES($c, $d, $p, $t)";
        cmd.Parameters.AddWithValue("$c", a.Code);
        cmd.Parameters.AddWithValue("$d", a.Designation);
        cmd.Parameters.AddWithValue("$p", D(a.PrixTtc));
        cmd.Parameters.AddWithValue("$t", D(a.TauxTva));
        cmd.ExecuteNonQuery();
    }

    // ------------------------------------------------------------------
    // Tickets
    // ------------------------------------------------------------------
    /// <summary>Dernier n° de ticket encaissé (0 si base neuve).</summary>
    public int DernierNumeroTicket()
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(numero), 0) FROM ticket";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// Enregistre un ticket et ses règlements (multi-paiement) — tout en une
    /// transaction : un ticket est traçable ou n'existe pas.
    /// </summary>
    public void EnregistrerTicket(int numero, CaisseService caisse,
                                  IReadOnlyList<Reglement>? reglements = null)
    {
        using var tx = _cnx.BeginTransaction();
        try
        {
            var t = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            using (var cmd = _cnx.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO ticket(numero, date_heure, total_ttc, total_ht, total_tva)
                    VALUES($n, $dh, $ttc, $ht, $tva)
                    """;
                cmd.Parameters.AddWithValue("$n", numero);
                cmd.Parameters.AddWithValue("$dh", t);
                cmd.Parameters.AddWithValue("$ttc", D(caisse.TotalTtc));
                cmd.Parameters.AddWithValue("$ht", D(caisse.TotalHt));
                cmd.Parameters.AddWithValue("$tva", D(caisse.TotalTva));
                cmd.ExecuteNonQuery();
            }

            var position = 0;
            foreach (var l in caisse.Lignes)
            {
                using var cmd = _cnx.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO ticket_ligne(ticket_numero, position, code_article, designation,
                                             prix_ttc, taux_tva, quantite, remise_pct, total_ligne, tva_ligne)
                    VALUES($tn, $pos, $c, $d, $p, $t, $q, $r, $tl, $tv)
                    """;
                cmd.Parameters.AddWithValue("$tn", numero);
                cmd.Parameters.AddWithValue("$pos", ++position);
                cmd.Parameters.AddWithValue("$c", l.Article.Code);
                cmd.Parameters.AddWithValue("$d", l.Article.Designation);
                cmd.Parameters.AddWithValue("$p", D(l.Article.PrixTtc));
                cmd.Parameters.AddWithValue("$t", D(l.Article.TauxTva));
                cmd.Parameters.AddWithValue("$q", D(l.Quantite));
                cmd.Parameters.AddWithValue("$r", D(l.RemisePct));
                cmd.Parameters.AddWithValue("$tl", D(l.TotalLigne));
                cmd.Parameters.AddWithValue("$tv", D(l.TvaLigne));
                cmd.ExecuteNonQuery();
            }

            var posRegl = 0;
            foreach (var r in reglements ?? Array.Empty<Reglement>())
            {
                using var cmd = _cnx.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO reglement_ticket(ticket_numero, position, mode, montant)
                    VALUES($tn, $pos, $m, $mt)
                    """;
                cmd.Parameters.AddWithValue("$tn", numero);
                cmd.Parameters.AddWithValue("$pos", ++posRegl);
                cmd.Parameters.AddWithValue("$m", ModesReglement.Code(r.Mode));
                cmd.Parameters.AddWithValue("$mt", D(r.Montant));
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

    /// <summary>Règlements persistés d'un ticket, dans l'ordre de saisie.</summary>
    public IReadOnlyList<Reglement> ReglementsDunTicket(int numero)
    {
        var liste = new List<Reglement>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = """
            SELECT mode, montant FROM reglement_ticket
            WHERE ticket_numero = $n ORDER BY position
            """;
        cmd.Parameters.AddWithValue("$n", numero);
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            liste.Add(new Reglement(
                ModesReglement.DepuisCode(lecteur.GetString(0)),
                decimal.Parse(lecteur.GetString(1), CultureInfo.InvariantCulture)));
        }
        return liste;
    }

    /// <summary>Seed de démonstration, remplacé par la migration depuis l'existant.</summary>
    public static BaseDonnees CreerDemo(string chemin)
    {
        var bdd = new BaseDonnees(chemin);
        if (bdd.ListerArticles().Count == 0)
        {
            bdd.AjouterArticle(new("P001", "Baguette tradition", 0.250m, 0.070m));
            bdd.AjouterArticle(new("P002", "Croissant", 0.600m, 0.130m));
            bdd.AjouterArticle(new("P003", "Pain au chocolat", 0.900m, 0.130m));
            bdd.AjouterArticle(new("P004", "Eau 1L", 0.500m, 0.190m));
            bdd.AjouterArticle(new("P005", "Café express", 1.200m, 0.190m));
            bdd.AjouterArticle(new("P006", "Sandwich thon", 2.500m, 0.190m));
        }
        return bdd;
    }

    // ------------------------------------------------------------------
    // Relecture d'un ticket (impression)
    // ------------------------------------------------------------------
    public record LigneTicketLue(
        string Designation, decimal Quantite, decimal PrixTtc,
        decimal RemisePct, decimal TotalLigne, decimal TvaLigne);

    public IReadOnlyList<LigneTicketLue> LignesDunTicket(int numero)
    {
        var lignes = new List<LigneTicketLue>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = """
            SELECT designation, quantite, prix_ttc, remise_pct, total_ligne, tva_ligne
            FROM ticket_ligne WHERE ticket_numero = $n ORDER BY position
            """;
        cmd.Parameters.AddWithValue("$n", numero);
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            lignes.Add(new LigneTicketLue(
                lecteur.GetString(0),
                decimal.Parse(lecteur.GetString(1), CultureInfo.InvariantCulture),
                decimal.Parse(lecteur.GetString(2), CultureInfo.InvariantCulture),
                decimal.Parse(lecteur.GetString(3), CultureInfo.InvariantCulture),
                decimal.Parse(lecteur.GetString(4), CultureInfo.InvariantCulture),
                decimal.Parse(lecteur.GetString(5), CultureInfo.InvariantCulture)));
        }
        return lignes;
    }

    /// <summary>Récap TVA par taux d'un ticket précis (relu, jamais recalculé).</summary>
    public IReadOnlyList<(decimal Taux, decimal Ht, decimal Tva)> RecapTvaDunTicket(int numero)
    {
        var accumulateurs = new Dictionary<decimal, (decimal Ttc, decimal Tva)>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = """
            SELECT taux_tva, total_ligne, tva_ligne
            FROM ticket_ligne WHERE ticket_numero = $n
            """;
        cmd.Parameters.AddWithValue("$n", numero);
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            var taux = decimal.Parse(lecteur.GetString(0), CultureInfo.InvariantCulture);
            var ttc  = decimal.Parse(lecteur.GetString(1), CultureInfo.InvariantCulture);
            var tva  = decimal.Parse(lecteur.GetString(2), CultureInfo.InvariantCulture);
            var (sTtc, sTva) = accumulateurs.TryGetValue(taux, out var acc) ? acc : (0m, 0m);
            accumulateurs[taux] = (sTtc + ttc, sTva + tva);
        }
        return accumulateurs.OrderByDescending(k => k.Key)
            .Select(k => (k.Key,
                decimal.Round(k.Value.Ttc - k.Value.Tva, 3, MidpointRounding.AwayFromZero),
                k.Value.Tva))
            .ToList();
    }

    public decimal TotalDunTicket(int numero)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "SELECT total_ttc FROM ticket WHERE numero = $n";
        cmd.Parameters.AddWithValue("$n", numero);
        var brut = cmd.ExecuteScalar() as string ?? "0.000";
        return decimal.Parse(brut, CultureInfo.InvariantCulture);
    }

    // ------------------------------------------------------------------
    // Clôture Z
    // ------------------------------------------------------------------
    /// <summary>Dernier n° de clôture Z effectué (0 si aucune).</summary>
    public int DernierNumeroZ()
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(numero), 0) FROM cloture_z";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// Récap TVA par taux des tickets non encore rattachés à une clôture
    /// (z_numero IS NULL). Agrégation en décimal côté C# — SQLite SUM() bascule
    /// en REAL (flottant), interdit pour l'argent (convention POSTEK §4.2).
    /// </summary>
    public IReadOnlyList<LigneZTva> RecapTvaTicketsNonClotures()
    {
        var accumulateurs = new Dictionary<decimal, (decimal Ttc, decimal Tva)>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = """
            SELECT tl.taux_tva, tl.total_ligne, tl.tva_ligne
            FROM ticket_ligne tl
            JOIN ticket t ON t.numero = tl.ticket_numero
            WHERE t.z_numero IS NULL
            ORDER BY tl.taux_tva DESC
            """;
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            var taux = decimal.Parse(lecteur.GetString(0), CultureInfo.InvariantCulture);
            var ttc  = decimal.Parse(lecteur.GetString(1), CultureInfo.InvariantCulture);
            var tva  = decimal.Parse(lecteur.GetString(2), CultureInfo.InvariantCulture);
            var (sommeTtc, sommeTva) = accumulateurs.TryGetValue(taux, out var acc)
                ? acc : (0m, 0m);
            accumulateurs[taux] = (sommeTtc + ttc, sommeTva + tva);
        }
        return accumulateurs.OrderByDescending(k => k.Key)
            .Select(k => new LigneZTva(
                k.Key,
                decimal.Round(k.Value.Ttc - k.Value.Tva, 3, MidpointRounding.AwayFromZero),
                k.Value.Tva))
            .ToList();
    }

    /// <summary>
    /// Totaux par mode de règlement des tickets non encore clôturés,
    /// en décimal côté C# (SQLite SUM() bascule en REAL, interdit).
    /// </summary>
    public IReadOnlyList<LigneZMode> RecapParModeNonClotures()
    {
        var accumulateurs = new SortedDictionary<ModeReglement, decimal>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = """
            SELECT rt.mode, rt.montant
            FROM reglement_ticket rt
            JOIN ticket t ON t.numero = rt.ticket_numero
            WHERE t.z_numero IS NULL
            """;
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            var mode = ModesReglement.DepuisCode(lecteur.GetString(0));
            var montant = decimal.Parse(lecteur.GetString(1), CultureInfo.InvariantCulture);
            accumulateurs[mode] = accumulateurs.TryGetValue(mode, out var s) ? s + montant : montant;
        }
        return accumulateurs.Select(k => new LigneZMode(k.Key, k.Value)).ToList();
    }

    /// <summary>Récapitulatif complet de la période en cours (tickets non clôturés).</summary>
    public RecapZ RecapZCourant()
    {
        var lignes = RecapTvaTicketsNonClotures();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "SELECT numero, total_ttc, total_ht, total_tva FROM ticket WHERE z_numero IS NULL";
        using var lecteur = cmd.ExecuteReader();
        var nbTickets = 0;
        decimal totalTtc = 0m, totalHt = 0m, totalTva = 0m;
        while (lecteur.Read())
        {
            nbTickets++;
            totalTtc += decimal.Parse(lecteur.GetString(1), CultureInfo.InvariantCulture);
            totalHt  += decimal.Parse(lecteur.GetString(2), CultureInfo.InvariantCulture);
            totalTva += decimal.Parse(lecteur.GetString(3), CultureInfo.InvariantCulture);
        }
        return new RecapZ(
            DernierNumeroZ() + 1,
            DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            nbTickets, totalTtc, totalHt, totalTva, lignes, RecapParModeNonClotures());
    }

    /// <summary>
    /// Enregistre la clôture Z n° donné, rattache les tickets de la période
    /// (z_numero) — tout en une transaction : un Z reste traçable ou n'existe pas.
    /// </summary>
    public void CloturerZ(RecapZ z)
    {
        if (z.Numero != DernierNumeroZ() + 1)
            throw new InvalidOperationException(
                $"numérotation Z non continue : attendu {DernierNumeroZ() + 1}, reçu {z.Numero}");
        if (z.NbTickets == 0)
            throw new InvalidOperationException("aucun ticket à clôturer");

        using var tx = _cnx.BeginTransaction();
        try
        {
            using (var cmd = _cnx.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO cloture_z(numero, date_heure, nb_tickets, total_ttc, total_ht, total_tva)
                    VALUES($n, $dh, $nb, $ttc, $ht, $tva)
                    """;
                cmd.Parameters.AddWithValue("$n", z.Numero);
                cmd.Parameters.AddWithValue("$dh", z.DateHeure);
                cmd.Parameters.AddWithValue("$nb", z.NbTickets);
                cmd.Parameters.AddWithValue("$ttc", D(z.TotalTtc));
                cmd.Parameters.AddWithValue("$ht", D(z.TotalHt));
                cmd.Parameters.AddWithValue("$tva", D(z.TotalTva));
                cmd.ExecuteNonQuery();
            }

            foreach (var l in z.LignesTva)
            {
                using var cmd = _cnx.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO cloture_z_tva(z_numero, taux_tva, total_ht, total_tva, total_ttc)
                    VALUES($n, $t, $ht, $tva, $ttc)
                    """;
                cmd.Parameters.AddWithValue("$n", z.Numero);
                cmd.Parameters.AddWithValue("$t", D(l.TauxTva));
                cmd.Parameters.AddWithValue("$ht", D(l.TotalHt));
                cmd.Parameters.AddWithValue("$tva", D(l.TotalTva));
                cmd.Parameters.AddWithValue("$ttc", D(l.TotalTtc));
                cmd.ExecuteNonQuery();
            }

            using (var cmd = _cnx.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "UPDATE ticket SET z_numero = $n WHERE z_numero IS NULL";
                cmd.Parameters.AddWithValue("$n", z.Numero);
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

    public void Dispose() => _cnx.Dispose();
}
