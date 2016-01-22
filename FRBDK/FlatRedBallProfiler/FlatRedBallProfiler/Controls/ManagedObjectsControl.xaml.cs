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
    /// Interaction logic for ManagedObjectsControl.xaml
    /// </summary>
    public partial class ManagedObjectsControl : UserControl
    {
        public ManagedObjectsControl()
        {
            InitializeComponent();

            this.DataContext = new ManagedObjectsViewModel();

            ProfilerCommands.Self.RegisterManagedObjectsTreeView(TreeView);

        }
        
        private void HandleRefreshClick(object sender, RoutedEventArgs e)
        {
            (this.DataContext as ManagedObjectsViewModel).ManualRefresh();

            ProfilerCommands.Self.RefreshManagedObjects();
            
        }
    }
}
