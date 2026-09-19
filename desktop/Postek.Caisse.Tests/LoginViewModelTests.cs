using System.IO;
using Postec.Caisse.Caisse;
using Xunit;

namespace Postec.Caisse.Tests;

public sealed class LoginViewModelTests : IDisposable
{
    private readonly string _chemin = Path.Combine(Path.GetTempPath(), $"postek_login_vm_{Guid.NewGuid():N}.db");

    public LoginViewModelTests()
    {
        using var bdd = new BaseDonnees(_chemin);
        bdd.AjouterUtilisateur("admin", "Administrateur", "Administrateur", "123456");
        bdd.AjouterUtilisateur("alice", "Alice", "Caissier", "654321");
    }

    public void Dispose() { try { File.Delete(_chemin); } catch { } }

    private LoginViewModel Creer(out BaseDonnees bdd)
    {
        bdd = new BaseDonnees(_chemin);
        return new LoginViewModel(bdd);
    }

    [Fact]
    public void LoginViewModel_ChargeUtilisateurs()
    {
        using var vm = Creer(out var bdd);
        using (bdd) Assert.Contains("admin", vm.Utilisateurs);
    }

    [Fact]
    public void LoginViewModel_ValiderAvecBonMotDePasse()
    {
        using var vm = Creer(out var bdd);
        using (bdd) { vm.UtilisateurSelectionne = "admin"; Assert.True(vm.SeConnecter("123456")); Assert.Equal("Administrateur", vm.Session!.Role); }
    }

    [Fact]
    public void LoginViewModel_ValiderAvecMauvaisMotDePasse()
    {
        using var vm = Creer(out var bdd);
        using (bdd) { vm.UtilisateurSelectionne = "admin"; Assert.False(vm.SeConnecter("incorrect")); Assert.Equal("Identifiant ou mot de passe incorrect.", vm.MessageErreur); Assert.Empty(vm.MotDePasse); }
    }

    [Fact]
    public void LoginViewModel_AjouterChiffre()
    {
        using var vm = Creer(out var bdd);
        using (bdd) { vm.AjouterChiffre("1"); vm.AjouterChiffre("2"); vm.AjouterChiffre("3"); Assert.Equal("123", vm.MotDePasse); }
    }

    [Fact]
    public void LoginViewModel_Effacer()
    {
        using var vm = Creer(out var bdd);
        using (bdd) { vm.MotDePasse = "123456"; vm.Effacer(); Assert.Empty(vm.MotDePasse); }
    }

    [Fact]
    public void LoginViewModel_Annuler()
    {
        using var vm = Creer(out var bdd);
        using (bdd) { var annule = false; vm.AnnulationDemandee += () => annule = true; vm.AnnulerCommand.Execute(null); Assert.True(annule); }
    }
}