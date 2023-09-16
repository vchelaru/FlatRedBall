using FlatRedBall.PlatformerPlugin.ViewModels;
using PlatformerPluginCore.ViewModels;
using System.Windows;
using System.Windows.Controls;

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
            var newVm = new AnimationRowViewModel();
            ViewModel.AssignAnimationRowEvents(newVm);
            ViewModel.AnimationRows.Add(newVm);
        }
    }
}
