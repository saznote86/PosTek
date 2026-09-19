using System.Windows;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class MotsDePasseModal : Window
{
    public MotsDePasseModal()
    {
        InitializeComponent();
        var vue = new MotsDePasseViewModel(new UsbKeyService());
        vue.FermerDemandee += Close;
        DataContext = vue;
    }
}
