using System.Windows;
using Postek.Caisse.Caisse;
using Postek.Caisse.Themes;

namespace Postek.Caisse;

public partial class App : Application
{
    public static Theme ThemeCourant { get; private set; } = new();
    public static Brand BrandCourant { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        // CP-863 pour l'ESC/POS (accents français sur ticket 80 mm).
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // Mode atelier : génère un ticket de test, l'envoie et s'arrête.
        // Usage : Postek.Caisse --test-impression [nom-imprimante]
        if (e.Args.Contains("--test-impression"))
        {
            TesterImpression();
            Shutdown();
            return;
        }
        // Rebranding : l'identité affichée vient uniquement de Brand/Theme.
        BrandCourant = Brand.Charger();
        ThemeCourant = Theme.Charger("themes");

        var ressources = Current.Resources;
        ressources["CouleurPrimaire"] = Brosse(ThemeCourant.Primaire);
        ressources["CouleurPrimaireSombre"] = Brosse(ThemeCourant.PrimaireSombre);
        ressources["CouleurFond"] = Brosse(ThemeCourant.Fond);
        ressources["CouleurSurface"] = Brosse(ThemeCourant.Surface);
        ressources["CouleurTexte"] = Brosse(ThemeCourant.Texte);
        ressources["CouleurTexteSecondaire"] = Brosse(ThemeCourant.TexteSecondaire);
        ressources["CouleurAlerte"] = Brosse(ThemeCourant.Alerte);
        ressources["CouleurSucces"] = Brosse(ThemeCourant.Succes);
        ressources["CouleurToucheFond"] = Brosse(ThemeCourant.ToucheFond);
        ressources["CouleurToucheFondActif"] = Brosse(ThemeCourant.ToucheFondActif);
        ressources["CouleurToucheTexte"] = Brosse(ThemeCourant.ToucheTexte);
        ressources["FamillePolice"] = new System.Windows.Media.FontFamily(ThemeCourant.FamillePolice);
        ressources["TaillePoliceClavier"] = ThemeCourant.TailleClavier;

        base.OnStartup(e);
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
            "POSTEK", MessageBoxButton.OK,
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
