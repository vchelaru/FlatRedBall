using Glue;
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
    /// Interaction logic for MultiButtonMessageBoxWpf.xaml
    /// </summary>
    public partial class MultiButtonMessageBoxWpf : System.Windows.Window
    {
        #region Fields/Properties

        List<Button> buttons = new List<Button>();

        public object ClickedResult
        {
            get;
            private set;
        }

        public string MessageText
        {
            set
            {
                LabelInstance.Text = value;
            }
        }

        #endregion

        public MultiButtonMessageBoxWpf()
        {
            InitializeComponent();

            this.KeyDown += HandleKeyDown;

            // Can this be done in the constructor?
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;

            double width = this.Width;
            if (double.IsNaN(width))
            {
                width = 0;
            }
            double height = this.Height;
            if (double.IsNaN(height))
            {
                height = 0;
            }

            this.Left = System.Math.Max(0, MainGlueWindow.MousePosition.X - width / 2);
            this.Top = MainGlueWindow.MousePosition.Y - height / 2;


        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        public void AddButton(string text, object result)
        {
            Button button = new Button();
            button.Click += HandleButtonClickInternal;
            button.TabIndex = buttons.Count;
            button.Content = text;
            button.Tag  = result;

            button.Margin = new Thickness(8,4,8,4);

            button.Height = 30;

            buttons.Add(button);

            this.MainPanel.Children.Add(button);
        }

        private void HandleButtonClickInternal(object sender, RoutedEventArgs e)
        {
            ClickedResult = ((Button)sender).Tag;

            this.DialogResult = true;
        }
    }
}
