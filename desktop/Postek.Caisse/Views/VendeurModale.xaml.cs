using System.Windows;
using System.Windows.Controls;
using Postec.Caisse.Services;
namespace Postec.Caisse.Views;
public partial class VendeurModale : Window
{
    public VendeurEdition? Vendeur { get; private set; }
    public VendeurModale(VendeurLigne? vendeur = null) { InitializeComponent(); if (vendeur is not null) { Titre.Text = "Modifier le vendeur"; Code.Text = vendeur.Code; Nom.Text = vendeur.Nom; Role.SelectedIndex = vendeur.Role == "Administrateur" ? 0 : 1; Actif.IsChecked = vendeur.Actif; } }
    private void Valider_Click(object sender, RoutedEventArgs e) { try { Vendeur = new VendeurEdition(Code.Text, Nom.Text, Prenom.Text, MotDePasse.Password, (Role.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Caissier", Actif.IsChecked == true); DialogResult = true; } catch (ArgumentException ex) { Erreur.Text = ex.Message; } }
}