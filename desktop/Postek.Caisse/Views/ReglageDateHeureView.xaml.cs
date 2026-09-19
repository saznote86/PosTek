using System.Windows.Controls;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class ReglageDateHeureView : UserControl
{
    public ReglageDateHeureView()
    {
        InitializeComponent();
        DataContext = new GestionStubViewModel("Réglage date-heure", "Élément", "Valeur", "État");
    }
}
