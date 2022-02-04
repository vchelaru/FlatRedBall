using OfficialPluginsCore.Compiler.ViewModels;
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

namespace OfficialPlugins.Compiler
{
    /// <summary>
    /// Interaction logic for RunnerToolbar.xaml
    /// </summary>
    public partial class RunnerToolbar : UserControl
    {
        public event EventHandler RunClicked;

        RunnerToolbarViewModel ViewModel => DataContext as RunnerToolbarViewModel;

        public bool IsOpen
        {
            get => SplitButton.IsOpen;
            set => SplitButton.IsOpen = value;
        }

        public RunnerToolbar()
        {
            InitializeComponent();

            InitializeItemsControlTemplate();
        }

        private void InitializeItemsControlTemplate()
        {
        }

        private void HandleButtonClick(object sender, RoutedEventArgs args)
        {
            RunClicked?.Invoke(this, null);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var screenName = (sender as MenuItem).Header as string;

            ViewModel.StartupScreenName = screenName;

            IsOpen = false;
        }
    }
}
