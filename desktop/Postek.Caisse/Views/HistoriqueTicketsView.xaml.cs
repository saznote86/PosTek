using System.Windows.Controls;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class HistoriqueTicketsView : UserControl
{
    public HistoriqueTicketsView()
    {
        InitializeComponent();
        DataContext = new GestionStubViewModel("Historique des tickets", "Ticket", "Date", "Total");
    }
}
