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

namespace OfficialPlugins.Compiler
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        OutputParser outputParser;

        public event EventHandler BuildClicked;
        public event EventHandler BuildContentClicked;
        public event EventHandler RunClicked;
        public event EventHandler TestItClicked;

        public MainControl()
        {
            outputParser = new OutputParser();

            InitializeComponent();

        }

        private void HandleCompileClick(object sender, RoutedEventArgs e)
        {
            TextBox.Clear();
            BuildClicked?.Invoke(this, null);
        }

        private void HandleBuildContentClick(object sender, RoutedEventArgs e)
        {

            TextBox.Clear();
            BuildContentClicked?.Invoke(this, null);
        }

        private void HandleRunClick(object sender, RoutedEventArgs e)
        {
            RunClicked?.Invoke(this, null);
        }

        private void HandleTestItClicked(object sender, RoutedEventArgs e)
        {
            TestItClicked?.Invoke(this, null);
        }

        public void PrintOutput(string text)
        {
            var outputType = outputParser.GetOutputType(text);

            // suppress warnings...
            if(outputType != OutputType.Warning)
            {
                Glue.MainGlueWindow.Self.Invoke(() =>
                {
                    TextBox.AppendText(text + "\n");
                    TextBox.ScrollToEnd();
                });
            }
        }
    }
}
