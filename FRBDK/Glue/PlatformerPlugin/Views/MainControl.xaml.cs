using FlatRedBall.PlatformerPlugin.ViewModels;
using System.Windows.Controls;

namespace FlatRedBall.PlatformerPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        PlatformerEntityViewModel ViewModel =>
            DataContext as PlatformerEntityViewModel;

        public MainControl()
        {
            InitializeComponent();
        }
    }
}
