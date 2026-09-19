using System.Globalization;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using Postec.Caisse.Services;
using System.IO;

namespace Postec.Caisse.Views;

public partial class ArticleModale : Window
{
    public ArticleEdition? Article { get; private set; }
    private string? _cheminImageSelectionne;

    private sealed record FamilleOption(string Code, string Libelle);
    private sealed record TvaOption(decimal Valeur, string Label);
    private sealed record ImprimanteOption(int Id, string Nom, string Type);

    public ArticleModale(ArticleLigne? article = null)
    {
        InitializeComponent();
        ChargerOptions(article);
        if (article is not null)
        {
            Titre.Text = "Modifier le produit";
            Code.Text = article.Code;
            Designation.Text = article.Designation;
            Prix.Text = article.PrixTtc.ToString("0.000", CultureInfo.CurrentCulture);
            Famille.SelectedValue = article.FamilleCode;
            Tva.SelectedValue = article.TauxTva;
            Imprimante.SelectedValue = article.ImprimanteId;
            if (!string.IsNullOrWhiteSpace(article.CheminImage)) ChargerImage(article.CheminImage);
        }
        else
        {
            Tva.SelectedValue = 0.190m;
        }
        Loaded += (_, _) => Code.Focus();
    }

    private void ChargerImage(string chemin)
    {
        try
        {
            if (!File.Exists(chemin)) return;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(chemin, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            ApercuImage.Source = bitmap;
            ApercuImage.Visibility = Visibility.Visible;
            TexteAucuneImage.Visibility = Visibility.Collapsed;
            BoutonSupprimerImage.Visibility = Visibility.Visible;
            _cheminImageSelectionne = chemin;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Impossible de charger l'image : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SurParcourirImage(object sender, RoutedEventArgs e)
    {
        var dialogue = new OpenFileDialog
        {
            Filter = "Fichiers image|*.jpg;*.jpeg;*.png;*.gif;*.bmp|Tous les fichiers|*.*",
            Title = "Sélectionner une image pour le produit"
        };
        if (dialogue.ShowDialog() != true) return;

        try
        {
            var dossierImages = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "produits");
            Directory.CreateDirectory(dossierImages);
            var extension = Path.GetExtension(dialogue.FileName).ToLowerInvariant();
            var code = string.IsNullOrWhiteSpace(Code.Text) ? "produit" : Code.Text.Trim();
            var cheminDestination = Path.Combine(dossierImages, $"{code}_{Guid.NewGuid():N}{extension}");
            File.Copy(dialogue.FileName, cheminDestination);
            ChargerImage(cheminDestination);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Erreur lors de la copie de l'image : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SurSupprimerImage(object sender, RoutedEventArgs e)
    {
        ApercuImage.Source = null;
        ApercuImage.Visibility = Visibility.Collapsed;
        TexteAucuneImage.Visibility = Visibility.Visible;
        BoutonSupprimerImage.Visibility = Visibility.Collapsed;
        _cheminImageSelectionne = null;
    }

    private void Valider_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Code.Text))
        {
            Erreur.Text = "Le code est obligatoire.";
            Code.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(Designation.Text))
        {
            Erreur.Text = "La désignation est obligatoire.";
            Designation.Focus();
            return;
        }
        if (!decimal.TryParse(Prix.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var prix))
        {
            Erreur.Text = "Le prix doit être un nombre valide.";
            Prix.Focus();
            return;
        }
        try
        {
            var taux = Tva.SelectedItem is TvaOption option ? option.Valeur : 0.190m;
            var famille = Famille.SelectedItem as FamilleOption;
            var imprimante = Imprimante.SelectedItem as ImprimanteOption;
            Article = new ArticleEdition(
                Code.Text.Trim(), Designation.Text.Trim(),
                decimal.Round(prix, 3, MidpointRounding.AwayFromZero),
                taux, famille?.Code, imprimante?.Id, _cheminImageSelectionne);
            DialogResult = true;
        }
        catch (ArgumentException ex)
        {
            Erreur.Text = ex.Message;
        }
    }

    private void ChargerOptions(ArticleLigne? article)
    {
        using var connexion = new SqliteConnection($"Data Source={App.CheminBase}");
        connexion.Open();
        var familles = new List<FamilleOption>();
        using (var commande = connexion.CreateCommand())
        {
            commande.CommandText = "SELECT code, libelle FROM famille_article ORDER BY ordre, libelle";
            using var lecture = commande.ExecuteReader();
            while (lecture.Read()) familles.Add(new(lecture.GetString(0), lecture.GetString(1)));
        }
        Famille.ItemsSource = familles;

        var taux = new List<TvaOption>();
        using (var commande = connexion.CreateCommand())
        {
            commande.CommandText = "SELECT valeur, label FROM taux_tva ORDER BY valeur DESC";
            using var lecture = commande.ExecuteReader();
            while (lecture.Read())
                taux.Add(new(decimal.Parse(lecture.GetString(0), CultureInfo.InvariantCulture), lecture.GetString(1)));
        }
        Tva.ItemsSource = taux;

        var imprimantes = new List<ImprimanteOption>();
        using (var commande = connexion.CreateCommand())
        {
            commande.CommandText = "SELECT id, nom, type FROM imprimante WHERE active = 1 ORDER BY nom";
            using var lecture = commande.ExecuteReader();
            while (lecture.Read()) imprimantes.Add(new(lecture.GetInt32(0), lecture.GetString(1), lecture.GetString(2)));
        }
        Imprimante.ItemsSource = imprimantes;
    }
}