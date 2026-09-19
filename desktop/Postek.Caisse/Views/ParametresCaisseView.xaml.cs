using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Postec.Caisse.Views;

public partial class ParametresCaisseView : UserControl
{
    public ObservableCollection<ParametreLigne> Parametres { get; } = new();
    public ParametresCaisseView()
    {
        InitializeComponent();
        foreach (var code in new[] { "AFF_STOCK", "APPEL_CLIENT", "APPEL_VEND", "CODE_BARRE", "CODE_VEND", "DOSE_AUTO", "DOUBLE_ECRAN", "IGNORE_MSG", "IMPR_AUTO", "IMPR_AUTON", "IMPR_AUTO_CDE" })
            Parametres.Add(new ParametreLigne(code, "", "", "Parametre caisse POSTEC"));
        DataContext = this;
    }
    private void SurSuivant(object sender, RoutedEventArgs e)
    {
        var index = Parametres.ToList().FindIndex(p => p.Code.Contains(Recherche.Text, StringComparison.OrdinalIgnoreCase));
        if (index >= 0) MessageBox.Show($"Paramètre trouvé : {Parametres[index].Code}", "POSTEC");
    }
}
