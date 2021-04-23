using OfficialPluginsCore.Wizard.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace OfficialPluginsCore.Wizard.Views
{
    /// <summary>
    /// Interaction logic for DownloadingProgress.xaml
    /// </summary>
    public partial class DownloadingProgress : UserControl
    {
        WizardData ViewModel => DataContext as WizardData;
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
