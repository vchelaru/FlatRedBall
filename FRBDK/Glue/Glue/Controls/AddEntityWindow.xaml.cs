using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Utilities;
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

        bool hasUserUncheckedICollidable = false;

        public string EnteredText
        {
            get { return TextBox.Text; }
            set { TextBox.Text = value; }
        }

        public bool SpriteChecked
        {
            get { return SpriteCheckBox.IsChecked == true; }
            set { SpriteCheckBox.IsChecked = value; }
        }

        public bool TextChecked
        {
            get { return TextCheckBox.IsChecked == true; }
            set { TextCheckBox.IsChecked = value; }
        }

        public bool CircleChecked
        {
            get { return CircleCheckBox.IsChecked == true; }
            set { CircleCheckBox.IsChecked = value; }
        }

        public bool AxisAlignedRectangleChecked
        {
            get { return AxisAlignedRectangleCheckBox.IsChecked == true; }
            set { AxisAlignedRectangleCheckBox.IsChecked = value; }
        }

        public bool PolygonChecked
        {
            get { return PolygonCheckBox.IsChecked == true; }
            set { PolygonCheckBox.IsChecked = value; }
        }

        public bool IVisibleChecked
        {
            get { return IVisibleCheckBox.IsChecked == true; }
            set { IVisibleCheckBox.IsChecked = value; }
        }

        public bool IClickableChecked
        {
            get { return IClickableCheckBox.IsChecked == true; }
            set { IClickableCheckBox.IsChecked = value; }
        }

        public bool IWindowChecked
        {
            get { return IWindowCheckBox.IsChecked == true; }
            set { IWindowCheckBox.IsChecked = value; }
        }

        public bool ICollidableChecked
        {
            get { return ICollidableCheckBox.IsChecked == true; }
            set { ICollidableCheckBox.IsChecked = value; }
        }

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

        private void CircleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if(CircleCheckBox.IsChecked == true && !hasUserUncheckedICollidable)
            {
                ICollidableChecked = true;
            }
        }

        private void AxisAlignedRectangleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (AxisAlignedRectangleCheckBox.IsChecked == true && !hasUserUncheckedICollidable)
            {
                ICollidableChecked = true;
            }
        }

        private void PolygonCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (PolygonCheckBox.IsChecked == true && !hasUserUncheckedICollidable)
            {
                ICollidableChecked = true;
            }
        }

        private void ICollidableCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if(!ICollidableCheckBox.IsChecked == false)
            {
                hasUserUncheckedICollidable = true;
            }
        }

        private void IWindowCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if(IWindowCheckBox.IsChecked == true)
            {
                IVisibleCheckBox.IsChecked = true;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string whyIsntValid;

            var isValid = NameVerifier.IsEntityNameValid(TextBox.Text, null, out whyIsntValid);

            if(isValid)
            {
                FailureTextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                FailureTextBlock.Visibility = Visibility.Visible;
                FailureTextBlock.Text = whyIsntValid;
            }
        }

        #endregion
    }
}
