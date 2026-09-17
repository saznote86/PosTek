using Postek.Caisse.Caisse;
using Xunit;

namespace Postek.Caisse.Tests;

/// <summary>
/// Tests du générateur ESC/POS : structure du flux (init, découpe), largeur
/// 48 colonnes, accents CP-863, alignement deux colonnes, récap TVA,
/// multi-règlement + rendu monnaie en pied de ticket.
/// </summary>
public class TicketEscPosTests
{
    static TicketEscPosTests()
    {
        // CP-863 : même enregistrement qu'App.OnStartup (provider pages 1252/850…).
        System.Text.Encoding.RegisterProvider(
            System.Text.CodePagesEncodingProvider.Instance);
    }
    /// <summary>
    /// Données de base d'un ticket ; les paramètres optionnels remplacent
    /// uniquement ce que le test veut préciser (propriétés init — pas de with).
    /// </summary>
    private static DonneesTicket Ticket(
        LigneTicketImpression[]? lignes = null,
        LigneRecapTvaImpression[]? recapTva = null,
        LigneReglementImpression[]? reglements = null,
        decimal monnaieRendue = 0m,
        decimal totalTtc = 2.400m)
    {
        return new DonneesTicket
        {
            NumeroTicket = 12,
            TotalTtc = totalTtc,
            Lignes = lignes ?? new LigneTicketImpression[]
            {
                new("Café express", 2m, 1.200m, 2.400m),
            },
            RecapTva = recapTva ?? Array.Empty<LigneRecapTvaImpression>(),
            Reglements = reglements ?? Array.Empty<LigneReglementImpression>(),
            MonnaieRendue = monnaieRendue,
        };
    }

    // ------------------------------------------------------------------
    // Structure du flux
    // ------------------------------------------------------------------
    [Fact]
    public void Flux_DemarreParInitEtTermineParDecoupe()
    {
        var flux = TicketEscPos.Generer(Ticket());

        Assert.True(flux.Length > 2);
        Assert.Equal(0x1B, flux[0]);              // ESC
        Assert.Equal((byte)'@', flux[1]);         // init

        // Découpe partielle : ESC d 3 — les 3 derniers octets du flux.
        var n = flux.Length;
        Assert.Equal(0x1B, flux[n - 3]);
        Assert.Equal((byte)'d', flux[n - 2]);
        Assert.Equal((byte)3, flux[n - 1]);
    }

    [Fact]
    public void Flux_ContientNumeroTicketEtTotal()
    {
        var flux = TicketEscPos.Generer(Ticket());

        var texte = Decode(flux).Texte;
        Assert.Contains("Ticket n° 12", texte);
        Assert.Contains("TOTAL", texte);
        Assert.Contains("2,400", texte);
    }

    // ------------------------------------------------------------------
    // Largeur 80 mm : police A = 48 colonnes
    // ------------------------------------------------------------------
    [Fact]
    public void LignesTexto_NeDepassentPas48Colonnes()
    {
        var donnees = Ticket(
            lignes: new LigneTicketImpression[]
            {
                new("Désignation très longue qui devrait être tronquée proprement", 1m, 9.999m, 9.999m),
            },
            recapTva: new LigneRecapTvaImpression[] { new(19m, 8.399m, 1.600m) });

        var texte = Decode(TicketEscPos.Generer(donnees)).Texte;

        foreach (var ligne in texte.Split('\n'))
        {
            var visible = ligne.TrimEnd('\r').Length;
            // Les lignes double-largeur comptent double en colonnes imprimées ;
            // en texte brut on borne simplement par la largeur physique.
            Assert.True(visible <= TicketEscPos.ColonnesPoliceA,
                $"ligne trop large ({visible}) : « {ligne} »");
        }
    }

    // ------------------------------------------------------------------
    // Accents : CP-863, jamais d'octet invalide, repli '?'
    // ------------------------------------------------------------------
    [Fact]
    public void Accents_EncodesCP863_SansOctetInvalide()
    {
        var donnees = Ticket(
            lignes: new LigneTicketImpression[]
            {
                new("Pâté café éclair à giác", 1m, 1m, 1m),
            });

        var flux = TicketEscPos.Generer(donnees);

        // 'é' → 0x82 (CP-850/863 imprimante), mappage manuel POSTEK.
        Assert.Contains((byte)0x82, flux);
        // Hors commandes ESC/POS, uniquement du texte imprimable + sauts de ligne.
        var (_, brut) = Decode(flux);
        Assert.True(brut.All(b => b >= 0x20 || b == 0x0A),
            "aucun octet de contrôle autre que LF hors commandes ESC");
    }

    // ------------------------------------------------------------------
    // Récap TVA
    // ------------------------------------------------------------------
    [Fact]
    public void RecapTva_ImprimeParTaux()
    {
        var donnees = Ticket(
            recapTva: new LigneRecapTvaImpression[]
            {
                new(19m, 2.017m, 0.383m),
                new(7m, 0.234m, 0.016m),
            });

        var texte = Decode(TicketEscPos.Generer(donnees)).Texte;

        Assert.Contains("TVA 19%", texte);
        Assert.Contains("TVA 7%", texte);
        // TTC de la ligne 19 % = HT + TVA (2,017 + 0,383).
        Assert.Contains("2,400", texte);
    }

    // ------------------------------------------------------------------
    // Multi-règlement + rendu monnaie
    // ------------------------------------------------------------------
    [Fact]
    public void Reglements_ImprimesEnPied_AvecRendu()
    {
        var donnees = Ticket(
            reglements: new LigneReglementImpression[]
            {
                new("ESPECES", 5.000m),
            },
            monnaieRendue: 2.600m);

        var texte = Decode(TicketEscPos.Generer(donnees)).Texte;

        Assert.Contains("ESPECES", texte);
        Assert.Contains("5,000", texte);
        Assert.Contains("RENDU", texte);
        Assert.Contains("2,600", texte);
    }

    [Fact]
    public void PlusieursReglements_TousImprimes()
    {
        var donnees = Ticket(
            reglements: new LigneReglementImpression[]
            {
                new("CB", 1.000m),
                new("ESPECES", 1.400m),
            });

        var texte = Decode(TicketEscPos.Generer(donnees)).Texte;

        Assert.Contains("CB", texte);
        Assert.Contains("1,000", texte);
        Assert.Contains("1,400", texte);
        Assert.DoesNotContain("RENDU", texte);
    }

    [Fact]
    public void SansReglement_AucuneLigneRendu()
    {
        var texte = Decode(TicketEscPos.Generer(Ticket())).Texte;

        Assert.DoesNotContain("RENDU", texte);
    }

    // ------------------------------------------------------------------
    // Rapport de clôture Z
    // ------------------------------------------------------------------
    [Fact]
    public void RapportZ_ContientRecapTvaEtTotaux()
    {
        var donnees = new DonneesRapportZ
        {
            Numero = 3,
            NbTickets = 4,
            TotalTtc = 19.450m,
            TotalHt = 16.930m,
            TotalTva = 2.520m,
            RecapTva = new LigneRecapTvaImpression[]
            {
                new(19m, 16.930m, 2.520m),
            },
            Reglements = new LigneReglementImpression[]
            {
                new("ESPECES", 13.500m),
                new("TICKET RESTO", 5.950m),
            },
        };

        var texte = Decode(TicketEscPos.GenererRapportZ(donnees)).Texte;

        Assert.Contains("RAPPORT Z n° 3", texte);
        Assert.Contains("TVA 19%", texte);
        Assert.Contains("ESPECES", texte);
        Assert.Contains("13,500", texte);
        Assert.Contains("TICKET RESTO", texte);
        Assert.Contains("5,950", texte);
        Assert.Contains("TOTAL TTC", texte);
        Assert.Contains("19,450", texte);
    }

    [Fact]
    public void RapportZ_RenduImprime_QuandPositif()
    {
        var donnees = new DonneesRapportZ
        {
            Numero = 7,
            NbTickets = 2,
            TotalTtc = 3.000m,
            TotalHt = 2.616m,
            TotalTva = 0.384m,
            Reglements = new LigneReglementImpression[]
            {
                new("ESPECES", 3.200m),   // montants reçus : TTC 3,000 + rendu 0,200
                new("CB", 0.500m),
            },
            MonnaieRendue = 0.200m,
        };

        var texte = Decode(TicketEscPos.GenererRapportZ(donnees)).Texte;

        Assert.Contains("RENDU", texte);
        Assert.Contains("0,200", texte);
        // ESPECES reste le montant reçu (3,200), RENDU l'explicite.
        Assert.Contains("3,200", texte);
    }

    [Fact]
    public void RapportZ_SansRendu_AucuneLigneRendu()
    {
        var donnees = new DonneesRapportZ
        {
            Numero = 8,
            NbTickets = 1,
            TotalTtc = 2.400m,
            Reglements = new LigneReglementImpression[]
            {
                new("CB", 2.400m),
            },
            MonnaieRendue = 0m,
        };

        var texte = Decode(TicketEscPos.GenererRapportZ(donnees)).Texte;

        Assert.DoesNotContain("RENDU", texte);
    }

    [Fact]
    public void RapportZ_Flux_DemarreParInitEtTermineParDecoupe()
    {
        var flux = TicketEscPos.GenererRapportZ(new DonneesRapportZ { Numero = 1 });

        Assert.True(flux.Length > 2);
        Assert.Equal(0x1B, flux[0]);              // ESC
        Assert.Equal((byte)'@', flux[1]);         // init

        var n = flux.Length;
        Assert.Equal(0x1B, flux[n - 3]);          // découpe partielle
        Assert.Equal((byte)'d', flux[n - 2]);
        Assert.Equal((byte)3, flux[n - 1]);
    }

    // ------------------------------------------------------------------
    internal static (string Texte, List<byte> Brut) Decode(byte[] flux)
    {
        // Miroir exact du générateur : on retire les commandes ESC/POS
        // (ESC + lettre + paramètre éventuel) et on inverse le mappage manuel
        // (°, ×, é, è) avant de décoder le reste en CP-863.
        var inverse = new Dictionary<byte, char>
        {
            [0xEF] = '°', [0xF6] = '×', [0x82] = 'é', [0x8A] = 'è',
        };
        var e863 = System.Text.Encoding.GetEncoding(863);
        var texte = new System.Text.StringBuilder(flux.Length);
        var brut = new List<byte>(flux.Length);
        var i = 0;
        while (i < flux.Length)
        {
            var b = flux[i];
            if (b == 0x1B)
            {
                i++;                                          // ESC
                if (i < flux.Length)
                {
                    var cmd = flux[i++];                      // lettre de commande
                    if (cmd is (byte)'!' or (byte)'E' or (byte)'d' && i < flux.Length)
                        i++;                                  // paramètre
                }
                continue;
            }
            brut.Add(b);
            texte.Append(inverse.TryGetValue(b, out var c)
                ? c : e863.GetString(flux, i, 1)[0]);
            i++;
        }
        return (texte.ToString(), brut);
    }
}
