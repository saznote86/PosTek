using Postec.Caisse.Views;
using Xunit;

namespace Postec.Caisse.Tests;

public sealed class AccueilViewModelTests
{
    [Fact]
    public void AccueilViewModel_AfficheDateEtSaint()
    {
        var vm = new AccueilViewModel();

        Assert.Equal("V 1.0.0 PRO", vm.Version);
        Assert.Equal("Poste N°2", vm.NomPoste);
        Assert.Matches(@"^[a-zàâçéèêëîïôûùüÿñ]+ \d{2}/\d{2}/\d{4}$", vm.DateTexte);
        Assert.False(string.IsNullOrWhiteSpace(vm.SaintsTexte));
    }

    [Fact]
    public void AccueilViewModel_RaccourcisF1aF5_RoutentLesCommandes()
    {
        var vm = new AccueilViewModel();
        var destinations = new List<string>();
        vm.NavigationRequested += destinations.Add;

        vm.CaisseCommand.Execute(null);
        vm.GestionCommand.Execute(null);
        vm.OutilsCommand.Execute(null);
        vm.InfoLicenceCommand.Execute(null);
        vm.QuitterCommand.Execute(null);

        Assert.Equal(new[] { "caisse", "gestion", "outils", "licence", "quitter" }, destinations);
    }
}