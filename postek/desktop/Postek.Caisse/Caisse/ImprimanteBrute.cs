using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Postek.Caisse.Caisse;

/// <summary>
/// Envoi d'un flux brut (ESC/POS) à une imprimante Windows.
/// Deux canaux :
///  1. winspool RAW (spouleur Windows, imprimante nommée) — canal standard POS ;
///  2. sink fichier « imprimante.logique » (dossier exe) — pratique pour tester
///     sans imprimante : le flux s'accumule dans un fichier qu'on peut décoder.
/// </summary>
public static class ImprimanteBrute
{
    /// <summary>Résultat d'envoi : succès + canal utilisé + message d'erreur éventuel.</summary>
    public sealed record Resultat(bool Succes, string Canal, string? Erreur = null);

    public static Resultat Envoyer(byte[] flux, string? nomImprimante)
    {
        if (!string.IsNullOrWhiteSpace(nomImprimante))
        {
            try
            {
                EnvoyerWinspool(flux, nomImprimante!);
                return new Resultat(true, $"winspool:{nomImprimante}");
            }
            catch (Exception ex)
            {
                // Repli fichier : le ticket n'est jamais perdu.
                return EnvoyerFichier(flux, $"winspool indisponible ({ex.Message})");
            }
        }
        return EnvoyerFichier(flux, "aucune imprimante configurée");
    }

    private static Resultat EnvoyerFichier(byte[] flux, string raison)
    {
        var chemin = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "imprimante.logique");
        using var f = new FileStream(chemin, FileMode.Append, FileAccess.Write, FileShare.Read);
        f.Write(flux, 0, flux.Length);
        return new Resultat(true, $"fichier:{chemin}", raison);
    }

    // ------------------------------------------------------------------
    // winspool :job RAW — P/Invoke standard (RAW dataType), comme la plupart
    // des imprimantes POS thermiques 80 mm.
    // ------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DOC_INFO_1
    {
        public string pDocName;
        public string pOutputFile;
        public string pDatatype;
    }

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr hPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOC_INFO_1 docInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBuf, int cbBuf, out int pcWritten);

    private static void EnvoyerWinspool(byte[] flux, string nomImprimante)
    {
        if (!OpenPrinter(nomImprimante, out var h, IntPtr.Zero))
            throw new InvalidOperationException(
                $"imprimante « {nomImprimante} » introuvable (erreur {Marshal.GetLastWin32Error()})");

        try
        {
            var doc = new DOC_INFO_1
            {
                pDocName = $"POSTEK ticket {DateTime.Now:yyyyMMdd HHmmss}",
                pOutputFile = null!,
                pDatatype = "RAW",
            };
            if (!StartDocPrinter(h, 1, ref doc))
                throw new InvalidOperationException($"StartDocPrinter a échoué (erreur {Marshal.GetLastWin32Error()})");
            try
            {
                if (!StartPagePrinter(h))
                    throw new InvalidOperationException($"StartPagePrinter a échoué (erreur {Marshal.GetLastWin32Error()})");
                try
                {
                    var nonManagé = Marshal.AllocHGlobal(flux.Length);
                    try
                    {
                        Marshal.Copy(flux, 0, nonManagé, flux.Length);
                        if (!WritePrinter(h, nonManagé, flux.Length, out var ecrits) || ecrits != flux.Length)
                            throw new InvalidOperationException($"WritePrinter a échoué (erreur {Marshal.GetLastWin32Error()})");
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(nonManagé);
                    }
                }
                finally { EndPagePrinter(h); }
            }
            finally { EndDocPrinter(h); }
        }
        finally { ClosePrinter(h); }
    }
}
