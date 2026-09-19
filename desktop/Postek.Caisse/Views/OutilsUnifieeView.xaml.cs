using System.Windows.Controls;

namespace Postec.Caisse.Views;

/// <summary>
/// Hôte de l'onglet Outils de l'Administration : réutilise la vue de
/// maintenance existante et transforme son « Retour » en sortie d'onglet
/// (l'Administration ferme alors la fenêtre).
/// </summary>
public partial class OutilsUnifieeView : UserControl
{
    public OutilsUnifieeView()
    {
        InitializeComponent();
        VueOutils.CloseRequested += () => RetourDemandee?.Invoke();
    }

    public event Action? RetourDemandee;
}
