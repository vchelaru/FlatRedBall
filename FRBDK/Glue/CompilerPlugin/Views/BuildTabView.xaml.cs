using CompilerPlugin.Managers;
using FlatRedBall.Math.Geometry;
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

namespace CompilerPlugin.Views
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class BuildTabView : UserControl
    {
        #region Fields/Properties

        OutputParser outputParser;

        #endregion

        #region Events

        public event EventHandler BuildClicked;
        public event Action PackageClicked;
        public event EventHandler RunClicked;
        public event EventHandler CancelBuildClicked;
        public event Action MSBuildSettingsClicked;


        #endregion

        public BuildTabView()
        {
            outputParser = new OutputParser();

            InitializeComponent();

            TextBox.Document.Blocks.Add(new Paragraph());
        }

        private void HandleCompileClick()
        {
            TextBox.Document.Blocks.Clear();
            TextBox.Document.Blocks.Add(new Paragraph());
            BuildClicked?.Invoke(this, null);
        }

        private void HandleRunClick()
        {
            RunClicked?.Invoke(this, null);
        }

        private void HandleMSBuildSettingsClicked()
        {
            MSBuildSettingsClicked?.Invoke();
        }

        public void PrintOutput(string text)
        {
            //////////////////////////////////// Early out ////////////////////////////////////////////
            if (text == null) return;
            //////////////////////////////////End Early Out////////////////////////////////////////////

            // suppress warnings...
            var split = text.Split('\n');

            try
            {
                Glue.MainGlueWindow.Self.Invoke(() =>
                {
                    var paragraph = TextBox.Document.Blocks.LastOrDefault() as Paragraph;
                    if(paragraph == null)
                    {
                        paragraph = new Paragraph();
                        TextBox.Document.Blocks.Add(paragraph);
                    }
                    foreach (var line in split)
                    {
                        if(!string.IsNullOrWhiteSpace(line))
                        {
                            var outputType = outputParser.GetOutputType(line);
                            if(outputType != OutputType.Warning)
                            {
                                var color = outputType == OutputType.Error ? Brushes.Red : Brushes.Black;
                                paragraph.Inlines.Add(new Run(line + "\r\n") { Foreground = color});
                            }
                        }
                    }

                    TextBox.ScrollToEnd();
                });
            }
            catch
            {
                // could be exiting the app so tolerate the error, don't show a message to the user
            }
        }

        public void PrintError(string text)
        {
            //////////////////////////////////// Early out ////////////////////////////////////////////
            if (string.IsNullOrEmpty(text)) return;
            //////////////////////////////////End Early Out////////////////////////////////////////////

            // suppress warnings...
            var split = text.Split('\n');

            try
            {
                Glue.MainGlueWindow.Self.Invoke(() =>
                {
                    var paragraph = TextBox.Document.Blocks.LastOrDefault() as Paragraph;
                    if (paragraph == null)
                    {
                        paragraph = new Paragraph();
                        TextBox.Document.Blocks.Add(paragraph);
                    }
                    foreach (var line in split)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            paragraph.Inlines.Add(new Run(line) { Foreground = Brushes.Red });
                        }
                    }
                    if(split.Length > 0)
                    {
                        paragraph.Inlines.Add(new Run("\r\n") { Foreground = Brushes.Red });
                    }

                    TextBox.ScrollToEnd();
                });
            }
            catch
            {
                // could be exiting the app so tolerate the error, don't show a message to the user
            }
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            TextBox.Document.Blocks.Clear();
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

        private void HandleCancelBuildButtonClick(object sender, RoutedEventArgs e) => CancelBuildClicked?.Invoke(this, null);

        private void WhileStoppedView_PackageClicked()
        {
            // build, then package it:
            PackageClicked?.Invoke();
        }
    }
}
