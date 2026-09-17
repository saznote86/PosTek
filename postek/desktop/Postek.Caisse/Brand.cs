namespace Postek.Caisse.Themes;

/// <summary>
/// Identité de marque centralisée : tout texte affiché passe par ici.
/// Aucune référence à l'application d'origine ne doit exister dans le code.
/// </summary>
public sealed class Brand
{
    public string Nom { get; init; } = "POSTEK";
    public string Slogan { get; init; } = "Caisse & gestion — conforme Tunisie";
    public string Version { get; init; } = "0.1.0";

    public static Brand Charger() => new();
}
