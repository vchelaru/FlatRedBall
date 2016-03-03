using MasterInstaller.Components.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MasterInstaller.Components.MainComponents.CustomSetup
{
    public class ComponentSelectionControl : BasePage
    {
        public List<ComponentViewModel> ViewModels
        {
            get;
            private set;
        }

        public ComponentSelectionControl() : base()
        {
            Title = "Select Components to Install";


            SetLeftText("Choose the components on the right that you would like " +
                "to install. We recommend installing all components.");

            CreateComponentsListBox();
        }

        private void CreateComponentsListBox()
        {
            var listView = CreateListView();

            ViewModels = new List<ComponentViewModel>();

            foreach (var component in ComponentStorage.GetInstallableComponents())
            {
                var viewModel = new ComponentViewModel();
                viewModel.IsSelected = true;
                viewModel.BackingData = component;
                ViewModels.Add(viewModel);
            }

            listView.ItemsSource = ViewModels;
            listView.HorizontalAlignment = HorizontalAlignment.Stretch;
            listView.VerticalAlignment = VerticalAlignment.Stretch;
            base.RightPanel.Children.Add(listView);

        }

        private ListView CreateListView()
        {
            ListView listView = new ListView();
            DataTemplate template = new DataTemplate(typeof(CheckBox));
            listView.ItemTemplate = template;

            // Create binding
            Binding binding = new Binding();
            binding.Path = new PropertyPath("Name");
            binding.Mode = BindingMode.TwoWay;

            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(CheckBox));
            factory.SetBinding(CheckBox.ContentProperty, binding);
            factory.SetBinding(CheckBox.IsCheckedProperty,
                new Binding { Path = new PropertyPath("IsSelected"), Mode = BindingMode.TwoWay}
                );
            factory.SetValue(CheckBox.VerticalContentAlignmentProperty, VerticalAlignment.Center);

            template.VisualTree = factory;
            

            return listView;
        }

    }
}
