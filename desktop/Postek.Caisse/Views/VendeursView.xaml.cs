using System.Windows;
using System.Windows.Controls;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class VendeursView : UserControl
{
    public VendeursView()
    {
        InitializeComponent();
        var modele = new VendeursViewModel(App.CheminBase);
        modele.EditionDemandee += vendeur => { var modale = new VendeurModale(vendeur) { Owner = Window.GetWindow(this) }; if (modale.ShowDialog() == true && modale.Vendeur is not null) { if (vendeur is null) modele.Creer(modale.Vendeur); else modele.Modifier(vendeur, modale.Vendeur); } };
        DataContext = modele;
    }
}
