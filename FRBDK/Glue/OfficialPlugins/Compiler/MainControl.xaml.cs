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
        public event EventHandler StopClicked;
        public event EventHandler RestartGameClicked;
        public event EventHandler RestartGameCurrentScreenClicked;
        public event EventHandler RestartScreenClicked;
        public event EventHandler PauseClicked;
        public event EventHandler UnpauseClicked;

        

        public MainControl()
        {
            outputParser = new OutputParser();

            InitializeComponent();

        }

        private void HandleCompileClick(object sender, EventArgs e)
        {
            TextBox.Clear();
            BuildClicked?.Invoke(this, null);
        }

        private void HandleBuildContentClick(object sender, EventArgs e)
        {

            TextBox.Clear();
            BuildContentClicked?.Invoke(this, null);
        }

        private void HandleRunClick(object sender, EventArgs e)
        {
            RunClicked?.Invoke(this, null);
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

        private void WhileRunningView_StopClicked(object sender, EventArgs e)
        {
            StopClicked?.Invoke(this, null);
        }

        private void WhileRunningView_RestartGameClicked(object sender, EventArgs e)
        {
            RestartGameClicked?.Invoke(this, null);
        }

        private void WhileRunningView_RestartGameCurrentScreenClicked(object sender, EventArgs e)
        {
            RestartGameCurrentScreenClicked?.Invoke(this, null);
        }

        private void WhileRunningView_RestartScreenClicked(object sender, EventArgs e)
        {
            RestartScreenClicked?.Invoke(this, null);
        }

        private void WhileRunningView_PauseClicked(object sender, EventArgs e)
        {
            PauseClicked?.Invoke(this, null);
        }

        private void WhileRunningView_UnpauseClicked(object sender, EventArgs e)
        {
            UnpauseClicked?.Invoke(this, null);
        }
    }
}
