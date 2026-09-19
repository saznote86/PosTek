using System.Windows.Controls;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class ProgrammationAvanceeView : UserControl
{
    public ProgrammationAvanceeView()
    {
        InitializeComponent();
        DataContext = new GestionStubViewModel("Programmation avancée", "Paramètre", "Valeur", "État");
    }
}
