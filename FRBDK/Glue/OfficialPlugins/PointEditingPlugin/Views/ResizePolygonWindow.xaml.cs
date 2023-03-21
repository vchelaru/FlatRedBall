using FlatRedBall.Glue.MVVM;
using GlueFormsCore.Extensions;
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

namespace OfficialPlugins.PointEditingPlugin.Views;

/// <summary>
/// Interaction logic for ResizePolygonWindow.xaml
/// </summary>
public partial class ResizePolygonWindow : Window
{

    ResizePolygonViewModel ViewModel => DataContext as ResizePolygonViewModel;

    public double WidthPercentage => ViewModel.WidthPercentage;
    public double HeightPercentage => ViewModel.HeightPercentage;

    public ResizePolygonWindow()
    {
        InitializeComponent();

        var vm = new ResizePolygonViewModel();
        vm.WidthPercentage = 100;
        vm.HeightPercentage = 100;
        DataContext = vm;

        this.Loaded += HandleLoaded;
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        this.MoveToCursor();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true;
    }
}

// maybe move to a different file eventually?
public class ResizePolygonViewModel : ViewModel
{
    public double WidthPercentage
    {
        get => Get<double>();
        set => Set(value);
    }

    public double HeightPercentage
    {
        get => Get<double>();
        set => Set(value);
    }
}
