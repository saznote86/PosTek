using System.Windows.Controls;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class ClaviersTactilesView : UserControl
{
    public ClaviersTactilesView()
    {
        InitializeComponent();
        DataContext = new GestionStubViewModel("Claviers tactiles", "Clavier", "Mode", "État");
    }
}
