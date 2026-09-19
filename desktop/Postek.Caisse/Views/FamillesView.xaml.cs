using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Views;

public partial class FamillesView : UserControl
{
    private readonly ObservableCollection<FamilleLigne> _familles = new();
    private FamilleLigne? _selection;

    public FamillesView()
    {
        InitializeComponent();
        ListeFamilles.ItemsSource = _familles;
        ChargerFamilles();
    }

    private void ChargerFamilles()
    {
        _familles.Clear();
        using var connexion = new SqliteConnection($"Data Source={App.CheminBase}");
        connexion.Open();
        using var commande = connexion.CreateCommand();
        commande.CommandText = @"
            SELECT f.code, f.libelle, f.ordre, COUNT(a.code)
            FROM famille_article f
            LEFT JOIN article a ON a.famille_code = f.code
            GROUP BY f.code, f.libelle, f.ordre
            ORDER BY f.ordre, f.libelle";
        using var lecture = commande.ExecuteReader();
        while (lecture.Read())
        {
            _familles.Add(new FamilleLigne(
                lecture.GetString(0), lecture.GetString(1), lecture.GetInt32(2), lecture.GetInt32(3)));
        }
    }

    private void SurSelectionFamille(object sender, SelectionChangedEventArgs e)
    {
        _selection = ListeFamilles.SelectedItem as FamilleLigne;
        if (_selection is null) return;
        ChampCode.Text = _selection.Code;
        ChampLibelle.Text = _selection.Libelle;
        ChampOrdre.Text = _selection.Ordre.ToString(CultureInfo.InvariantCulture);
    }

    private void SurNouvelleFamille(object sender, RoutedEventArgs e)
    {
        _selection = null;
        ListeFamilles.SelectedItem = null;
        ChampCode.Clear();
        ChampLibelle.Clear();
        ChampOrdre.Text = "0";
        ChampLibelle.Focus();
    }

    private void SurEnregistrer(object sender, RoutedEventArgs e)
    {
        var libelle = ChampLibelle.Text.Trim();
        if (string.IsNullOrWhiteSpace(libelle))
        {
            MessageBox.Show(Window.GetWindow(this), "Le libellé est obligatoire.", "Familles", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!int.TryParse(ChampOrdre.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ordre))
        {
            MessageBox.Show(Window.GetWindow(this), "L'ordre doit être un nombre entier.", "Familles", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var code = string.IsNullOrWhiteSpace(ChampCode.Text)
            ? NormaliserCode(libelle)
            : ChampCode.Text.Trim().ToUpperInvariant();
        try
        {
            using var bdd = new BaseDonnees(App.CheminBase);
            bdd.AjouterFamille(new FamilleArticle(code, libelle, ordre, "#3B82F6"));
            ChargerFamilles();
            Selectionner(code);
            MessageBox.Show(Window.GetWindow(this), "Famille enregistrée avec succès.", "POSTEC", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), exception.Message, "Familles", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SurSupprimer(object sender, RoutedEventArgs e)
    {
        if (_selection is null)
        {
            MessageBox.Show(Window.GetWindow(this), "Veuillez sélectionner une famille à supprimer.", "POSTEC", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var famille = _selection;
        if (MessageBox.Show(Window.GetWindow(this), $"Confirmer la suppression de la famille {famille.Libelle} ?", "POSTEC", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            using var bdd = new BaseDonnees(App.CheminBase);
            bdd.SupprimerFamille(famille.Code);
            SurNouvelleFamille(sender, e);
            ChargerFamilles();
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), exception.Message, "Familles", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Selectionner(string code)
    {
        ListeFamilles.SelectedItem = _familles.FirstOrDefault(famille => famille.Code == code);
    }

    private static string NormaliserCode(string libelle)
    {
        var normalise = libelle.ToUpperInvariant().Normalize(NormalizationForm.FormD);
        return new string(normalise
            .Where(caractere => CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
            .Where(char.IsLetterOrDigit)
            .Take(10)
            .ToArray());
    }

    private void SurFermer(object sender, RoutedEventArgs e)
    {
        if (RetourDemandee is not null)
            RetourDemandee.Invoke();
        else
            Window.GetWindow(this)?.Close();
    }

    public event Action? RetourDemandee;

    private sealed record FamilleLigne(string Code, string Libelle, int Ordre, int NbProduits);
}