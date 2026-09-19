using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Media;
using Postec.Caisse.Caisse;
using Postec.Caisse.Themes;
using Postec.Caisse.Views;

namespace Postec.Caisse;

public partial class App : Application
{
    private static readonly bool ConnexionDesactiveeProvisoirement = false;
    private static readonly SoundPlayer SonClic = new(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bip.wav"));
    public static Theme ThemeCourant { get; private set; } = new();
    public static Brand BrandCourant { get; private set; } = new();
    public static UtilisateurSession? SessionCourante { get; private set; }
    public static string CheminBase =>
        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "postec_demo.db");

    protected override void OnStartup(StartupEventArgs e)
    {
        EventManager.RegisterClassHandler(
            typeof(Button),
            Button.ClickEvent,
            new RoutedEventHandler(SurClicBouton));

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            JournaliserCrash(exception);
            MessageBox.Show(
                $"Erreur critique : {exception?.Message ?? args.ExceptionObject}",
                "Crash", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (_, args) =>
        {
            JournaliserCrash(args.Exception);
            MessageBox.Show(
                $"Erreur UI : {args.Exception.Message}",
                "Crash", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // CP-863 pour l'ESC/POS (accents français sur ticket 80 mm).
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // Mode atelier : génère un ticket de test, l'envoie et s'arrête.
        // Usage : Postec.Caisse --test-impression [nom-imprimante]
        if (e.Args.Contains("--test-impression"))
        {
            TesterImpression();
            Shutdown();
            return;
        }
        // Rebranding : l'identité affichée vient uniquement de Brand/Theme.
        BrandCourant = Brand.Charger();

        try
        {
            ThemeCourant = Theme.Charger("themes");
        }
        catch (Exception ex)
        {
            // Fallback : thème par défaut si le dossier themes est absent/corrompu
            ThemeCourant = new Theme();
            File.AppendAllText(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "postec_demarrage.log"),
                $"{DateTime.Now:O}: Thème par défaut utilisé (erreur chargement) : {ex}{Environment.NewLine}");
        }

        var ressources = Current.Resources;

        // Vérification défensive : chaque ressource doit exister avant d'être utilisée
        void DefinirRessource(string cle, string valeurHex)
        {
            try
            {
                ressources[cle] = Brosse(valeurHex);
            }
            catch (Exception ex)
            {
                ressources[cle] = System.Windows.Media.Brushes.Gray;
                File.AppendAllText(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "postec_demarrage.log"),
                    $"{DateTime.Now:O}: Ressource {cle} en fallback (valeur={valeurHex}) : {ex}{Environment.NewLine}");
            }
        }

        DefinirRessource("CouleurPrimaire", ThemeCourant.Primaire ?? "#1E3A8A");
        DefinirRessource("CouleurPrimaireSombre", ThemeCourant.PrimaireSombre ?? "#1E40AF");
        DefinirRessource("CouleurFond", ThemeCourant.Fond ?? "#F3F4F6");
        DefinirRessource("CouleurSurface", ThemeCourant.Surface ?? "#FFFFFF");
        DefinirRessource("CouleurTexte", ThemeCourant.Texte ?? "#111827");
        DefinirRessource("CouleurTexteSecondaire", ThemeCourant.TexteSecondaire ?? "#6B7280");
        DefinirRessource("CouleurAlerte", ThemeCourant.Alerte ?? "#EF4444");
        DefinirRessource("CouleurSucces", ThemeCourant.Succes ?? "#10B981");
        DefinirRessource("CouleurToucheFond", ThemeCourant.ToucheFond ?? "#E5E7EB");
        DefinirRessource("CouleurToucheFondActif", ThemeCourant.ToucheFondActif ?? "#D1D5DB");
        DefinirRessource("CouleurToucheTexte", ThemeCourant.ToucheTexte ?? "#111827");

        try
        {
            ressources["FamillePolice"] = new System.Windows.Media.FontFamily(
                ThemeCourant.FamillePolice ?? "Segoe UI");
        }
        catch
        {
            ressources["FamillePolice"] = new System.Windows.Media.FontFamily("Segoe UI");
        }

        ressources["TaillePoliceClavier"] = ThemeCourant.TailleClavier;

        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            LoginWindow? login = null;
            if (!ConnexionDesactiveeProvisoirement)
            {
                // Toujours créer une nouvelle fenêtre modale : une instance WPF
                // fermée ne doit jamais être réactivée ou réutilisée.
                login = new LoginWindow(CheminBase);
                if (login.ShowDialog() != true || login.Session is null)
                {
                    Shutdown();
                    return;
                }
            }

            SessionCourante = login?.Session;
            if (SessionCourante is null)
            {
                using var bddInitialisation = new BaseDonnees(CheminBase);
                var admin = bddInitialisation.GarantirAdministrateurParDefaut();
                SessionCourante = admin with { DoitChangerMotDePasse = false };
            }
            using (var bdd = new BaseDonnees(CheminBase))
            {
                if (SessionCourante.DoitChangerMotDePasse)
                {
                    // Le LoginWindow est déjà fermé quand ShowDialog() retourne :
                    // le définir comme Owner lève « Window qui a été fermé ».
                    var changement = new ChangerMotDePasseModal(bdd, SessionCourante);
                    if (changement.ShowDialog() != true)
                    {
                        Shutdown();
                        return;
                    }
                    SessionCourante = SessionCourante with { DoitChangerMotDePasse = false };
                }

                if (bdd.SessionCaisseOuverte() is null)
                {
                    var ouverture = new OuvertureCaisseWindow(bdd, SessionCourante);
                    if (ouverture.ShowDialog() != true)
                    {
                        Shutdown();
                        return;
                    }
                }
            }

            if (SessionCourante.EstAdministrateur)
            {
                var accueil = new AccueilWindow();
                MainWindow = accueil;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                accueil.Show();
            }
            else
            {
                var caisse = new MainWindow();
                MainWindow = caisse;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                caisse.Show();
            }
        }
        catch (Exception ex)
        {
            var journal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "postec_demarrage.log");
            File.AppendAllText(journal,
                $"{DateTime.Now:O}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
            MessageBox.Show(
                $"POSTEC n'a pas pu démarrer.\n\n{ex.Message}\n\nJournal : {journal}",
                "POSTEC - Erreur de démarrage", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static void SurClicBouton(object sender, RoutedEventArgs e)
    {
        SonClic.Play();
    }

    private static void JournaliserCrash(Exception? exception)
    {
        try
        {
            var journal = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
            File.AppendAllText(
                journal,
                $"{DateTime.Now:O}: {exception?.ToString() ?? "Exception inconnue"}{Environment.NewLine}");
        }
        catch
        {
            // Une erreur de journalisation ne doit pas masquer l'exception initiale.
        }
    }

    /// <summary>
    /// Vérification d'impression sans passer par la caisse : génère un ticket
    /// de démonstration et l'envoie (winspool ou fichier imprimante.logique).
    /// </summary>
    private static void TesterImpression()
    {
        var z = Array.IndexOf(Environment.GetCommandLineArgs(), "--test-impression");
        var imprimante = z >= 0 && Environment.GetCommandLineArgs().Length > z + 1
            ? Environment.GetCommandLineArgs()[z + 1] : null;

        var donnees = new DonneesTicket
        {
            EnTete = Brand.Charger().Nom,
            SousTitre = "Ticket de test",
            Adresse = "Avenue Habib Bourguiba — Tunis",
            MentionsLegales = new[] { "MF : 1234567/A/M/000", "Merci de votre visite !" },
            NumeroTicket = 999,
            Lignes = new LigneTicketImpression[]
            {
                new("Café express", 2m, 1.200m, 2.400m),
                new("Baguette tradition", 1m, 0.250m, 0.250m),
                new("Sandwich thon", 1m, 2.500m, 2.500m),
            },
            RecapTva = new LigneRecapTvaImpression[]
            {
                new(19m, 4.118m, 0.782m),
                new(7m, 0.234m, 0.016m),
            },
            TotalTtc = 5.150m,
        };

        var flux = TicketEscPos.Generer(donnees);
        var resultat = ImprimanteBrute.Envoyer(flux, imprimante);
        MessageBox.Show(
            $"Test d'impression : {resultat.Canal}\n{resultat.Erreur ?? "OK"}",
            "POSTEC", MessageBoxButton.OK,
            resultat.Succes ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    /// <summary>Convertit « #RRGGBB » en pinceau gelé (Background/Foreground WPF).</summary>
    private static System.Windows.Media.SolidColorBrush Brosse(string hex)
    {
        try
        {
            var brosse = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
            brosse.Freeze();
            return brosse;
        }
        catch (FormatException)
        {
            return System.Windows.Media.Brushes.Gray;
        }
    }
}
