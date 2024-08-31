using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.OutputPlugin
{
    struct ColoredTextWpf
    {
        public Brush Brush;
        public string Text;
    }

    /// <summary>
    /// Interaction logic for OutputControl.xaml
    /// </summary>
    public partial class OutputControl : UserControl
    {
        List<ColoredTextWpf> mBuffer = new();

        private readonly System.Windows.Threading.DispatcherTimer _timer;

        private readonly SolidColorBrush _error;
        private Brush _normal;

        public OutputControl()
        {
            InitializeComponent();

            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Tick += HandleTimerTick;
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Start();

            _error = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            _normal = TextBox.Foreground;
        }

        private void HandleTimerTick(object sender, EventArgs e)
        {
            lock (mBuffer)
            {
                if (_normal != TextBox.Foreground)
                {
                    ForegroundChanged(_normal, TextBox.Foreground);
                    _normal = TextBox.Foreground;
                }

                if (mBuffer.Count != 0)
                {
                    for (int i = 0; i < mBuffer.Count; i++)
                    {
                        var block = new Paragraph();
                        block.Margin = new Thickness(0);
                        if (mBuffer[i].Brush != _normal)
                        {
                            block.Foreground = mBuffer[i].Brush;
                        }
                        block.Inlines.Add(mBuffer[i].Text);
                        this.TextBox.Document.Blocks.Add(block);

                        //if(!this.TextBox.IsFocused)
                        {
                            this.TextBox.ScrollToEnd();
                        }
                    }
                    mBuffer.Clear();
                    ShortenOutputIfNecessary();

                }
            }
        }

        public void OnOutput(string output)
        {
            AppendText(output, _normal);
        }

        public void OnErrorOutput(string output)
        {
            AppendText(output, _error);
        }

        private void AppendText(string output, Brush brush)
        {
            lock (mBuffer)
            {
                mBuffer.Add(new ColoredTextWpf() { Brush = brush, Text = output });
            }
        }

        public int MaxLinesOfText { get; set; } = 1000;

        private void ShortenOutputIfNecessary()
        {
            if(this.TextBox.Document.Blocks.Count > MaxLinesOfText)
            {
                for(int i = 0; i < 100; i++)
                {
                    this.TextBox.Document.Blocks.Remove(this.TextBox.Document.Blocks.FirstBlock);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            lock(mBuffer)
            {
                this.TextBox.Document.Blocks.Clear();
            }
        }

        private void MaxLinesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(int.TryParse(MaxLinesTextBox.Text, out int parsedValue))
            {
                MaxLinesOfText = parsedValue;
            }
        }

        private void ForegroundChanged(Brush oldValue, Brush newValue)
        {
            // Loop through all the paragraphs and update their foreground if not an error
            foreach (var block in TextBox.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    if (paragraph.Foreground == oldValue) // Avoid changing error paragraphs
                    {
                        paragraph.Foreground = newValue;
                    }
                }
            }
        }
    }
}
