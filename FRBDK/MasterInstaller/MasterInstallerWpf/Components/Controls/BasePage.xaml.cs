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

namespace MasterInstaller.Components.Controls
{
    /// <summary>
    /// Interaction logic for BasePage.xaml
    /// </summary>
    public partial class BasePage : UserControl
    {
        public event EventHandler NextClicked;

        public string Title
        {
            get
            {
                return PageTitleTextBlock.Text;
            }
            set
            {
                PageTitleTextBlock.Text = value;
            }
        }

        public BasePage()
        {
            InitializeComponent();
        }

        protected TextBlock SetLeftText(string leftText)
        {
            var textBlock = new TextBlock();
            textBlock.Text = leftText;
            textBlock.SetValue(Grid.RowProperty, 1);
            textBlock.FontSize = 16;
            textBlock.LineHeight = 24;
            textBlock.TextWrapping = System.Windows.TextWrapping.Wrap;
            LeftPanel.Children.Add(textBlock);

            return textBlock;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NextClicked?.Invoke(this, null);
        }
    }
}
