using OfficialPluginsCore.Wizard.ViewModels;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

            foreach (var vm in ViewModel.Items)
            {
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
