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
        public Color Color;
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

        public OutputControl()
        {
            InitializeComponent();

            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += HandleTimerTick;
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Start();
        }

        private void HandleTimerTick(object sender, EventArgs e)
        {
            lock (mBuffer)
            {
                if (mBuffer.Count != 0)
                {
                    for (int i = 0; i < mBuffer.Count; i++)
                    {
                        //try
                        //{
                        //    TextBox.SelectionStart = TextBox.Text.Length;
                        //}
                        //catch
                        //{
                        //    break;
                        //}

                        //TextBox.SelectionColor = mBuffer[i].Color;

                        this.TextBox.AppendText(mBuffer[i].Text + "\n");

                        // It's focused right when the user clicks the tab, so need to figure this out.
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

        // TODO:
        // 1.  Make a maximum length for the text (see how I did this in Mother) - DONE
        // 2.  Make error text red (see how I did this in Mother) - DONE
        // 3.  Add buttons to delete entire text

        public void OnOutput(string output)
        {
            Color color = Color.FromRgb(0,0,0);

            AppendText(output, color);
        }

        public void OnErrorOutput(string output)
        {
            Color color = Color.FromRgb(255,0,0);
            AppendText(output, color);

        }


        private Color AppendText(string output, Color color)
        {
            lock (mBuffer)
            {
                mBuffer.Add(new ColoredTextWpf() { Color = color, Text = output });
            }



            return color;
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
    }
}
