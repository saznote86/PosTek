using System.Windows.Controls;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class ComptabiliteView : UserControl
{
    public ComptabiliteView()
    {
        InitializeComponent();
        DataContext = new GestionStubViewModel("Comptabilité", "État", "Libellé", "Montant");
    }
}
