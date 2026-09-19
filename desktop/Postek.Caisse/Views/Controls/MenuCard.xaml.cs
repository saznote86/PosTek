using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Postec.Caisse.Views.Controls;

public partial class MenuCard : UserControl
{
    public MenuCard() => InitializeComponent();
    public string Titre { get => (string)GetValue(TitreProperty); set => SetValue(TitreProperty, value); }
    public static readonly DependencyProperty TitreProperty = DependencyProperty.Register(nameof(Titre), typeof(string), typeof(MenuCard), new PropertyMetadata(""));
    public string Icone { get => (string)GetValue(IconeProperty); set => SetValue(IconeProperty, value); }
    public static readonly DependencyProperty IconeProperty = DependencyProperty.Register(nameof(Icone), typeof(string), typeof(MenuCard), new PropertyMetadata(""));
    public string Raccourci { get => (string)GetValue(RaccourciProperty); set => SetValue(RaccourciProperty, value); }
    public static readonly DependencyProperty RaccourciProperty = DependencyProperty.Register(nameof(Raccourci), typeof(string), typeof(MenuCard), new PropertyMetadata(""));
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(MenuCard));
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(MenuCard));
}
