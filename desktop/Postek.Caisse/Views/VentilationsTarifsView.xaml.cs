using System.Windows.Controls;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class VentilationsTarifsView : UserControl
{
    public VentilationsTarifsView()
    {
        InitializeComponent();
        DataContext = new GestionStubViewModel("Ventilations et tarifs", "Code", "Taux", "Tarif");
    }
}
