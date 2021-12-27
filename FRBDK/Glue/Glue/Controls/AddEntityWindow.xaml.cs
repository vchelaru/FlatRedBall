using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Utilities;
using GlueFormsCore.ViewModels;
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
            this.DialogResult = true;
        }

        private void CancelClickInternal(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                this.DialogResult = true;
            }
            if(e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
        }

        #endregion
    }
}
