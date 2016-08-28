using FlatRedBall.PlatformerPlugin.ViewModels;
using FlatRedBall.Utilities;
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

namespace FlatRedBall.PlatformerPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        PlatformerEntityViewModel ViewModel
        {
            get
            {
                return DataContext as PlatformerEntityViewModel;
            }
        }

        public MainControl()
        {
            InitializeComponent();
        }

        private void AddPlatformerValuesClick(object sender, RoutedEventArgs e)
        {
            string name = "Unnamed";

            while(ViewModel.PlatformerValues.Any(item=>item.Name == name))
            {
                name = FlatRedBall.Utilities.StringFunctions.IncrementNumberAtEnd(name);
            }

            var values = new PlatformerValuesViewModel();
            values.Name = name;


            ViewModel.PlatformerValues.Add(values);
        }

        private void PlatformerValuesXClick(object sender, RoutedEventArgs e)
        {
            var valuesViewModel = (sender as UserControl).DataContext as PlatformerValuesViewModel;

            bool contains = ViewModel.PlatformerValues.Contains(valuesViewModel);

            if(contains)
            {
                ViewModel.PlatformerValues.Remove(valuesViewModel);
            }
        }
    }
}
