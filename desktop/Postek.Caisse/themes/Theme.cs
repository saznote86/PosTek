using System.IO;
using System.Text.Json;

namespace Postec.Caisse.Themes;

/// <summary>
/// ThÃ¨me visuel POSTEC : couleurs, police, sons, logo.
/// Surchargable par le revendeur sans recompilation (themes/postec.json).
/// </summary>
public sealed class Theme
{
    // Couleurs (hex #RRGGBB)
    public string Primaire { get; set; } = "#1F6FEB";
    public string PrimaireSombre { get; set; } = "#1A5FD0";
    public string Fond { get; set; } = "#10151C";
    public string Surface { get; set; } = "#1A2230";
    public string Texte { get; set; } = "#E8EDF4";
    public string TexteSecondaire { get; set; } = "#8FA3BF";
    public string Succes { get; set; } = "#2EA043";
    public string Alerte { get; set; } = "#D29922";
    public string Danger { get; set; } = "#C93C37";
    public string ToucheFond { get; set; } = "#232D3F";
    public string ToucheFondActif { get; set; } = "#2C3A52";
    public string ToucheTexte { get; set; } = "#E8EDF4";

    public string FamillePolice { get; set; } = "Segoe UI";
    public double TailleClavier { get; set; } = 22;
    public double TailleTicket { get; set; } = 14;

    public string? FichierSons { get; set; }
    public string? FichierLogo { get; set; }

    // Impression (revendeur) â€” ticket 80 mm ESC/POS.
    public string? Imprimante { get; set; }
    public bool ImprimerAutoApresEncaissement { get; set; } = true;

    public static Theme Charger(string dossierThemes)
    {
        var chemin = Path.Combine(dossierThemes, "postec.json");
        if (!File.Exists(chemin))
            return new Theme(); // valeurs de repli

        var json = File.ReadAllText(chemin);
        var charge = JsonSerializer.Deserialize<ThemeCharge>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var t = new Theme();
        if (charge?.Couleurs is not null)
        {
            t.Primaire = charge.Couleurs.Primaire ?? t.Primaire;
            t.PrimaireSombre = charge.Couleurs.PrimaireSombre ?? t.PrimaireSombre;
            t.Fond = charge.Couleurs.Fond ?? t.Fond;
            t.Surface = charge.Couleurs.Surface ?? t.Surface;
            t.Texte = charge.Couleurs.Texte ?? t.Texte;
            t.TexteSecondaire = charge.Couleurs.TexteSecondaire ?? t.TexteSecondaire;
            t.Succes = charge.Couleurs.Succes ?? t.Succes;
            t.Alerte = charge.Couleurs.Alerte ?? t.Alerte;
            t.Danger = charge.Couleurs.Danger ?? t.Danger;
            t.ToucheFond = charge.Couleurs.ToucheFond ?? t.ToucheFond;
            t.ToucheFondActif = charge.Couleurs.ToucheFondActif ?? t.ToucheFondActif;
            t.ToucheTexte = charge.Couleurs.ToucheTexte ?? t.ToucheTexte;
        }
        if (charge?.Police is not null)
        {
            t.FamillePolice = charge.Police.Famille ?? t.FamillePolice;
            t.TailleClavier = charge.Police.TailleClavier ?? t.TailleClavier;
            t.TailleTicket = charge.Police.TailleTicket ?? t.TailleTicket;
        }
        t.FichierSons = charge?.Sons is not null ? charge.Sons.Touche : null;
        t.FichierLogo = charge?.Logo;
        if (charge?.Impression is not null)
        {
            t.Imprimante = charge.Impression.Imprimante;
            t.ImprimerAutoApresEncaissement = charge.Impression.ImprimerAuto ?? true;
        }
        return t;
    }
}

public sealed class ThemeCharge
{
    public CouleursTheme? Couleurs { get; set; }
    public PoliceTheme? Police { get; set; }
    public SonsTheme? Sons { get; set; }
    public string? Logo { get; set; }
    public ImpressionTheme? Impression { get; set; }
}

public sealed class CouleursTheme
{
    public string? Primaire { get; set; }
    public string? PrimaireSombre { get; set; }
    public string? Fond { get; set; }
    public string? Surface { get; set; }
    public string? Texte { get; set; }
    public string? TexteSecondaire { get; set; }
    public string? Succes { get; set; }
    public string? Alerte { get; set; }
    public string? Danger { get; set; }
    public string? ToucheFond { get; set; }
    public string? ToucheFondActif { get; set; }
    public string? ToucheTexte { get; set; }
}

public sealed class PoliceTheme
{
    public string? Famille { get; set; }
    public double? TailleClavier { get; set; }
    public double? TailleTicket { get; set; }
}

public sealed class SonsTheme
{
    public string? Touche { get; set; }
    public string? Tiroir { get; set; }
}

public sealed class ImpressionTheme
{
    public string? Imprimante { get; set; }
    public bool? ImprimerAuto { get; set; }
}

