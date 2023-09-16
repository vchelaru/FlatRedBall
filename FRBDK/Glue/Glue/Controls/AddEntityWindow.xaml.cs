using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Utilities;
using GlueFormsCore.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// Interaction logic for AddEntityWindow.xaml
    /// </summary>
    public partial class AddEntityWindow : Window
    {
        #region Fields/Properties

        public IReadOnlyCollection<UserControl> UserControlChildren
        {
            get
            {
                var listToReturn = new List<UserControl>();
                var uiElements = MainStackPanel.Children.Where(item => item is UserControl);
                foreach(var element in uiElements)
                {
                    listToReturn.Add(element as UserControl);
                }

                return listToReturn;
            }
        }

        AddEntityViewModel ViewModel => DataContext as AddEntityViewModel;

        #endregion

        #region Constructor

        public AddEntityWindow()
        {
            InitializeComponent();

            this.IsVisibleChanged += HandleVisibleChanged;
        }

        #endregion

        public void AddControl(UIElement element)
        {
            var indexToInsertAt = MainStackPanel.Children.Count - 1;

            // above ok/cancel buttons:
            this.MainStackPanel.Children.Insert(indexToInsertAt, element);
        }

        #region Event Handlers

        private void HandleVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(this.IsVisible)
            {
                TextBox.Focus();
            }
        }

        private void OkClickInternal(object sender, RoutedEventArgs e)
        {
            HandleOkClicked();
        }

        private void HandleOkClicked()
        {
            if(!string.IsNullOrEmpty(ViewModel.FailureText))
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(ViewModel.FailureText);
            }
            else
            {
                this.DialogResult = true;
            }
        }

        private void CancelClickInternal(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                HandleOkClicked();
            }
            if(e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
        }

        #endregion
    }
}
