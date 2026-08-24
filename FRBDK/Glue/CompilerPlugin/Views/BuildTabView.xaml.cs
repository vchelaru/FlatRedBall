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

        /// <summary>
        /// Lines waiting to be shown. Filled from any thread, drained by <see cref="flushTimer"/>.
        /// </summary>
        readonly CompilerLibrary.OutputLineBuffer pendingLines = new();

        readonly System.Windows.Threading.DispatcherTimer flushTimer;

        /// <summary>
        /// Cap on retained lines. Live edit can print continuously for hours, and an unbounded
        /// FlowDocument grows without limit and gets progressively more expensive to lay out.
        /// </summary>
        public int MaxLinesOfText { get; set; } = 2000;

        const int LinesToDropWhenOverCap = 200;

        #endregion

        #region Events

        public event EventHandler BuildClicked;
        public event Action PackageClicked;
        public event EventHandler RunClicked;
        public event EventHandler CancelBuildClicked;
        public event Action MSBuildSettingsClicked;


        #endregion

        private Binding _foregroundBinding;

        public BuildTabView()
        {
            outputParser = new OutputParser();

            InitializeComponent();

            _foregroundBinding = new Binding("Foreground")
            {
                Source = TextBox,
                Mode = BindingMode.OneWay
            };

            flushTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            flushTimer.Tick += HandleFlushTimerTick;
            flushTimer.Start();
        }

        private void HandleCompileClick()
        {
            pendingLines.Clear();
            TextBox.Document.Blocks.Clear();
            BuildClicked?.Invoke(this, null);
        }

        private void HandleRunClick()
        {
            RunClicked?.Invoke(this, null);
        }

        private void HandleMSBuildSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            MSBuildSettingsClicked?.Invoke();
        }

        /// <summary>
        /// Queues output for display. Safe to call from any thread, and never blocks on the UI thread.
        /// </summary>
        public void PrintOutput(string text) => pendingLines.Add(text, isError: false);

        /// <inheritdoc cref="PrintOutput"/>
        public void PrintError(string text) => pendingLines.Add(text, isError: true);

        void HandleFlushTimerTick(object sender, EventArgs e)
        {
            if (pendingLines.Count == 0)
            {
                return;
            }

            var toAppend = pendingLines.TakeAll();

            try
            {
                foreach (var line in toAppend)
                {
                    // Warnings are suppressed, same as before this was buffered.
                    var outputType = line.IsError ? OutputType.Error : outputParser.GetOutputType(line.Text);
                    if (outputType == OutputType.Warning)
                    {
                        continue;
                    }

                    var run = new Run(line.Text);
                    if (outputType == OutputType.Error)
                    {
                        run.Foreground = Brushes.Red;
                    }
                    else
                    {
                        BindingOperations.SetBinding(run, ForegroundProperty, _foregroundBinding);
                    }

                    // One paragraph per line so the document can be trimmed by block. A single
                    // ever-growing paragraph cannot be, and re-lays out in full on every append.
                    var paragraph = new Paragraph(run) { Margin = new Thickness(0) };
                    TextBox.Document.Blocks.Add(paragraph);
                }

                ShortenOutputIfNecessary();

                TextBox.ScrollToEnd();
            }
            catch
            {
                // could be exiting the app so tolerate the error, don't show a message to the user
            }
        }

        void ShortenOutputIfNecessary()
        {
            if (TextBox.Document.Blocks.Count <= MaxLinesOfText)
            {
                return;
            }

            for (int i = 0; i < LinesToDropWhenOverCap && TextBox.Document.Blocks.FirstBlock != null; i++)
            {
                TextBox.Document.Blocks.Remove(TextBox.Document.Blocks.FirstBlock);
            }
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            pendingLines.Clear();
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
