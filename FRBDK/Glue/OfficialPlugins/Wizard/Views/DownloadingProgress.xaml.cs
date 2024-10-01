using OfficialPlugins.Wizard.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace OfficialPlugins.Wizard.Views
{
    /// <summary>
    /// Interaction logic for DownloadingProgress.xaml
    /// </summary>
    public partial class DownloadingProgress : UserControl
    {
        WizardViewModel ViewModel => DataContext as WizardViewModel;
        public DownloadingProgress()
        {
            InitializeComponent();

            DataContextChanged += HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(ViewModel != null)
            {
                ViewModel.PropertyChanged += HandlePropertyChanged;
                UpdateTasks();
            }
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(ViewModel.Tasks))
            {
                UpdateTasks();
            }
        }

        private void UpdateTasks()
        {
            if (ViewModel?.Tasks != null)
            {
                foreach (var task in ViewModel.Tasks)
                {
                    var view = new TaskItemView();
                    view.DataContext = task;
                    TaskStack.Children.Add(view);
                }
            }
        }
    }
}
