using System.Security.Cryptography;

namespace Postec.Caisse.Caisse;

public sealed record UtilisateurSession(long Id, string Identifiant, string NomAffiche, string Role,
    bool DoitChangerMotDePasse = false)
{
    public bool EstAdministrateur => Role == "Administrateur";
}

public sealed record UtilisateurAdmin(long Id, string Identifiant, string NomAffiche,
    string Role, bool Actif, string CreeLe);

public sealed record EntreeAudit(long Id, long? UtilisateurId, string? Identifiant,
    string Action, string? Details, string Horodatage);

/// <summary>Hash PBKDF2 local pour les accès humains à la caisse.</summary>
public static class AuthentificationService
{
    private const int Iterations = 600_000;
    private const int TailleSel = 16;
    private const int TailleHash = 32;

    public static string HacherMotDePasse(string motDePasse)
    {
        if (string.IsNullOrEmpty(motDePasse))
            throw new ArgumentException("Le mot de passe ne peut pas être vide.", nameof(motDePasse));
        var sel = RandomNumberGenerator.GetBytes(TailleSel);
        var hash = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, Iterations,
            HashAlgorithmName.SHA256, TailleHash);
        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(sel)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifierMotDePasse(string motDePasse, string empreinte)
    {
        try
        {
            var morceaux = empreinte.Split('$');
            if (morceaux.Length != 4 || morceaux[0] != "PBKDF2-SHA256") return false;
            var iterations = int.Parse(morceaux[1]);
            var sel = Convert.FromBase64String(morceaux[2]);
            var attendu = Convert.FromBase64String(morceaux[3]);
            var obtenu = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, iterations,
                HashAlgorithmName.SHA256, attendu.Length);
            return CryptographicOperations.FixedTimeEquals(obtenu, attendu);
        }
        catch (FormatException) { return false; }
        catch (OverflowException) { return false; }
    }
}