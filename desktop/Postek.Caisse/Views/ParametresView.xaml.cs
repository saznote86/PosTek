using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace Postec.Caisse.Views;

public partial class ParametresView : UserControl
{
    public ObservableCollection<ParametreLigne> Parametres { get; } = new();

    public ParametresView()
    {
        InitializeComponent();
        DataContext = this;
        foreach (var code in new[] { "CHEM_DATA", "CHEQ_ORDRE", "CHEQ_VILLE", "CREDIT", "DUREE_ALARM", "GERE_AVOIR", "GEST_PLUS", "GEST_PRIX", "HEUR_CLOT", "IMPR_STE", "MOD_IMP", "MOD_RESTO", "MSG1_RELEVE" })
            Parametres.Add(new ParametreLigne(code, "", "", "Parametre POSTEC"));
    }

    private void SurAbandon(object sender, RoutedEventArgs e) => AbandonRequested?.Invoke();
    private void SurValider(object sender, RoutedEventArgs e) => ValiderRequested?.Invoke(Parametres);
    public event Action? AbandonRequested;
    public event Action<IReadOnlyList<ParametreLigne>>? ValiderRequested;
}

public sealed record ParametreLigne(string Code, string Valeur, string Complement, string Observation);
