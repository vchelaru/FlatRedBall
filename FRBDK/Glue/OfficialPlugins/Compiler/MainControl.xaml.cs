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
        #region Fields/Properties

        OutputParser outputParser;

        #endregion

        #region Events

        public event EventHandler BuildClicked;
        public event EventHandler BuildContentClicked;
        public event EventHandler RunClicked;


        #endregion

        public MainControl()
        {
            outputParser = new OutputParser();

            InitializeComponent();

            TextBox.Document.Blocks.Add(new Paragraph());
        }

        private void HandleCompileClick(object sender, EventArgs e)
        {
            TextBox.Document.Blocks.Clear();
            TextBox.Document.Blocks.Add(new Paragraph());
            BuildClicked?.Invoke(this, null);
        }

        private void HandleBuildContentClick(object sender, EventArgs e)
        {
            TextBox.Document.Blocks.Clear();
            TextBox.Document.Blocks.Add(new Paragraph());
            BuildContentClicked?.Invoke(this, null);
        }

        private void HandleRunClick(object sender, EventArgs e)
        {
            RunClicked?.Invoke(this, null);
        }

        public void PrintOutput(string text)
        {

            // suppress warnings...
            var split = text.Split('\n');

            Glue.MainGlueWindow.Self.Invoke(() =>
            {
                var paragraph = TextBox.Document.Blocks.Last() as Paragraph;
                foreach (var line in split)
                {
                    
                    var outputType = outputParser.GetOutputType(line);
                    if(outputType != OutputType.Warning)
                    {
                        var color = outputType == OutputType.Error ? Brushes.Red : Brushes.Black;
                        paragraph.Inlines.Add(new Run(line + "\r\n") { Foreground = color});
                    }
                }
            });
        }

        private void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = System.Windows.Controls.TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null) { binding.UpdateSource(); }

                e.Handled = true;
            }
        }

    }
}
