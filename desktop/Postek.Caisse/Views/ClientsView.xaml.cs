using System.Windows;
using System.Windows.Controls;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class ClientsView : UserControl
{
    public ClientsView()
    {
        InitializeComponent();
        var modele = new ClientsViewModel(App.CheminBase);
        modele.EditionDemandee += client =>
        {
            var modale = new ClientModale(client) { Owner = Window.GetWindow(this) };
            if (modale.ShowDialog() != true || modale.Client is null) return;
            if (client is null) modele.Creer(modale.Client);
            else modele.Modifier(client, modale.Client);
        };
        DataContext = modele;
    }
}
