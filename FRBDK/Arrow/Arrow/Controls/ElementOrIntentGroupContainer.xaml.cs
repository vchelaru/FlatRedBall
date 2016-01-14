using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Arrow.Managers;
using FlatRedBall.Arrow.ViewModels;
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

namespace FlatRedBall.Arrow.Controls
{
    /// <summary>
    /// Interaction logic for ElementOrIntentGroupContainer.xaml
    /// </summary>
    public partial class ElementOrIntentGroupContainer : UserControl
    {
        public ElementOrIntentGroupContainer()
        {
            InitializeComponent();
        }


        private void HandleAddElementInstanceClick(object sender, RoutedEventArgs e)
        {
            ArrowCommands.Self.Add.ElementInstance();
        }

        private void HandleAddInstanceClick(object sender, RoutedEventArgs e)
        {
            object dataContext = ((System.Windows.Controls.Control)sender).DataContext;

            if (dataContext is ArrowElementOrIntentVm)
            {
                ArrowElementVm aevm = ((ArrowElementOrIntentVm)dataContext).ElementVm;

                ArrowElementSave arrowElementSave = aevm.Model;

                var newInstance = ArrowCommands.Self.Add.ElementInstance(arrowElementSave.Name + "Instance", arrowElementSave);

                ArrowCommands.Self.Add.MakeNewObjectUnique(ArrowState.Self.CurrentArrowElementSave, newInstance);
            }

        }

    }
}
