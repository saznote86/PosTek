using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Postec.Caisse.Views.Controls;

namespace Postec.Caisse.Services;

public sealed class NavigationService
{
    private readonly ContentControl _hote;
    private readonly Stack<UserControl> _historique = new();

    public NavigationService(ContentControl hote) => _hote = hote;

    public event Action<string>? DestinationDemandee;

    public void NaviguerVers(string destination) => DestinationDemandee?.Invoke(destination);

    public void Naviguer(UserControl vue)
    {
        if (_hote.Content is UserControl actuelle)
            _historique.Push(actuelle);
        _hote.Content = vue;
        _hote.UpdateLayout();
        (_hote as AutomationContentControl)?.RafraichirAutomationPeer();
        FocusManager.SetFocusedElement(_hote, vue);
        Keyboard.Focus(vue);
    }

    public void Retour(UserControl? secours = null)
    {
        if (_historique.Count > 0)
            _hote.Content = _historique.Pop();
        else if (secours is not null)
            _hote.Content = secours;
        _hote.UpdateLayout();
        (_hote as AutomationContentControl)?.RafraichirAutomationPeer();
        if (_hote.Content is UserControl vue)
        {
            FocusManager.SetFocusedElement(_hote, vue);
            Keyboard.Focus(vue);
        }
    }

    public void ViderHistorique() => _historique.Clear();
}
