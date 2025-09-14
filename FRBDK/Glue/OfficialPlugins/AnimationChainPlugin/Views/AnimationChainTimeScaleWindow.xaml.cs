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
using System.Windows.Shapes;

namespace OfficialPlugins.AnimationChainPlugin.Views;
/// <summary>
/// Interaction logic for AnimationChainTimeScaleWindow.xaml
/// </summary>
public partial class AnimationChainTimeScaleWindow : Window
{
    public AnimationChainTimeScaleWindow()
    {
        InitializeComponent();
    }

    private void HandleOkClick(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
    }

    private void HandleCancelClick(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
    }
}
