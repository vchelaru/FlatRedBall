using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using PlatformerPluginCore.Controllers;
using PlatformerPluginCore.SaveClasses;
using PlatformerPluginCore.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace PlatformerPluginCore.Views
{
    /// <summary>
    /// Interaction logic for AnimationRow.xaml
    /// </summary>
    public partial class AnimationRow : UserControl
    {
        AnimationRowViewModel ViewModel => DataContext as AnimationRowViewModel;

        public event Action MoveUpClicked;
        public event Action MoveDownClicked;
        public event Action RemoveAnimationClicked;


        public AnimationRow()
        {
            InitializeComponent();

            DataContextChanged += HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AnimationController.InitializeDataUiGridToNewViewModel(DataUiGrid, ViewModel);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveAnimationClicked?.Invoke();
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            MoveUpClicked?.Invoke();
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            MoveDownClicked?.Invoke();
        }
    }
}