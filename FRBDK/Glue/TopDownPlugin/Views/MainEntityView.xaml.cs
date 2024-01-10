using System.Windows.Controls;
using TopDownPlugin.ViewModels;

namespace TopDownPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainEntityView.xaml
    /// </summary>
    public partial class MainEntityView : UserControl
    {
        TopDownEntityViewModel ViewModel =>
            DataContext as TopDownEntityViewModel;

        public MainEntityView()
        {
            InitializeComponent();
        }



    }
}
