using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class ClientModale : Window
{
    public ClientEdition? Client { get; private set; }

    public ClientModale(ClientLigne? client = null)
    {
        InitializeComponent();
        if (client is not null)
        {
            Titre.Text = "Modifier le client";
            Code.Text = client.Code; Nom.Text = client.Nom; Raison.Text = client.RaisonSociale;
            Telephone.Text = client.Telephone; Matricule.Text = client.MatriculeFiscal;
        }
        Loaded += (_, _) => Code.Focus();
    }

    private void Valider_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(Limite.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var limite) || limite < 0)
        {
            Erreur.Text = "La limite de crédit doit être un nombre positif."; return;
        }
        Client = new ClientEdition(Code.Text, Nom.Text, Raison.Text, Adresse.Text, Ville.Text, Postal.Text,
            Telephone.Text, Email.Text, Matricule.Text,
            (Regime.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Normal",
            decimal.Round(limite, 3, MidpointRounding.AwayFromZero));
        DialogResult = true;
    }
}