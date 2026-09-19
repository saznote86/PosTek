using System.Windows;
using System.Windows.Controls;

namespace Postec.Caisse;

/// <summary>Synchronise le PasswordBox avec une propriété ViewModel sans exposer de mot de passe dans le code-behind.</summary>
public static class PasswordHelper
{
    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.RegisterAttached(
            "Password", typeof(string), typeof(PasswordHelper),
            new FrameworkPropertyMetadata(string.Empty, OnPasswordChanged));

    public static string GetPassword(DependencyObject element) =>
        (string)element.GetValue(PasswordProperty);

    public static void SetPassword(DependencyObject element, string value) =>
        element.SetValue(PasswordProperty, value ?? string.Empty);

    private static void OnPasswordChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is not PasswordBox passwordBox) return;

        passwordBox.PasswordChanged -= PasswordBoxPasswordChanged;
        var value = args.NewValue as string ?? string.Empty;
        if (passwordBox.Password != value)
            passwordBox.Password = value;
        passwordBox.PasswordChanged += PasswordBoxPasswordChanged;
    }

    private static void PasswordBoxPasswordChanged(object sender, RoutedEventArgs args)
    {
        if (sender is PasswordBox passwordBox)
            SetPassword(passwordBox, passwordBox.Password);
    }
}