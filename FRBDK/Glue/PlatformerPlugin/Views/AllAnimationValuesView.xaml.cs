using FlatRedBall.PlatformerPlugin.ViewModels;
using PlatformerPluginCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PlatformerPluginCore.Views
{
    /// <summary>
    /// Interaction logic for AllAnimationValuesView.xaml
    /// </summary>
    public partial class AllAnimationValuesView : UserControl
    {
        PlatformerEntityViewModel ViewModel => DataContext as PlatformerEntityViewModel;

        public AllAnimationValuesView()
        {
            InitializeComponent();
        }

        private void AddAnimationEntryButtonClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.AnimationRows.Add(new AnimationRowViewModel());
        }
    }
}
