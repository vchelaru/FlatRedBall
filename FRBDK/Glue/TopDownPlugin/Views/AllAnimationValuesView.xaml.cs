using System.Windows;
using System.Windows.Controls;
using TopDownPlugin.ViewModels;

namespace TopDownPlugin.Views
{
    /// <summary>
    /// Interaction logic for AllAnimationValuesView.xaml
    /// </summary>
    public partial class AllAnimationValuesView : UserControl
    {
        TopDownEntityViewModel ViewModel => DataContext as TopDownEntityViewModel;
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
