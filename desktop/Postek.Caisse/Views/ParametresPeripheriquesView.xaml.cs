using System.Windows.Controls;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class ParametresPeripheriquesView : UserControl
{
    private readonly ParametresPeripheriquesViewModel _vue;

    public ParametresPeripheriquesView()
    {
        InitializeComponent();
        _vue = new ParametresPeripheriquesViewModel(
            new SystemInfoService(), new SerialPortService(), new PrinterService());
        _vue.RetourDemande += () => CloseRequested?.Invoke();
        DataContext = _vue;
    }

    public event Action? CloseRequested;
}
