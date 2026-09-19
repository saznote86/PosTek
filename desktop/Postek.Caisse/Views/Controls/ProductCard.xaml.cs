using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Postec.Caisse.Caisse;

namespace Postec.Caisse.Views.Controls;

public partial class ProductCard : UserControl
{
    public static readonly DependencyProperty ArticleProperty =
        DependencyProperty.Register(nameof(Article), typeof(Article), typeof(ProductCard));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(ProductCard));

    public Article? Article
    {
        get => (Article?)GetValue(ArticleProperty);
        set => SetValue(ArticleProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public ProductCard()
    {
        InitializeComponent();
        DataContext = this;
    }
}
