using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins
{

    struct ColoredText
    {
        public Color Color;
        public string Text;


    }

    public partial class OutputWindow : UserControl
    {
        List<ColoredText> mBuffer = new List<ColoredText>();
        const int MaxLength = 800000;
        const int LengthToReduceTo = 700000;

        Timer mTimer;

        public OutputWindow()
        {
            InitializeComponent();
            mTimer = new Timer();
            mTimer.Interval = 100;
            mTimer.Tick += HandleTimerTick;
            mTimer.Start();
        }

        private void HandleTimerTick(object sender, EventArgs e)
        {
            lock (mBuffer)
            {
                if (mBuffer.Count != 0)
                {
                    for (int i = 0; i < mBuffer.Count; i++)
                    {
                        if (!TextBox.IsDisposed)
                        {
                            try
                            {
                                TextBox.SelectionStart = TextBox.Text.Length;
                            }
                            catch
                            {
                                break;
                            }

                            TextBox.SelectionColor = mBuffer[i].Color;

                            this.TextBox.AppendText(mBuffer[i].Text + "\n");

                            this.TextBox.ScrollToCaret();
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
            Color color = Color.Black;

            AppendText(output, color);
        }

        public void OnErrorOutput(string output)
        {
            Color color = Color.Red;
            AppendText(output, color);

        }


        private Color AppendText(string output, Color color)
        {
            lock (mBuffer)
            {
                mBuffer.Add(new ColoredText() { Color = color, Text = output });
            }
            


            return color;
        }



        private void ShortenOutputIfNecessary()
        {
            if (this.TextBox.Text.Length > MaxLength)
            {
                this.TextBox.Text = this.TextBox.Text.Substring(TextBox.Text.Length - LengthToReduceTo);
            }
        }

    }
}
