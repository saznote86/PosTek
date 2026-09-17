using System.Windows;
using System.Windows.Controls;
using Postek.Caisse.Caisse;

namespace Postek.Caisse;

/// <summary>
/// Encaissement tactile multi-règlement : saisie au pavé, répartition sur
/// espèces/CB/chèque/ticket resto/avoir, rendu monnaie calculé sur les espèces.
/// La fenêtre ne modifie le ticket qu'au moment de la validation — « Retour »
/// n'a rien changé (les règlements sont retirés dans ce cas).
/// </summary>
public partial class FenetrePaiement : Window
{
    private readonly CaisseService _caisse;

    /// <summary>Règlements validés (lus par MainWindow après ShowDialog).</summary>
    public IReadOnlyList<Reglement> ReglementsValides { get; private set; } =
        Array.Empty<Reglement>();

    /// <summary>Monnaie rendue au client (espèces uniquement).</summary>
    public decimal MonnaieRendue { get; private set; }

    // Saisie par pavé : partie entière + 3 décimales (millimes, convention POSTEK).
    private decimal _partieEntiere;
    private decimal _partieDecimale;
    private int _decimales;

    public FenetrePaiement(CaisseService caisse)
    {
        InitializeComponent();
        _caisse = caisse;

        TexteAPayer.Text = $"{_caisse.TotalTtc:0.000} TND";
        Rafraichir();

        // Fermeture par la barre de titre : même traitement que « Retour » —
        // aucun règlement ne doit subsister sur un ticket non validé.
        Closing += (_, _) =>
        {
            if (DialogResult != true)
                foreach (var r in _caisse.Reglements.ToList())
                    _caisse.SupprimerReglement(r);
        };
    }

    // ------------------------------------------------------------------
    // Pavé numérique
    // ------------------------------------------------------------------
    private void SurChiffre(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string c }) return;
        if (c == ",")
        {
            _apresVirgule = true;   // un seul séparateur : re-appuyer est sans effet
        }
        else if (_decimales > 0 || EstEnDecimales())
        {
            if (_decimales < 3)
            {
                _partieDecimale = _partieDecimale * 10 + (c[0] - '0');
                _decimales++;
            }
        }
        else
        {
            _partieEntiere = _partieEntiere * 10 + (c[0] - '0');
        }
        Rafraichir();
    }

    private bool _apresVirgule;

    private bool EstEnDecimales() => _apresVirgule;

    private void SurRetourArriere(object sender, RoutedEventArgs e)
    {
        if (_decimales > 0)
        {
            _partieDecimale /= 10;
            _decimales--;
            if (_decimales == 0) _apresVirgule = false;
        }
        else
        {
            _partieEntiere = decimal.Truncate(_partieEntiere / 10);
        }
        Rafraichir();
    }

    private void SurEffacerSaisie(object sender, RoutedEventArgs e)
    {
        _partieEntiere = 0; _partieDecimale = 0; _decimales = 0; _apresVirgule = false;
        Rafraichir();
    }

    private decimal SaisieCourante =>
        _apresVirgule || _decimales > 0
            ? _partieEntiere + _partieDecimale / Puissance10(_decimales)
            : _partieEntiere;

    private static decimal Puissance10(int n) => n switch
    {
        0 => 1m, 1 => 10m, 2 => 100m, _ => 1000m,
    };

    // ------------------------------------------------------------------
    // Modes de règlement
    // ------------------------------------------------------------------
    private void SurModeEspeces(object s, RoutedEventArgs e) => AjouterReglement(ModeReglement.Especes);
    private void SurModeCb(object s, RoutedEventArgs e) => AjouterReglement(ModeReglement.CarteBancaire);
    private void SurModeCheque(object s, RoutedEventArgs e) => AjouterReglement(ModeReglement.Cheque);
    private void SurModeTicketResto(object s, RoutedEventArgs e) => AjouterReglement(ModeReglement.TicketRestaurant);
    private void SurModeAvoir(object s, RoutedEventArgs e) => AjouterReglement(ModeReglement.Avoir);

    private void AjouterReglement(ModeReglement mode)
    {
        var montant = SaisieCourante;

        if (montant <= 0m)
        {
            // Confort tactile : toucher un mode sans saisir règle exactement le reste.
            montant = _caisse.ResteAPayer;
        }
        if (montant <= 0m) return;

        if (mode == ModeReglement.Especes)
        {
            // Les espèces peuvent dépasser le total : c'est le rendu monnaie.
            // Plafond de sécurité : aucun sens de « donner » plus que le total arrondi au-dessus.
            if (montant > _caisse.TotalTtc * 100) return;
        }
        else
        {
            // CB/chèque/TR/avoir : pas de monnaie — plafonnés au reste à payer.
            montant = Math.Min(montant, _caisse.ResteAPayer);
        }

        _caisse.Regler(mode, montant);
        SurEffacerSaisie(sender: null!, e: null!);
    }

    private void SurSupprimerReglement(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: VueReglement vue }) return;
        _caisse.SupprimerReglement(vue.Reglement);
        Rafraichir();
    }

    // ------------------------------------------------------------------
    // Validation
    // ------------------------------------------------------------------
    private void SurValider(object sender, RoutedEventArgs e)
    {
        if (!_caisse.EstSolde) return;

        MonnaieRendue = _caisse.MonnaieARendre;
        ReglementsValides = _caisse.Reglements.ToList();
        DialogResult = true;
    }

    private void SurRetour(object sender, RoutedEventArgs e)
    {
        // Annulation : on retire les règlements saisies — le ticket est intact.
        foreach (var r in _caisse.Reglements.ToList())
            _caisse.SupprimerReglement(r);
        DialogResult = false;
    }

    // ------------------------------------------------------------------
    // Rafraîchissement
    // ------------------------------------------------------------------
    private void Rafraichir()
    {
        TexteSaisie.Text = $"{SaisieCourante:0.000}";
        TexteReste.Text = _caisse.ResteAPayer > 0m
            ? $"Reste à payer : {_caisse.ResteAPayer:0.000} TND"
            : _caisse.MonnaieARendre > 0m
                ? $"Rendu : {_caisse.MonnaieARendre:0.000} TND"
                : "Ticket soldé";
        BoutonValider.IsEnabled = _caisse.EstSolde;

        ListeReglements.ItemsSource = _caisse.Reglements
            .Select(r => new VueReglement(r))
            .ToList();
    }

    /// <summary>Vue d'un règlement pour le DataTemplate (libellés en français).</summary>
    private sealed class VueReglement
    {
        public Reglement Reglement { get; }
        public string LibelleMode => ModesReglement.Libelle(Reglement.Mode);
        public string MontantAffiche => $"{Reglement.Montant:0.000} TND";

        public VueReglement(Reglement reglement) => Reglement = reglement;
    }
}
