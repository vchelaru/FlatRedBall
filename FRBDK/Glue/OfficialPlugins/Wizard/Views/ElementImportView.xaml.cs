using OfficialPluginsCore.Wizard.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

namespace OfficialPluginsCore.Wizard.Views
{
    /// <summary>
    /// Interaction logic for ElementImportView.xaml
    /// </summary>
    public partial class ElementImportView : UserControl
    {
        ElementImportViewModel ViewModel => DataContext as ElementImportViewModel;

        public ElementImportView()
        {
            InitializeComponent();

            this.DataContextChanged += HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(ViewModel != null)
            {
                ViewModel.Items.CollectionChanged += HandleItemsChanged;

                HandleItemsChanged(null, null);
            }
        }

        private void HandleItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            for(int i = MainStackPanel.Children.Count-1; i > -1; i--)
            {
                var view = MainStackPanel.Children[i] as ElementImportItem;

                var isVmContained = ViewModel.Items.Contains(view.DataContext as ElementImportItemViewModel);
                if(!isVmContained)
                {
                    MainStackPanel.Children.RemoveAt(i);
                }
            }

            for(int i = 0; i < ViewModel.Items.Count; i++)
            {
                var vm = ViewModel.Items[i];

                var isVmReferenced = MainStackPanel.Children
                    .Any(item => ((ElementImportItem)item).DataContext == vm);

                if(!isVmReferenced)
                {
                    var newView = new ElementImportItem();
                    newView.DataContext = vm;
                    MainStackPanel.Children.Add(newView);
                }
            }
        }
    }
}
