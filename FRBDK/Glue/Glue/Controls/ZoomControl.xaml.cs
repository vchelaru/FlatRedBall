using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlatRedBall.Glue.Controls;
/// <summary>
/// Interaction logic for ZoomControl.xaml
/// </summary>
public partial class ZoomControl : UserControl
{
    public event Action? ZoomMinusClick;

    public event Action? ZoomPlusClick;

    public static readonly DependencyProperty ButtonVisibilityProperty =
    DependencyProperty.Register(
        nameof(ButtonVisibility),
        typeof(Visibility),
        typeof(ZoomControl),
        new PropertyMetadata(Visibility.Visible));

    // CLR Property wrapper
    public Visibility ButtonVisibility
    {
        get => (Visibility)GetValue(ButtonVisibilityProperty);
        set => SetValue(ButtonVisibilityProperty, value);
    }


    public static readonly DependencyProperty CurrentZoomLevelDisplayProperty =
    DependencyProperty.Register(
        nameof(CurrentZoomLevelDisplay),
        typeof(string),
        typeof(ZoomControl),
        new PropertyMetadata("100%"));

    // CLR Property wrapper
    public string CurrentZoomLevelDisplay
    {
        get => (string)GetValue(CurrentZoomLevelDisplayProperty);
        set => SetValue(CurrentZoomLevelDisplayProperty, value);
    }

    public ZoomControl()
    {
        InitializeComponent();
    }

    private void ZoomMinusClicked(object sender, RoutedEventArgs e)
    {
        ZoomMinusClick?.Invoke();
    }

    private void ZoomPlusClicked(object sender, RoutedEventArgs e)
    {
        ZoomPlusClick?.Invoke();
    }
}
