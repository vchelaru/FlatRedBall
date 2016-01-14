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
using System.Windows.Shapes;

namespace FlatRedBall.Arrow.Gui
{
    /// <summary>
    /// Interaction logic for TextInputWindow.xaml
    /// </summary>
    public partial class TextInputWindow : Window
    {
        #region Properties

        public string Result
        {
            get
            {
                return this.TextBox.Text;
            }
            set
            {
                this.TextBox.Text = value;
            }
        }

        public string Text
        {
            get
            {
                return this.Label.Content as string;
            }
            set
            {
                this.Label.Content = value;
            }

        }

        #endregion

        #region Constructor

        public TextInputWindow()
        {
            InitializeComponent();
        }

        #endregion

        public void AddControl(Control control)
        {
            this.CustomPanel.Children.Add(control);
        }

        public TreeView AddTreeView(IEnumerable<object> listOfItems)
        {
            TreeView treeView = new TreeView();
            treeView.HorizontalAlignment = HorizontalAlignment.Stretch;
            treeView.VerticalAlignment = VerticalAlignment.Top;
            treeView.Height = 80;
            treeView.Margin = new Thickness(3);

            List<object> itemsSource = new List<object>();
            itemsSource.AddRange(listOfItems);
            treeView.ItemsSource = itemsSource;

            AddControl(treeView);

            return treeView;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            var point = System.Windows.Forms.Control.MousePosition;
            this.Top = point.Y;
            this.Left = point.X - this.Width / 2;

            this.TextBox.Focus();
        }

    }
}
