namespace Postec.Caisse.Themes;

/// <summary>
/// Identité de marque centralisée : tout texte affiché passe par ici.
/// Aucune référence à l'application d'origine ne doit exister dans le code.
/// </summary>
public sealed class Brand
{
    public string Nom { get; init; } = "POSTEC";
    public string Slogan { get; init; } = "Caisse & gestion — conforme Tunisie";
    public string Version { get; init; } = "0.1.0";
    public string Adresse { get; init; } = "Tunis - Tunisie";
    public string MatriculeFiscal { get; init; } = "A COMPLETER";
    public IReadOnlyList<string> InfosSociete { get; init; } =
        new[] { "POSTEC - Solutions de caisse", "Service client : à compléter" };

    public static Brand Charger() => new();
}
