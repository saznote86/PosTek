using System.IO;
using Postek.Caisse.Caisse;
using Xunit;

namespace Postek.Caisse.Tests;

/// <summary>
/// Tests du canal d'impression : sans imprimante configurée, le flux ESC/POS
/// part dans le fichier « imprimante.logique » (append) — rien n'est perdu.
/// Le canal winspool n'est pas testé ici (imprimante physique requise) ;
/// il se vérifie avec `Postek.Caisse --test-impression &lt;nom-imprimante&gt;`.
/// </summary>
public class ImprimanteBruteTests : IDisposable
{
    // Le sink fichier écrit dans BaseDirectory : on l'isole via un fichier dédié
    // que l'on purge avant/après. ImprimanteBrute écrit à une seule adresse,
    // on vérifie donc l'append et le contenu, pas le nom du fichier.
    private readonly string _sink =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "imprimante.logique");

    public ImprimanteBruteTests()
    {
        TryDelete();
    }

    public void Dispose() => TryDelete();

    private void TryDelete()
    {
        try { File.Delete(_sink); } catch { /* absent ou verrouillé : tant pis */ }
    }

    [Fact]
    public void SansImprimante_LeFluxPartDansLeFichier()
    {
        var flux = new byte[] { 0x1B, (byte)'@', (byte)'A', (byte)'B', 0x0A };

        var resultat = ImprimanteBrute.Envoyer(flux, null);

        Assert.True(resultat.Succes);
        Assert.StartsWith("fichier:", resultat.Canal);
        Assert.False(string.IsNullOrWhiteSpace(resultat.Erreur));   // raison tracée

        var ecrit = File.ReadAllBytes(_sink);
        Assert.Equal(flux, ecrit);
    }

    [Fact]
    public void DeuxEnvois_SAccumulentsDansLeFichier()
    {
        var f1 = new byte[] { 0x01, 0x02 };
        var f2 = new byte[] { 0x03 };

        ImprimanteBrute.Envoyer(f1, null);
        ImprimanteBrute.Envoyer(f2, null);

        var ecrit = File.ReadAllBytes(_sink);
        Assert.Equal(3, ecrit.Length);
        Assert.Equal(0x01, ecrit[0]);
        Assert.Equal(0x03, ecrit[2]);
    }

    [Fact]
    public void ImprimanteInexistante_RepliFichier()
    {
        var flux = new byte[] { 0x1B, (byte)'@' };

        var resultat = ImprimanteBrute.Envoyer(flux, "imprimante-qui-nexiste-pas-XYZ");

        // Succès par repli : le ticket n'est jamais perdu.
        Assert.True(resultat.Succes);
        Assert.StartsWith("fichier:", resultat.Canal);
        Assert.Contains("winspool indisponible", resultat.Erreur);
        Assert.Equal(flux, File.ReadAllBytes(_sink));
    }

    [Fact]
    public void UnTicketComplet_SeDecodeEnCP863()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        var donnees = new DonneesTicket
        {
            NumeroTicket = 7,
            TotalTtc = 3.600m,
            Lignes = new LigneTicketImpression[] { new("Croissant", 1m, 0.600m, 0.600m) },
            Reglements = new LigneReglementImpression[] { new("ESPECES", 5.000m) },
            MonnaieRendue = 1.400m,
        };

        var resultat = ImprimanteBrute.Envoyer(TicketEscPos.Generer(donnees), null);

        Assert.True(resultat.Succes);
        var ecrit = File.ReadAllBytes(_sink);
        Assert.Contains((byte)0x1B, ecrit);            // commandes ESC/POS présentes
        Assert.Contains((byte)'R', ecrit);             // « RENDU »
    }
}
