using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Postec.Caisse.Views.Controls;

public partial class NumericKeypad : UserControl
{
    public NumericKeypad() => InitializeComponent();

    public ICommand? NumberPressedCommand
    {
        get => (ICommand?)GetValue(NumberPressedCommandProperty);
        set => SetValue(NumberPressedCommandProperty, value);
    }

    public static readonly DependencyProperty NumberPressedCommandProperty =
        DependencyProperty.Register(nameof(NumberPressedCommand), typeof(ICommand), typeof(NumericKeypad));

    public ICommand? KeyCommand
    {
        get => NumberPressedCommand;
        set => NumberPressedCommand = value;
    }
}
