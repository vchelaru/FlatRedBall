using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.OutputPlugin
{
    struct ColoredTextWpf
    {
        public SolidColorBrush Brush;
        public string Text;
    }

    /// <summary>
    /// Interaction logic for OutputControl.xaml
    /// </summary>
    public partial class OutputControl : UserControl
    {
        List<ColoredTextWpf> mBuffer = new List<ColoredTextWpf>();
        // Used to be 800,000, but increasing slightly to get more output
        const int MaxLength = 900000;
        const int LengthToReduceTo = 700000;

        System.Windows.Threading.DispatcherTimer timer;

        SolidColorBrush Error;
        SolidColorBrush Normal;

        public OutputControl()
        {
            InitializeComponent();

            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += HandleTimerTick;
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Start();

            Error = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            Normal = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }

        private void HandleTimerTick(object sender, EventArgs e)
        {
            lock (mBuffer)
            {
                if (mBuffer.Count != 0)
                {
                    for (int i = 0; i < mBuffer.Count; i++)
                    {
                        var block = new Paragraph();
                        block.Margin = new Thickness(0);
                        block.Foreground = mBuffer[i].Brush;
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
            Color color = Color.FromRgb(0,0,0);

            AppendText(output, Normal);
        }

        public void OnErrorOutput(string output)
        {
            Color color = Color.FromRgb(255,0,0);
            AppendText(output, Error);

        }


        private void AppendText(string output, SolidColorBrush brush)
        {
            lock (mBuffer)
            {
                mBuffer.Add(new ColoredTextWpf() { Brush = brush, Text = output });
            }
        }



        private void ShortenOutputIfNecessary()
        {
            if(this.TextBox.Document.Blocks.Count > 1000)
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
    }
}
