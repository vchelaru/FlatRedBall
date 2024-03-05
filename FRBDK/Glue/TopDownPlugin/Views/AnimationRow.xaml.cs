using System;
using System.Windows;
using System.Windows.Controls;
using TopDownPlugin.Controllers;
using TopDownPlugin.ViewModels;

namespace TopDownPlugin.Views
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
