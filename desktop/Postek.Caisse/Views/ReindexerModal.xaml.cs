using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class ReindexerModal : Window
{
    public ReindexerModal()
    {
        InitializeComponent();
        var vue = new ReindexerViewModel(new DatabaseMaintenanceService(), App.CheminBase);
        vue.FermerDemandee += Close;
        DataContext = vue;
    }

    private void SurParcourir(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ReindexerViewModel vue) return;
        var dialogue = new OpenFileDialog { Filter = "Base SQLite|*.db" };
        if (dialogue.ShowDialog(this) == true) vue.Chemin = dialogue.FileName;
    }
}
