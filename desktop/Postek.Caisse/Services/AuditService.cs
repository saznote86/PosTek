using Postec.Caisse.Caisse;

namespace Postec.Caisse.Services;

/// <summary>Point d'entrée unique pour les opérations auditées de l'application.</summary>
public sealed class AuditService
{
    private readonly string _cheminBase;
    private readonly Func<UtilisateurSession?> _utilisateurCourant;

    public AuditService(string cheminBase, Func<UtilisateurSession?>? utilisateurCourant = null)
    {
        _cheminBase = cheminBase ?? throw new ArgumentNullException(nameof(cheminBase));
        _utilisateurCourant = utilisateurCourant ?? (() => App.SessionCourante);
    }

    public void EnregistrerAudit(string utilisateur, string action, string details)
    {
        using var bdd = new BaseDonnees(_cheminBase);
        var session = _utilisateurCourant();
        if (session is null || !string.Equals(session.Identifiant, utilisateur, StringComparison.OrdinalIgnoreCase))
            session = TrouverUtilisateur(bdd, utilisateur);
        bdd.JournaliserAudit(session, action, details);
    }

    public void EnregistrerAudit(string action, string details)
    {
        EnregistrerAudit(_utilisateurCourant()?.Identifiant ?? "systeme", action, details);
    }

    private static UtilisateurSession? TrouverUtilisateur(BaseDonnees bdd, string identifiant)
    {
        var utilisateur = bdd.ListerUtilisateurs()
            .FirstOrDefault(x => string.Equals(x.Identifiant, identifiant, StringComparison.OrdinalIgnoreCase));
        return utilisateur is null
            ? null
            : new UtilisateurSession(utilisateur.Id, utilisateur.Identifiant, utilisateur.NomAffiche, utilisateur.Role);
    }
}