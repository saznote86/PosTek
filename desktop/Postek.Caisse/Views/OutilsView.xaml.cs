using System.Windows;
using System.Windows.Controls;
using Postec.Caisse.Services;

namespace Postec.Caisse.Views;

public partial class OutilsView : UserControl
{
    private readonly OutilsViewModel _vue;

    public OutilsView()
    {
        InitializeComponent();
        _vue = new OutilsViewModel(new DatabaseMaintenanceService(), new SystemInfoService(),
            new PrinterService(), new SerialPortService(), new UsbKeyService())
        {
            ConfirmationRemiseAZ = ConfirmerRemiseAZ,
            DemanderCodeRAZ = DemanderCodeRAZ,
            ConfirmationPremiereInstallation = ConfirmerPremiereInstallation,
        };
        _vue.ReindexerDemandee += OuvrirReindexer;
        _vue.MotsDePasseDemandee += OuvrirMotsDePasse;
        _vue.RetourDemande += () => CloseRequested?.Invoke();
        _vue.NotificationDemandee += message => MessageBox.Show(Window.GetWindow(this), message,
            "POSTEC", MessageBoxButton.OK, MessageBoxImage.Information);
        DataContext = _vue;
    }

    public event Action? CloseRequested;

    private bool ConfirmerRemiseAZ()
    {
        var owner = Window.GetWindow(this);
        if (MessageBox.Show(owner,
            "La remise à zéro va effacer TOUTES les données métier (articles, clients, ventes, clôtures...). Cette action est IRRÉVERSIBLE. Continuer ?",
            "Confirmation 1/2", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return false;
        return true;
    }

    private string? DemanderCodeRAZ()
    {
        var saisie = new TextBox
        {
            Width = 260,
            Height = 42,
            FontSize = 18,
            Margin = new Thickness(0, 12, 0, 12),
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        var panneau = new StackPanel { Margin = new Thickness(20) };
        panneau.Children.Add(new TextBlock
        {
            Text = "CONFIRMATION FINALE : tapez RAZ pour confirmer l'effacement total.",
            TextWrapping = TextWrapping.Wrap,
        });
        panneau.Children.Add(saisie);
        var fenetre = new Window
        {
            Title = "Confirmation RAZ",
            Content = panneau,
            Owner = Window.GetWindow(this),
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
        };
        var boutons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var annuler = new Button { Content = "Annuler", Width = 110, Margin = new Thickness(4) };
        var confirmer = new Button { Content = "Confirmer", Width = 110, Margin = new Thickness(4), IsDefault = true };
        annuler.Click += (_, _) => { fenetre.DialogResult = false; fenetre.Close(); };
        confirmer.Click += (_, _) => { fenetre.DialogResult = true; fenetre.Close(); };
        boutons.Children.Add(annuler);
        boutons.Children.Add(confirmer);
        panneau.Children.Add(boutons);
        saisie.Focus();
        return fenetre.ShowDialog() == true ? saisie.Text : null;
    }

    private bool ConfirmerPremiereInstallation()
    {
        var owner = Window.GetWindow(this);
        return MessageBox.Show(owner,
            "La première installation va configurer l'application pour un nouveau client. Continuer ?",
            "Première installation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private void OuvrirReindexer()
    {
        var modal = new ReindexerModal { Owner = Window.GetWindow(this) };
        modal.ShowDialog();
    }

    private void OuvrirMotsDePasse()
    {
        var modal = new MotsDePasseModal { Owner = Window.GetWindow(this) };
        modal.ShowDialog();
    }
}
