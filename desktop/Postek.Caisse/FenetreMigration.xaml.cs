using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace Postec.Caisse;

public partial class FenetreMigration : Window
{
    private sealed class EtapeAffichage
    {
        public string Etape { get; init; } = "";
        public string Statut { get; init; } = "";
        public string Details { get; init; } = "";
    }

    public FenetreMigration()
    {
        InitializeComponent();
        ChampBase.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "postec_demo.db");
        var dossier = TrouverDossierSource();
        if (dossier is not null)
        {
            ChampExports.Text = dossier;
            TexteEtat.Text = $"Dossier source détecté : {dossier}";
        }
    }

    private void SurChoisirExports(object sender, RoutedEventArgs e)
    {
        var dialogue = new OpenFileDialog
        {
            Title = "Sélectionner un fichier du dossier source",
            Filter = "Fichiers source|*.FIC;*.XLS;*.XLSX;*.CSV;*.xdd|Tous les fichiers|*.*",
            CheckFileExists = true,
        };
        if (dialogue.ShowDialog(this) == true)
            ChampExports.Text = Path.GetDirectoryName(dialogue.FileName) ?? "";
    }

    private void SurChoisirBase(object sender, RoutedEventArgs e)
    {
        var dialogue = new SaveFileDialog
        {
            Title = "Sélectionner la base SQLite POSTEC",
            Filter = "Base SQLite|*.db|Tous les fichiers|*.*",
            FileName = Path.GetFileName(ChampBase.Text),
        };
        if (dialogue.ShowDialog(this) == true)
            ChampBase.Text = dialogue.FileName;
    }

    private async void SurLancer(object sender, RoutedEventArgs e)
    {
        var exports = ChampExports.Text.Trim();
        var baseSqlite = ChampBase.Text.Trim();
        if (!Directory.Exists(exports))
        {
            MessageBox.Show(this, "Sélectionnez le dossier source local à importer.", "Migration", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(baseSqlite))
            return;

        BoutonLancer.IsEnabled = false;
        ListeEtapes.ItemsSource = null;
        TexteEtat.Text = "Migration en cours...";
        try
        {
            var resultat = await Task.Run(() => ExecuterPython(exports, baseSqlite));
            if (resultat is null)
            {
                TexteEtat.Text = "Échec : aucune réponse du moteur Python.";
                return;
            }

            var etapes = resultat.RootElement.GetProperty("etapes").EnumerateArray()
                .Select(etape => new EtapeAffichage
                {
                    Etape = etape.GetProperty("etape").GetString() ?? "",
                    Statut = etape.GetProperty("ok").GetBoolean() ? "OK" : "ÉCHEC",
                    Details = string.Join(" | ", etape.GetProperty("details").EnumerateArray()
                        .Select(detail => detail.GetString() ?? "")),
                }).ToList();
            ListeEtapes.ItemsSource = etapes;
            var ok = resultat.RootElement.GetProperty("ok").GetBoolean();
            TexteEtat.Text = ok ? "Migration terminée avec succès." : "Migration terminée avec anomalies.";
        }
        catch (Exception ex)
        {
            TexteEtat.Text = "Erreur pendant la migration.";
            MessageBox.Show(this, ex.Message, "Migration", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BoutonLancer.IsEnabled = true;
        }
    }

    private static JsonDocument? ExecuterPython(string exports, string baseSqlite)
    {
        var racine = TrouverRacinePython();
        var info = new ProcessStartInfo
        {
            FileName = "python",
            WorkingDirectory = racine,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        info.ArgumentList.Add("-m");
        info.ArgumentList.Add("migration");
        info.ArgumentList.Add("--exports");
        info.ArgumentList.Add(exports);
        info.ArgumentList.Add("--base");
        info.ArgumentList.Add(baseSqlite);

        using var processus = Process.Start(info)
            ?? throw new InvalidOperationException("Python n'a pas pu être lancé.");
        var sortie = processus.StandardOutput.ReadToEnd();
        var erreur = processus.StandardError.ReadToEnd();
        processus.WaitForExit();
        if (string.IsNullOrWhiteSpace(sortie))
            throw new InvalidOperationException(erreur.Trim().Length > 0 ? erreur.Trim() : "Le moteur Python n'a rien retourné.");
        return JsonDocument.Parse(sortie);
    }

    private static string TrouverRacinePython()
    {
        var dossier = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dossier is not null)
        {
            var candidat = Path.Combine(dossier.FullName, "migration", "__main__.py");
            if (File.Exists(candidat))
                return dossier.FullName;
            dossier = dossier.Parent;
        }
        throw new DirectoryNotFoundException("Dossier Python postek/migration introuvable.");
    }

    private static string? TrouverDossierSource()
    {
        var dossier = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dossier is not null)
        {
            if (dossier.EnumerateFiles("*.FIC").Any() || dossier.EnumerateFiles("*.XLS").Any()
                || dossier.EnumerateFiles("*.CSV").Any()
                || dossier.EnumerateDirectories("export*", new EnumerationOptions
                {
                    MatchCasing = MatchCasing.CaseInsensitive,
                    IgnoreInaccessible = true,
                }).Any())
                return dossier.FullName;
            dossier = dossier.Parent;
        }
        return null;
    }
}
