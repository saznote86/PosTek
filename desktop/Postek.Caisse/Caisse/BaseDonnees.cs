using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Postec.Caisse.Caisse;

/// <summary>
/// Famille d'articles affichée comme onglet dans la grille tactile.
/// </summary>
public record FamilleArticle(string Code, string Libelle, int Ordre, string Couleur);

public sealed class HistoriqueConnexion
{
    public int Id { get; set; }
    public string NomUtilisateur { get; set; } = "";
    public DateTime DerniereConnexion { get; set; }
    public int NombreConnexions { get; set; }
    public bool EstActif { get; set; }
}

/// <summary>
/// Accès SQLite : catalogue, tickets encaissés, compteur de tickets.
/// Conventions : snake_case, décimaux stockés en TEXTE (jamais de flottant),
/// montants au millime — miroir C# du noyau fiscal Python (fiscal/comptabilite.py).
/// </summary>
public sealed class BaseDonnees : IDisposable
{
    private readonly SqliteConnection _cnx;
    public string CheminFichier { get; }

    public BaseDonnees(string cheminFichier)
    {
        CheminFichier = cheminFichier;
        var connexion = $"Data Source={cheminFichier}";
        _cnx = new SqliteConnection(connexion);
        _cnx.Open();
        CreerSchema();
    }

    private void CreerSchema()
    {
        Executer(@"
            CREATE TABLE IF NOT EXISTS famille_article (
                code    TEXT PRIMARY KEY,
                libelle TEXT NOT NULL,
                ordre   INTEGER NOT NULL DEFAULT 0,
                couleur TEXT NOT NULL DEFAULT '#3B82F6'
            );

            CREATE TABLE IF NOT EXISTS article (
                code         TEXT PRIMARY KEY,
                designation  TEXT NOT NULL,
                prix_ttc     TEXT NOT NULL,
                taux_tva     TEXT NOT NULL,
                famille_code TEXT REFERENCES famille_article(code),
                description  TEXT,
                chemin_image TEXT
            );

            CREATE TABLE IF NOT EXISTS ticket (
                numero      INTEGER PRIMARY KEY,
                date_heure  TEXT NOT NULL,
                total_ttc   TEXT NOT NULL,
                total_ht    TEXT NOT NULL,
                total_tva   TEXT NOT NULL,
                z_numero    INTEGER
            );

            CREATE TABLE IF NOT EXISTS ticket_ligne (
                ticket_numero INTEGER NOT NULL REFERENCES ticket(numero),
                position      INTEGER NOT NULL,
                code_article  TEXT NOT NULL,
                designation   TEXT NOT NULL,
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
                mode          TEXT NOT NULL,
                montant       TEXT NOT NULL,
                PRIMARY KEY (ticket_numero, position)
            );

            CREATE TABLE IF NOT EXISTS cloture_z (
                numero        INTEGER PRIMARY KEY,
                date_heure    TEXT NOT NULL,
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

            CREATE TABLE IF NOT EXISTS Utilisateurs (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                identifiant     TEXT NOT NULL UNIQUE COLLATE NOCASE,
                nom_affiche     TEXT NOT NULL,
                role            TEXT NOT NULL CHECK(role IN ('Administrateur', 'Caissier')),
                mot_de_passe    TEXT NOT NULL,
                actif           INTEGER NOT NULL DEFAULT 1,
                cree_le         TEXT NOT NULL,
                DoitChangerMotDePasse INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Parametres (
                code TEXT PRIMARY KEY,
                valeur TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Licence (
                id INTEGER PRIMARY KEY CHECK(id = 1),
                donnees TEXT NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS HistoriqueConnexions (
                Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                NomUtilisateur      TEXT NOT NULL UNIQUE COLLATE NOCASE,
                DerniereConnexion   TEXT NOT NULL,
                NombreConnexions    INTEGER NOT NULL DEFAULT 1,
                EstActif            INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS AuditLog (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                utilisateur_id  INTEGER REFERENCES Utilisateurs(id),
                action          TEXT NOT NULL,
                details         TEXT,
                horodatage      TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS SessionsCaisse (
                id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                utilisateur_id      INTEGER NOT NULL REFERENCES Utilisateurs(id),
                ouverte_le          TEXT NOT NULL,
                fonds_initial       TEXT NOT NULL,
                fermee_le           TEXT,
                fonds_final         TEXT,
                ecart               TEXT,
                rapport_z_id        INTEGER,
                statut              TEXT NOT NULL DEFAULT 'Ouverte'
                                    CHECK(statut IN ('Ouverte', 'Fermee'))
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_session_caisse_ouverte
                ON SessionsCaisse(statut) WHERE statut = 'Ouverte';

            CREATE TABLE IF NOT EXISTS RapportsZ (
                id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                numero_z            INTEGER NOT NULL UNIQUE,
                session_id          INTEGER NOT NULL REFERENCES SessionsCaisse(id),
                utilisateur_id      INTEGER NOT NULL REFERENCES Utilisateurs(id),
                cree_le             TEXT NOT NULL,
                total_especes       TEXT NOT NULL,
                total_autres        TEXT NOT NULL,
                total_remises       TEXT NOT NULL,
                total_tva           TEXT NOT NULL,
                fonds_initial       TEXT NOT NULL,
                fonds_final         TEXT NOT NULL,
                ecart               TEXT NOT NULL,
                signature_caissier  TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Familles (
                code TEXT PRIMARY KEY,
                nom TEXT NOT NULL,
                description TEXT,
                ordre_affichage INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Clients (
                code TEXT PRIMARY KEY,
                nom TEXT NOT NULL,
                raison_sociale TEXT,
                adresse TEXT,
                ville TEXT,
                code_postal TEXT,
                telephone TEXT,
                email TEXT,
                matricule_fiscal TEXT,
                regime_tva TEXT,
                solde TEXT NOT NULL DEFAULT '0.000',
                limite_credit TEXT NOT NULL DEFAULT '0.000',
                mode_reglement_prefere TEXT
            );

            CREATE TABLE IF NOT EXISTS Fournisseurs (
                code TEXT PRIMARY KEY,
                nom TEXT NOT NULL,
                raison_sociale TEXT,
                adresse TEXT,
                ville TEXT,
                code_postal TEXT,
                telephone TEXT,
                email TEXT,
                matricule_fiscal TEXT,
                conditions_reglement TEXT,
                delai_paiement TEXT,
                solde TEXT NOT NULL DEFAULT '0.000',
                compte_comptable TEXT
            );

            CREATE TABLE IF NOT EXISTS Mouvements (
                numero_piece TEXT NOT NULL,
                date_mouvement TEXT,
                type_mouvement TEXT NOT NULL,
                code_client TEXT,
                code_fournisseur TEXT,
                code_produit TEXT,
                quantite TEXT,
                prix_unitaire TEXT,
                total_ht TEXT,
                tva TEXT,
                total_ttc TEXT,
                mode_reglement TEXT,
                reference_paiement TEXT,
                PRIMARY KEY(numero_piece, code_produit)
            );
        ");

        AjouterColonneSiAbsent("ticket", "z_numero", "INTEGER");
        AjouterColonneSiAbsent("ticket_ligne", "tva_ligne", "TEXT NOT NULL DEFAULT '0.000'");
        AjouterColonneSiAbsent("article", "famille_code", "TEXT REFERENCES famille_article(code)");
        AjouterColonneSiAbsent("article", "description", "TEXT");
        AjouterColonneSiAbsent("article", "imprimante_id", "INTEGER");
        AjouterColonneSiAbsent("Utilisateurs", "DoitChangerMotDePasse", "INTEGER NOT NULL DEFAULT 0");

        Executer(@"
            CREATE TABLE IF NOT EXISTS taux_tva (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                code TEXT UNIQUE NOT NULL,
                valeur TEXT NOT NULL,
                label TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS imprimante (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                nom TEXT UNIQUE NOT NULL,
                type TEXT NOT NULL,
                port TEXT,
                active INTEGER NOT NULL DEFAULT 1
            );
            INSERT OR IGNORE INTO taux_tva(code, valeur, label) VALUES
                ('TVA19', '0.190', '19%'), ('TVA13', '0.130', '13%'),
                ('TVA7', '0.070', '7%'), ('TVA0', '0.000', '0%');
            INSERT OR IGNORE INTO famille_article(code, libelle, ordre, couleur) VALUES
                ('BOISSONS', 'Boissons', 1, '#3B82F6'),
                ('ALIMENTATION', 'Alimentation', 2, '#3B82F6'),
                ('PIZZAS', 'Pizzas', 3, '#3B82F6'),
                ('DIVERS', 'Divers', 99, '#3B82F6');
            INSERT OR IGNORE INTO imprimante(nom, type, port, active) VALUES
                ('Imprimante Caisse', 'Caisse', 'USB', 1),
                ('Imprimante Cuisine', 'Cuisine', 'IP:192.168.1.100', 1),
                ('Imprimante Bar', 'Bar', 'IP:192.168.1.101', 1);");

        // Migration additive : les noms metier sont conserves pour les bases
        // creees par les versions precedentes de l'application.
        AjouterColonneSiAbsent("RapportsZ", "DateCloture", "TEXT");
        AjouterColonneSiAbsent("RapportsZ", "CaisseId", "TEXT NOT NULL DEFAULT '1'");
        AjouterColonneSiAbsent("RapportsZ", "UtilisateurId", "TEXT NOT NULL DEFAULT ''");
        AjouterColonneSiAbsent("RapportsZ", "FondsInitial", "TEXT NOT NULL DEFAULT '0.000'");
        AjouterColonneSiAbsent("RapportsZ", "FondsFinal", "TEXT NOT NULL DEFAULT '0.000'");
        AjouterColonneSiAbsent("RapportsZ", "EcartCaisse", "TEXT NOT NULL DEFAULT '0.000'");
        AjouterColonneSiAbsent("RapportsZ", "TotalVentesTTC", "TEXT NOT NULL DEFAULT '0.000'");
        AjouterColonneSiAbsent("RapportsZ", "TotalEspeces", "TEXT NOT NULL DEFAULT '0.000'");
        AjouterColonneSiAbsent("RapportsZ", "TotalCB", "TEXT NOT NULL DEFAULT '0.000'");
        AjouterColonneSiAbsent("RapportsZ", "TotalCheque", "TEXT NOT NULL DEFAULT '0.000'");
        AjouterColonneSiAbsent("RapportsZ", "TotalTicketResto", "TEXT NOT NULL DEFAULT '0.000'");
        AjouterColonneSiAbsent("RapportsZ", "TotalAvoir", "TEXT NOT NULL DEFAULT '0.000'");
        AjouterColonneSiAbsent("RapportsZ", "MonnaieRendue", "TEXT NOT NULL DEFAULT '0.000'");
        AjouterColonneSiAbsent("RapportsZ", "SignatureCaissier", "TEXT NOT NULL DEFAULT ''");
        AjouterColonneSiAbsent("RapportsZ", "NumeroZ", "INTEGER");
        AjouterColonneSiAbsent("article", "chemin_image", "TEXT");
    }

    private void Executer(string sql)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private void AjouterColonneSiAbsent(string table, string colonne, string type)
    {
        var sql = $"SELECT COUNT(*) FROM pragma_table_info('{table.Replace("'", "''")}') WHERE name = '{colonne.Replace("'", "''")}'";
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = sql;
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        if (count == 0)
        {
            Executer($"ALTER TABLE {table} ADD COLUMN {colonne} {type}");
        }
    }

    private static string D(decimal v) => v.ToString("0.000", CultureInfo.InvariantCulture);

    public SessionCaisse? SessionCaisseOuverte()
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"SELECT id, utilisateur_id, ouverte_le, fonds_initial,
                                   fermee_le, fonds_final, ecart, rapport_z_id, statut
                            FROM SessionsCaisse WHERE statut = 'Ouverte' LIMIT 1";
        using var lecteur = cmd.ExecuteReader();
        if (!lecteur.Read()) return null;
        return LireSession(lecteur);
    }

    public SessionCaisse OuvrirSessionCaisse(UtilisateurSession utilisateur, decimal fondsInitial)
    {
        if (fondsInitial < 0m)
            throw new ArgumentOutOfRangeException(nameof(fondsInitial), "Le fonds initial ne peut pas être négatif.");
        if (SessionCaisseOuverte() is not null)
            throw new InvalidOperationException("Une caisse est déjà ouverte.");

        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"INSERT INTO SessionsCaisse(utilisateur_id, ouverte_le, fonds_initial, statut)
                            VALUES($utilisateur, $ouverteLe, $fondsInitial, 'Ouverte');
                            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$utilisateur", utilisateur.Id);
        cmd.Parameters.AddWithValue("$ouverteLe", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$fondsInitial", D(fondsInitial));
        var id = Convert.ToInt64(cmd.ExecuteScalar());
        var session = SessionCaisseOuverte()!;
        JournaliserAudit(utilisateur, "Ouverture caisse", $"Session {id}, fonds initial {D(fondsInitial)}");
        return session;
    }

    private static SessionCaisse LireSession(Microsoft.Data.Sqlite.SqliteDataReader lecteur) =>
        new(
            lecteur.GetInt64(0),
            lecteur.GetInt64(1),
            lecteur.GetString(2),
            decimal.Parse(lecteur.GetString(3), CultureInfo.InvariantCulture),
            lecteur.IsDBNull(4) ? null : lecteur.GetString(4),
            lecteur.IsDBNull(5) ? null : decimal.Parse(lecteur.GetString(5), CultureInfo.InvariantCulture),
            lecteur.IsDBNull(6) ? null : decimal.Parse(lecteur.GetString(6), CultureInfo.InvariantCulture),
            lecteur.IsDBNull(7) ? null : lecteur.GetInt64(7),
            lecteur.GetString(8));

    public int CompterUtilisateurs()
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Utilisateurs";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public bool CompteUtilisateurExiste(string identifiant)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Utilisateurs WHERE identifiant = $identifiant";
        cmd.Parameters.AddWithValue("$identifiant", identifiant.Trim());
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public IReadOnlyList<UtilisateurAdmin> ListerUtilisateurs()
    {
        var resultats = new List<UtilisateurAdmin>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"SELECT id, identifiant, nom_affiche, role, actif, cree_le
                            FROM Utilisateurs ORDER BY nom_affiche, identifiant";
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            resultats.Add(new UtilisateurAdmin(lecteur.GetInt64(0), lecteur.GetString(1),
                lecteur.GetString(2), lecteur.GetString(3), lecteur.GetInt64(4) == 1,
                lecteur.GetString(5)));
        }
        return resultats;
    }

    public void ModifierUtilisateur(long id, string nomAffiche, string role, bool actif,
        string? nouveauMotDePasse = null)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = nouveauMotDePasse is null
            ? "UPDATE Utilisateurs SET nom_affiche = $nom, role = $role, actif = $actif WHERE id = $id"
            : "UPDATE Utilisateurs SET nom_affiche = $nom, role = $role, actif = $actif, mot_de_passe = $motDePasse, DoitChangerMotDePasse = 0 WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$nom", nomAffiche.Trim());
        cmd.Parameters.AddWithValue("$role", role);
        cmd.Parameters.AddWithValue("$actif", actif ? 1 : 0);
        if (nouveauMotDePasse is not null)
            cmd.Parameters.AddWithValue("$motDePasse", AuthentificationService.HacherMotDePasse(nouveauMotDePasse));
        cmd.ExecuteNonQuery();
    }

    public void SupprimerUtilisateur(long id)
    {
        using var tx = _cnx.BeginTransaction();
        try
        {
            using var sessionCmd = _cnx.CreateCommand();
            sessionCmd.Transaction = tx;
            sessionCmd.CommandText = "SELECT COUNT(*) FROM SessionsCaisse WHERE utilisateur_id = $id";
            sessionCmd.Parameters.AddWithValue("$id", id);
            var sessions = Convert.ToInt32(sessionCmd.ExecuteScalar());

            if (sessions > 0)
            {
                using var desactiver = _cnx.CreateCommand();
                desactiver.Transaction = tx;
                desactiver.CommandText = "UPDATE Utilisateurs SET actif = 0 WHERE id = $id";
                desactiver.Parameters.AddWithValue("$id", id);
                desactiver.ExecuteNonQuery();
            }
            else
            {
                using var audit = _cnx.CreateCommand();
                audit.Transaction = tx;
                audit.CommandText = "UPDATE AuditLog SET utilisateur_id = NULL WHERE utilisateur_id = $id";
                audit.Parameters.AddWithValue("$id", id);
                audit.ExecuteNonQuery();

                using var supprimer = _cnx.CreateCommand();
                supprimer.Transaction = tx;
                supprimer.CommandText = "DELETE FROM Utilisateurs WHERE id = $id";
                supprimer.Parameters.AddWithValue("$id", id);
                supprimer.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public IReadOnlyList<EntreeAudit> ListerAudit(int limite = 500)
    {
        var resultats = new List<EntreeAudit>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"SELECT a.id, a.utilisateur_id, u.identifiant, a.action, a.details, a.horodatage
                            FROM AuditLog a LEFT JOIN Utilisateurs u ON u.id = a.utilisateur_id
                            ORDER BY a.id DESC LIMIT $limite";
        cmd.Parameters.AddWithValue("$limite", limite);
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            resultats.Add(new EntreeAudit(lecteur.GetInt64(0),
                lecteur.IsDBNull(1) ? null : lecteur.GetInt64(1),
                lecteur.IsDBNull(2) ? null : lecteur.GetString(2), lecteur.GetString(3),
                lecteur.IsDBNull(4) ? null : lecteur.GetString(4), lecteur.GetString(5)));
        }
        return resultats;
    }

    public void AjouterUtilisateur(string identifiant, string nomAffiche, string role, string motDePasse,
        bool doitChangerMotDePasse = false)
    {
        var empreinte = AuthentificationService.HacherMotDePasse(motDePasse);
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Utilisateurs(identifiant, nom_affiche, role, mot_de_passe, cree_le, DoitChangerMotDePasse)
            VALUES($identifiant, $nom, $role, $motDePasse, $creeLe, $doitChanger)";
        cmd.Parameters.AddWithValue("$identifiant", identifiant.Trim());
        cmd.Parameters.AddWithValue("$nom", nomAffiche.Trim());
        cmd.Parameters.AddWithValue("$role", role);
        cmd.Parameters.AddWithValue("$motDePasse", empreinte);
        cmd.Parameters.AddWithValue("$creeLe", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$doitChanger", doitChangerMotDePasse ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void DefinirMotDePasse(long id, string motDePasse)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "UPDATE Utilisateurs SET mot_de_passe = $motDePasse, DoitChangerMotDePasse = 0 WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$motDePasse", AuthentificationService.HacherMotDePasse(motDePasse));
        cmd.ExecuteNonQuery();
    }

    public UtilisateurSession CreerOuReinitialiserAdministrateur(string motDePasse,
        bool doitChangerMotDePasse)
    {
        using var tx = _cnx.BeginTransaction();
        try
        {
            long id;
            using (var recherche = _cnx.CreateCommand())
            {
                recherche.Transaction = tx;
                recherche.CommandText = "SELECT id FROM Utilisateurs WHERE identifiant = 'admin' LIMIT 1";
                var existant = recherche.ExecuteScalar();
                if (existant is null || existant is DBNull)
                {
                    using var ajout = _cnx.CreateCommand();
                    ajout.Transaction = tx;
                    ajout.CommandText = @"
                        INSERT INTO Utilisateurs(identifiant, nom_affiche, role, mot_de_passe, cree_le, DoitChangerMotDePasse)
                        VALUES('admin', 'Administrateur', 'Administrateur', $motDePasse, $creeLe, $doitChanger);
                        SELECT last_insert_rowid();";
                    ajout.Parameters.AddWithValue("$motDePasse", AuthentificationService.HacherMotDePasse(motDePasse));
                    ajout.Parameters.AddWithValue("$creeLe", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                    ajout.Parameters.AddWithValue("$doitChanger", doitChangerMotDePasse ? 1 : 0);
                    id = Convert.ToInt64(ajout.ExecuteScalar());
                }
                else
                {
                    id = Convert.ToInt64(existant);
                    using var modification = _cnx.CreateCommand();
                    modification.Transaction = tx;
                    modification.CommandText = @"UPDATE Utilisateurs
                        SET nom_affiche = 'Administrateur', role = 'Administrateur', actif = 1,
                            mot_de_passe = $motDePasse, DoitChangerMotDePasse = $doitChanger
                        WHERE id = $id";
                    modification.Parameters.AddWithValue("$id", id);
                    modification.Parameters.AddWithValue("$motDePasse", AuthentificationService.HacherMotDePasse(motDePasse));
                    modification.Parameters.AddWithValue("$doitChanger", doitChangerMotDePasse ? 1 : 0);
                    modification.ExecuteNonQuery();
                }
            }
            tx.Commit();
            return new UtilisateurSession(id, "admin", "Administrateur", "Administrateur", doitChangerMotDePasse);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public UtilisateurSession GarantirAdministrateurParDefaut()
    {
        using var recherche = _cnx.CreateCommand();
        recherche.CommandText = @"SELECT id, identifiant, nom_affiche, role, DoitChangerMotDePasse
                                  FROM Utilisateurs WHERE identifiant = 'admin' LIMIT 1";
        using var lecteur = recherche.ExecuteReader();
        if (lecteur.Read())
        {
            return new UtilisateurSession(lecteur.GetInt64(0), lecteur.GetString(1),
                lecteur.GetString(2), lecteur.GetString(3), lecteur.GetInt64(4) == 1);
        }

        return CreerOuReinitialiserAdministrateur("123456", true);
    }

    public void CreerSessionCaisseInitialeFermee(long utilisateurId)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"INSERT INTO SessionsCaisse(
                utilisateur_id, ouverte_le, fonds_initial, fermee_le, fonds_final, ecart, statut)
            VALUES($utilisateur, $date, '0.000', $date, '0.000', '0.000', 'Fermee')";
        var date = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        cmd.Parameters.AddWithValue("$utilisateur", utilisateurId);
        cmd.Parameters.AddWithValue("$date", date);
        cmd.ExecuteNonQuery();
    }

    public UtilisateurSession? Authentifier(string identifiant, string motDePasse)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"SELECT id, identifiant, nom_affiche, role, mot_de_passe, DoitChangerMotDePasse
                            FROM Utilisateurs
                            WHERE identifiant = $identifiant AND actif = 1";
        cmd.Parameters.AddWithValue("$identifiant", identifiant.Trim());
        using var lecteur = cmd.ExecuteReader();
        if (!lecteur.Read()) return null;

        var hash = lecteur.GetString(4);
        if (!AuthentificationService.VerifierMotDePasse(motDePasse, hash)) return null;
        var session = new UtilisateurSession(lecteur.GetInt64(0), lecteur.GetString(1),
            lecteur.GetString(2), lecteur.GetString(3), lecteur.GetInt64(5) == 1);
        JournaliserAudit(session, "Connexion");
        return session;
    }

    public IReadOnlyList<HistoriqueConnexion> ListerHistoriqueConnexions(int limite = 10)
    {
        var resultats = new List<HistoriqueConnexion>();
        using (var synchroniser = _cnx.CreateCommand())
        {
            synchroniser.CommandText = @"UPDATE HistoriqueConnexions
                                         SET EstActif = 0
                                         WHERE EstActif = 1
                                           AND EXISTS (
                                               SELECT 1 FROM Utilisateurs u
                                               WHERE u.identifiant = HistoriqueConnexions.NomUtilisateur
                                                 AND u.actif = 0)";
            synchroniser.ExecuteNonQuery();
        }
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"SELECT Id, NomUtilisateur, DerniereConnexion,
                                   NombreConnexions, EstActif
                            FROM (
                                SELECT h.Id, h.NomUtilisateur, h.DerniereConnexion,
                                       h.NombreConnexions, h.EstActif
                                FROM HistoriqueConnexions h
                                WHERE h.EstActif = 1
                                UNION ALL
                                SELECT -u.id, u.identifiant, $maintenant, 0, 1
                                FROM Utilisateurs u
                                LEFT JOIN HistoriqueConnexions h
                                    ON h.NomUtilisateur = u.identifiant
                                WHERE u.actif = 1 AND h.Id IS NULL
                            )
                            ORDER BY DerniereConnexion DESC, NomUtilisateur
                            LIMIT $limite";
        cmd.Parameters.AddWithValue("$limite", Math.Clamp(limite, 0, 10));
        cmd.Parameters.AddWithValue("$maintenant", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            resultats.Add(new HistoriqueConnexion
            {
                Id = lecteur.GetInt32(0),
                NomUtilisateur = lecteur.GetString(1),
                DerniereConnexion = DateTime.Parse(lecteur.GetString(2), CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind),
                NombreConnexions = lecteur.GetInt32(3),
                EstActif = lecteur.GetInt64(4) == 1
            });
        }
        return resultats;
    }

    public void EnregistrerConnexionReussie(string nomUtilisateur)
    {
        var nom = nomUtilisateur.Trim();
        if (nom.Length == 0) throw new ArgumentException("Le nom utilisateur est obligatoire.", nameof(nomUtilisateur));

        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO HistoriqueConnexions(NomUtilisateur, DerniereConnexion, NombreConnexions, EstActif)
            VALUES($nom, $derniereConnexion, 1, 1)
            ON CONFLICT(NomUtilisateur) DO UPDATE SET
                DerniereConnexion = excluded.DerniereConnexion,
                NombreConnexions = HistoriqueConnexions.NombreConnexions + 1,
                EstActif = 1";
        cmd.Parameters.AddWithValue("$nom", nom);
        cmd.Parameters.AddWithValue("$derniereConnexion", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        cmd.ExecuteNonQuery();
    }

    public void DesactiverHistoriqueConnexion(string nomUtilisateur)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "UPDATE HistoriqueConnexions SET EstActif = 0 WHERE NomUtilisateur = $nom";
        cmd.Parameters.AddWithValue("$nom", nomUtilisateur.Trim());
        cmd.ExecuteNonQuery();
    }

    public void EffacerHistoriqueConnexions()
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "UPDATE HistoriqueConnexions SET EstActif = 0";
        cmd.ExecuteNonQuery();
    }

    public void JournaliserAudit(UtilisateurSession? utilisateur, string action, string? details = null)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"INSERT INTO AuditLog(utilisateur_id, action, details, horodatage)
                            VALUES($utilisateur, $action, $details, $horodatage)";
        cmd.Parameters.AddWithValue("$utilisateur", utilisateur?.Id is long id ? id : DBNull.Value);
        cmd.Parameters.AddWithValue("$action", action);
        cmd.Parameters.AddWithValue("$details", details ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$horodatage", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<FamilleArticle> ListerFamilles()
    {
        var liste = new List<FamilleArticle>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "SELECT code, libelle, ordre, couleur FROM famille_article ORDER BY ordre, libelle";
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            liste.Add(new FamilleArticle(
                lecteur.GetString(0),
                lecteur.GetString(1),
                lecteur.GetInt32(2),
                lecteur.GetString(3)));
        }
        return liste;
    }

    public void AjouterFamille(FamilleArticle famille)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO famille_article(code, libelle, ordre, couleur)
            VALUES($code, $libelle, $ordre, $couleur)";
        cmd.Parameters.AddWithValue("$code", famille.Code);
        cmd.Parameters.AddWithValue("$libelle", famille.Libelle);
        cmd.Parameters.AddWithValue("$ordre", famille.Ordre);
        cmd.Parameters.AddWithValue("$couleur", famille.Couleur);
        cmd.ExecuteNonQuery();
    }

    public int CompterArticlesFamille(string code)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM article WHERE famille_code = $code";
        cmd.Parameters.AddWithValue("$code", code);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void SupprimerFamille(string code)
    {
        if (CompterArticlesFamille(code) > 0)
            throw new InvalidOperationException("Cette famille contient encore des produits.");

        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "DELETE FROM famille_article WHERE code = $code";
        cmd.Parameters.AddWithValue("$code", code);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Insère la famille si son code est absent, sans écraser une famille
    /// existante de même code (contrairement à AjouterFamille qui remplace).
    /// </summary>
    public void AjouterFamilleSiAbsente(FamilleArticle famille)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"
            INSERT OR IGNORE INTO famille_article(code, libelle, ordre, couleur)
            VALUES($code, $libelle, $ordre, $couleur)";
        cmd.Parameters.AddWithValue("$code", famille.Code);
        cmd.Parameters.AddWithValue("$libelle", famille.Libelle);
        cmd.Parameters.AddWithValue("$ordre", famille.Ordre);
        cmd.Parameters.AddWithValue("$couleur", famille.Couleur);
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<Article> ListerArticles()
    {
        var liste = new List<Article>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"SELECT a.code, a.designation, a.prix_ttc, a.taux_tva,
                        a.famille_code, a.imprimante_id, i.nom, a.chemin_image
                            FROM article a LEFT JOIN imprimante i ON i.id = a.imprimante_id
                            ORDER BY a.designation";
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            liste.Add(new Article(
                lecteur.GetString(0),
                lecteur.GetString(1),
                decimal.Parse(lecteur.GetString(2), CultureInfo.InvariantCulture),
                decimal.Parse(lecteur.GetString(3), CultureInfo.InvariantCulture))
            {
                FamilleCode = lecteur.IsDBNull(4) ? null : lecteur.GetString(4),
                ImprimanteId = lecteur.IsDBNull(5) ? null : lecteur.GetInt32(5),
                NomImprimante = lecteur.IsDBNull(6) ? null : lecteur.GetString(6),
                CheminImage = lecteur.IsDBNull(7) ? null : lecteur.GetString(7),
            });
        }
        return liste;
    }

    /// <summary>Catalogue filtré utilisé par l'écran de caisse tactile.</summary>
    public IReadOnlyList<Article> ChargerArticlesParFamille(string? familleCode, string? recherche = null)
    {
        var articles = ListerArticles().Where(a => familleCode is null || a.FamilleCode == familleCode);
        if (!string.IsNullOrWhiteSpace(recherche))
        {
            articles = articles.Where(a => a.Code.Contains(recherche, StringComparison.OrdinalIgnoreCase)
                || a.Designation.Contains(recherche, StringComparison.OrdinalIgnoreCase));
        }
        return articles.ToList();
    }

    public IReadOnlyList<FamilleArticle> ChargerFamilles() => ListerFamilles();

    public IReadOnlyList<Article> ListerArticlesParFamille(string? familleCode)
    {
        var liste = new List<Article>();
        using var cmd = _cnx.CreateCommand();
        if (familleCode == null)
        {
            cmd.CommandText = "SELECT code, designation, prix_ttc, taux_tva, chemin_image FROM article ORDER BY designation";
        }
        else
        {
            cmd.CommandText = "SELECT code, designation, prix_ttc, taux_tva, chemin_image FROM article WHERE famille_code = $familleCode ORDER BY designation";
            cmd.Parameters.AddWithValue("$familleCode", familleCode);
        }

        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            liste.Add(new Article(
                lecteur.GetString(0),
                lecteur.GetString(1),
                decimal.Parse(lecteur.GetString(2), CultureInfo.InvariantCulture),
                decimal.Parse(lecteur.GetString(3), CultureInfo.InvariantCulture))
            {
                CheminImage = lecteur.IsDBNull(4) ? null : lecteur.GetString(4),
            });
        }
        return liste;
    }

    public void AjouterArticle(Article article, string? familleCode = null)
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO article(code, designation, prix_ttc, taux_tva, famille_code)
            VALUES($code, $designation, $prixTtc, $tauxTva, $familleCode)";
        cmd.Parameters.AddWithValue("$code", article.Code);
        cmd.Parameters.AddWithValue("$designation", article.Designation);
        cmd.Parameters.AddWithValue("$prixTtc", D(article.PrixTtc));
        cmd.Parameters.AddWithValue("$tauxTva", D(article.TauxTva));
        cmd.Parameters.AddWithValue("$familleCode", familleCode ?? (object)DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public int DernierNumeroTicket()
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(numero), 0) FROM ticket";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void EnregistrerTicket(int numero, CaisseService caisse, IReadOnlyList<Reglement>? reglements = null)
    {
        using var tx = _cnx.BeginTransaction();
        try
        {
            var dateHeure = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            using (var cmd = _cnx.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO ticket(numero, date_heure, total_ttc, total_ht, total_tva)
                    VALUES($numero, $dateHeure, $totalTtc, $totalHt, $totalTva)";
                cmd.Parameters.AddWithValue("$numero", numero);
                cmd.Parameters.AddWithValue("$dateHeure", dateHeure);
                cmd.Parameters.AddWithValue("$totalTtc", D(caisse.TotalTtc));
                cmd.Parameters.AddWithValue("$totalHt", D(caisse.TotalHt));
                cmd.Parameters.AddWithValue("$totalTva", D(caisse.TotalTva));
                cmd.ExecuteNonQuery();
            }

            var position = 0;
            foreach (var ligne in caisse.Lignes)
            {
                using var cmd = _cnx.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO ticket_ligne(ticket_numero, position, code_article, designation,
                                             prix_ttc, taux_tva, quantite, remise_pct, total_ligne, tva_ligne)
                    VALUES($ticketNumero, $position, $codeArticle, $designation, $prixTtc, $tauxTva, $quantite,
                           $remisePct, $totalLigne, $tvaLigne)";
                cmd.Parameters.AddWithValue("$ticketNumero", numero);
                cmd.Parameters.AddWithValue("$position", ++position);
                cmd.Parameters.AddWithValue("$codeArticle", ligne.Article.Code);
                cmd.Parameters.AddWithValue("$designation", ligne.Article.Designation);
                cmd.Parameters.AddWithValue("$prixTtc", D(ligne.Article.PrixTtc));
                cmd.Parameters.AddWithValue("$tauxTva", D(ligne.Article.TauxTva));
                cmd.Parameters.AddWithValue("$quantite", D(ligne.Quantite));
                cmd.Parameters.AddWithValue("$remisePct", D(ligne.RemisePct));
                cmd.Parameters.AddWithValue("$totalLigne", D(ligne.TotalLigne));
                cmd.Parameters.AddWithValue("$tvaLigne", D(ligne.TvaLigne));
                cmd.ExecuteNonQuery();
            }

            var posRegl = 0;
            foreach (var reglement in reglements ?? Array.Empty<Reglement>())
            {
                using var cmd = _cnx.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO reglement_ticket(ticket_numero, position, mode, montant)
                    VALUES($ticketNumero, $position, $mode, $montant)";
                cmd.Parameters.AddWithValue("$ticketNumero", numero);
                cmd.Parameters.AddWithValue("$position", ++posRegl);
                cmd.Parameters.AddWithValue("$mode", ModesReglement.Code(reglement.Mode));
                cmd.Parameters.AddWithValue("$montant", D(reglement.Montant));
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

    public IReadOnlyList<Reglement> ReglementsDunTicket(int numero)
    {
        var liste = new List<Reglement>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"
            SELECT mode, montant FROM reglement_ticket
            WHERE ticket_numero = $ticketNumero ORDER BY position";
        cmd.Parameters.AddWithValue("$ticketNumero", numero);
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            liste.Add(new Reglement(
                ModesReglement.DepuisCode(lecteur.GetString(0)),
                decimal.Parse(lecteur.GetString(1), CultureInfo.InvariantCulture)));
        }
        return liste;
    }

    /// <summary>Familles du jeu de démonstration, partagées par CreerDemo.</summary>
    private static readonly FamilleArticle[] FamillesDemo =
    {
        new("BOISSONS", "Boissons", 1, "#3B82F6"),
        new("BOULANGERIE", "Boulangerie", 2, "#F59E0B"),
        new("SANDWICHS", "Sandwichs & Plats", 3, "#10B981"),
        new("DIVERS", "Divers", 99, "#6B7280"),
    };

    public static BaseDonnees CreerDemo(string chemin)
    {
        var bdd = new BaseDonnees(chemin);
        if (bdd.ListerFamilles().Count == 0)
        {
            foreach (var famille in FamillesDemo)
                bdd.AjouterFamille(famille);
        }

        if (bdd.ListerArticles().Count == 0)
        {
            // Les articles de démonstration référencent ces familles :
            // garantir leur existence même si la base contient déjà d'autres
            // familles (RAZ, migration) — sinon la contrainte de clé
            // étrangère fait planter l'ouverture de la caisse (bouton Caisse F1).
            foreach (var famille in FamillesDemo)
                bdd.AjouterFamilleSiAbsente(famille);

            bdd.AjouterArticle(new("P001", "Baguette tradition", 0.250m, 0.070m), "BOULANGERIE");
            bdd.AjouterArticle(new("P002", "Croissant", 0.600m, 0.130m), "BOULANGERIE");
            bdd.AjouterArticle(new("P003", "Pain au chocolat", 0.900m, 0.130m), "BOULANGERIE");
            bdd.AjouterArticle(new("P004", "Eau 1L", 0.500m, 0.190m), "BOISSONS");
            bdd.AjouterArticle(new("P005", "Café express", 1.200m, 0.190m), "BOISSONS");
            bdd.AjouterArticle(new("P006", "Thé", 1.000m, 0.190m), "BOISSONS");
            bdd.AjouterArticle(new("P007", "Jus orange", 1.500m, 0.190m), "BOISSONS");
            bdd.AjouterArticle(new("P008", "Sandwich thon", 2.500m, 0.190m), "SANDWICHS");
            bdd.AjouterArticle(new("P009", "Sandwich poulet", 2.800m, 0.190m), "SANDWICHS");
            bdd.AjouterArticle(new("P010", "Pizza margherita", 4.500m, 0.190m), "SANDWICHS");
            bdd.AjouterArticle(new("P011", "Eau gazeuse 1L", 0.700m, 0.190m), "BOISSONS");
            bdd.AjouterArticle(new("P012", "Sac plastique", 0.050m, 0.190m), "DIVERS");
        }

        return bdd;
    }

    public record LigneTicketLue(
        string Designation, decimal Quantite, decimal PrixTtc,
        decimal RemisePct, decimal TotalLigne, decimal TvaLigne);

    public IReadOnlyList<LigneTicketLue> LignesDunTicket(int numero)
    {
        var lignes = new List<LigneTicketLue>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"
            SELECT designation, quantite, prix_ttc, remise_pct, total_ligne, tva_ligne
            FROM ticket_ligne WHERE ticket_numero = $ticketNumero ORDER BY position";
        cmd.Parameters.AddWithValue("$ticketNumero", numero);
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

    public IReadOnlyList<(decimal Taux, decimal Ht, decimal Tva)> RecapTvaDunTicket(int numero)
    {
        var accumulateurs = new Dictionary<decimal, (decimal Ttc, decimal Tva)>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "SELECT taux_tva, total_ligne, tva_ligne FROM ticket_ligne WHERE ticket_numero = $ticketNumero";
        cmd.Parameters.AddWithValue("$ticketNumero", numero);
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            var taux = decimal.Parse(lecteur.GetString(0), CultureInfo.InvariantCulture);
            var ttc = decimal.Parse(lecteur.GetString(1), CultureInfo.InvariantCulture);
            var tva = decimal.Parse(lecteur.GetString(2), CultureInfo.InvariantCulture);
            var (ttcTotal, tvaTotal) = accumulateurs.TryGetValue(taux, out var acc) ? acc : (0m, 0m);
            accumulateurs[taux] = (ttcTotal + ttc, tvaTotal + tva);
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
        cmd.CommandText = "SELECT total_ttc FROM ticket WHERE numero = $numero";
        cmd.Parameters.AddWithValue("$numero", numero);
        var brut = cmd.ExecuteScalar() as string ?? "0.000";
        return decimal.Parse(brut, CultureInfo.InvariantCulture);
    }

    public int DernierNumeroZ()
    {
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(numero), 0) FROM cloture_z";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void EnregistrerRapportZ(RapportZ rapport, SessionCaisse session, UtilisateurSession utilisateur)
    {
        var date = rapport.DateCloture.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        using var tx = _cnx.BeginTransaction();
        try
        {
            using var cmd = _cnx.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO RapportsZ(
                    numero_z, NumeroZ, session_id, utilisateur_id, cree_le, DateCloture,
                    CaisseId, UtilisateurId, total_especes, total_autres, total_remises,
                    total_tva, fonds_initial, FondsInitial, fonds_final, FondsFinal,
                    ecart, EcartCaisse, TotalVentesTTC, TotalEspeces, TotalCB,
                    TotalCheque, TotalTicketResto, TotalAvoir, MonnaieRendue,
                    signature_caissier, SignatureCaissier)
                VALUES($numero, $numero, $session, $utilisateur, $date, $date,
                    $caisse, $utilisateurTexte, $especes, $autres, '0.000', $tva,
                    $initial, $initial, $final, $final, $ecart, $ecart, $ventes,
                    $especes, $cb, $cheque, $ticketResto, $avoir, $rendu, $signature, $signature);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$numero", rapport.NumeroZ);
            cmd.Parameters.AddWithValue("$session", session.Id);
            cmd.Parameters.AddWithValue("$utilisateur", utilisateur.Id);
            cmd.Parameters.AddWithValue("$date", date);
            cmd.Parameters.AddWithValue("$caisse", rapport.CaisseId);
            cmd.Parameters.AddWithValue("$utilisateurTexte", rapport.UtilisateurId);
            cmd.Parameters.AddWithValue("$especes", D(rapport.TotalEspeces));
            cmd.Parameters.AddWithValue("$autres", D(rapport.TotalCB + rapport.TotalCheque + rapport.TotalTicketResto + rapport.TotalAvoir));
            cmd.Parameters.AddWithValue("$tva", D(rapport.TotalTva));
            cmd.Parameters.AddWithValue("$initial", D(rapport.FondsInitial));
            cmd.Parameters.AddWithValue("$final", D(rapport.FondsFinal));
            cmd.Parameters.AddWithValue("$ecart", D(rapport.EcartCaisse));
            cmd.Parameters.AddWithValue("$ventes", D(rapport.TotalVentesTTC));
            cmd.Parameters.AddWithValue("$cb", D(rapport.TotalCB));
            cmd.Parameters.AddWithValue("$cheque", D(rapport.TotalCheque));
            cmd.Parameters.AddWithValue("$ticketResto", D(rapport.TotalTicketResto));
            cmd.Parameters.AddWithValue("$avoir", D(rapport.TotalAvoir));
            cmd.Parameters.AddWithValue("$rendu", D(rapport.MonnaieRendue));
            cmd.Parameters.AddWithValue("$signature", rapport.SignatureCaissier);
            rapport.Id = Convert.ToInt32(cmd.ExecuteScalar());

            using var sessionCmd = _cnx.CreateCommand();
            sessionCmd.Transaction = tx;
            sessionCmd.CommandText = @"UPDATE SessionsCaisse
                SET fermee_le = $date, fonds_final = $final, ecart = $ecart,
                    rapport_z_id = $rapport, statut = 'Fermee' WHERE id = $session";
            sessionCmd.Parameters.AddWithValue("$date", date);
            sessionCmd.Parameters.AddWithValue("$final", D(rapport.FondsFinal));
            sessionCmd.Parameters.AddWithValue("$ecart", D(rapport.EcartCaisse));
            sessionCmd.Parameters.AddWithValue("$rapport", rapport.Id);
            sessionCmd.Parameters.AddWithValue("$session", session.Id);
            sessionCmd.ExecuteNonQuery();
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        JournaliserAudit(utilisateur, "ClotureCaisse",
            $"Z {rapport.NumeroZ}, ecart {D(rapport.EcartCaisse)}, session {session.Id}");
    }

    public IReadOnlyList<LigneZTva> RecapTvaTicketsNonClotures()
    {
        var accumulateurs = new Dictionary<decimal, (decimal Ttc, decimal Tva)>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"
            SELECT tl.taux_tva, tl.total_ligne, tl.tva_ligne
            FROM ticket_ligne tl
            JOIN ticket t ON t.numero = tl.ticket_numero
            WHERE t.z_numero IS NULL
            ORDER BY tl.taux_tva DESC";
        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            var taux = decimal.Parse(lecteur.GetString(0), CultureInfo.InvariantCulture);
            var ttc = decimal.Parse(lecteur.GetString(1), CultureInfo.InvariantCulture);
            var tva = decimal.Parse(lecteur.GetString(2), CultureInfo.InvariantCulture);
            var (ttcTotal, tvaTotal) = accumulateurs.TryGetValue(taux, out var acc) ? acc : (0m, 0m);
            accumulateurs[taux] = (ttcTotal + ttc, tvaTotal + tva);
        }

        return accumulateurs.OrderByDescending(k => k.Key)
            .Select(k => new LigneZTva(
                k.Key,
                decimal.Round(k.Value.Ttc - k.Value.Tva, 3, MidpointRounding.AwayFromZero),
                k.Value.Tva))
            .ToList();
    }

    public IReadOnlyList<LigneZMode> RecapParModeNonClotures()
    {
        var accumulateurs = new SortedDictionary<ModeReglement, decimal>();
        using var cmd = _cnx.CreateCommand();
        cmd.CommandText = @"
            SELECT rt.mode, rt.montant
            FROM reglement_ticket rt
            JOIN ticket t ON t.numero = rt.ticket_numero
            WHERE t.z_numero IS NULL";

        using var lecteur = cmd.ExecuteReader();
        while (lecteur.Read())
        {
            var mode = ModesReglement.DepuisCode(lecteur.GetString(0));
            var montant = decimal.Parse(lecteur.GetString(1), CultureInfo.InvariantCulture);
            accumulateurs[mode] = accumulateurs.TryGetValue(mode, out var s) ? s + montant : montant;
        }

        return accumulateurs.Select(k => new LigneZMode(k.Key, k.Value)).ToList();
    }

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
            totalHt += decimal.Parse(lecteur.GetString(2), CultureInfo.InvariantCulture);
            totalTva += decimal.Parse(lecteur.GetString(3), CultureInfo.InvariantCulture);
        }

        return new RecapZ(
            DernierNumeroZ() + 1,
            DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            nbTickets, totalTtc, totalHt, totalTva, lignes, RecapParModeNonClotures());
    }

    public void CloturerZ(RecapZ z)
    {
        if (z.Numero != DernierNumeroZ() + 1)
            throw new InvalidOperationException($"numérotation Z non continue : attendu {DernierNumeroZ() + 1}, reçu {z.Numero}");
        if (z.NbTickets == 0)
            throw new InvalidOperationException("aucun ticket à clôturer");

        using var tx = _cnx.BeginTransaction();
        try
        {
            using (var cmd = _cnx.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO cloture_z(numero, date_heure, nb_tickets, total_ttc, total_ht, total_tva)
                    VALUES($numero, $dateHeure, $nbTickets, $totalTtc, $totalHt, $totalTva)";
                cmd.Parameters.AddWithValue("$numero", z.Numero);
                cmd.Parameters.AddWithValue("$dateHeure", z.DateHeure);
                cmd.Parameters.AddWithValue("$nbTickets", z.NbTickets);
                cmd.Parameters.AddWithValue("$totalTtc", D(z.TotalTtc));
                cmd.Parameters.AddWithValue("$totalHt", D(z.TotalHt));
                cmd.Parameters.AddWithValue("$totalTva", D(z.TotalTva));
                cmd.ExecuteNonQuery();
            }

            foreach (var ligne in z.LignesTva)
            {
                using var cmd = _cnx.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO cloture_z_tva(z_numero, taux_tva, total_ht, total_tva, total_ttc)
                    VALUES($zNumero, $tauxTva, $totalHt, $totalTva, $totalTtc)";
                cmd.Parameters.AddWithValue("$zNumero", z.Numero);
                cmd.Parameters.AddWithValue("$tauxTva", D(ligne.TauxTva));
                cmd.Parameters.AddWithValue("$totalHt", D(ligne.TotalHt));
                cmd.Parameters.AddWithValue("$totalTva", D(ligne.TotalTva));
                cmd.Parameters.AddWithValue("$totalTtc", D(ligne.TotalTtc));
                cmd.ExecuteNonQuery();
            }

            using (var cmd = _cnx.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "UPDATE ticket SET z_numero = $zNumero WHERE z_numero IS NULL";
                cmd.Parameters.AddWithValue("$zNumero", z.Numero);
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
