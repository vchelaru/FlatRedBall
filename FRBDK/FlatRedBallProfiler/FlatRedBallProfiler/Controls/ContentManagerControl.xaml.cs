using System;
using System.Collections.Generic;
using System.Linq;
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
using FlatRedBallProfiler.ViewModels;

namespace FlatRedBallProfiler.Controls
{
    /// <summary>
    /// Interaction logic for ContentManagerControl.xaml
    /// </summary>
    public partial class ContentManagerControl : UserControl
    {
        AllContentManagersViewModel ViewModel
        {
            get
            {
                return DataContext as AllContentManagersViewModel;
            }
        }

        public ContentManagerControl()
        {
            InitializeComponent();

            this.DataContext = new AllContentManagersViewModel();
        }

        private void HandleRefreshClick(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateToEngine();
        }

        private void ContentItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (ViewModel != null && ViewModel.CurrentContentManager != null && ViewModel.CurrentContentManager.SelectedItem != null)
                {
                    ViewModel.CurrentContentManager.SelectedItem.Open();
                }
            }
        }

        private void SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is ContentManagerViewModel)
            {
                ViewModel.SelectedItem = e.NewValue as ContentManagerViewModel;
                ViewModel.CurrentContentManager = e.NewValue as ContentManagerViewModel;
            }
            else
            {
                ViewModel.SelectedItem = null;

                if(e.NewValue is ContentViewModel)
                {
                    var contentViewModel = e.NewValue as ContentViewModel;
                    ViewModel.CurrentContentManager = contentViewModel.Parent;
                    ViewModel.CurrentContentManager.SelectedItem = contentViewModel;
                }
            }

        }
    }
}
