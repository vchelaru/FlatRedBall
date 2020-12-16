using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Controls
{
    [Obsolete("Use MultiButtonMessageBoxWpf")]
    public partial class MultiButtonMessageBox : Form
    {
        #region Fields

        List<Button> mButtons = new List<Button>();

        #endregion

        public object ClickedTag
        {
            get;
            private set;
        }

        public string MessageText
        {
            set
            {
                label1.Text = value;
            }
        }

        void CloseThisWindow(object sender, EventArgs args)
        {
            this.Close();
        }

        public MultiButtonMessageBox()
        {
            InitializeComponent();
        }




        public void AddButton(string text, DialogResult result)
        {
            AddButton(text, result, null);
        }

        public void AddButton(string text, DialogResult result, object tag)
        {
            Button button = new Button();
            button.Click += new EventHandler(OnButtonClickInternal);
            button.Location = new System.Drawing.Point(12, 116 + 53 * mButtons.Count);
            button.Size = new System.Drawing.Size(518, 47);
            button.TabIndex = mButtons.Count;
            button.Text = text;
            button.DialogResult = result;
            button.UseVisualStyleBackColor = true;
            button.Tag = tag;

            mButtons.Add(button);

            this.Controls.Add(button);

            this.Size = new Size(
                this.Size.Width, System.Math.Max(Size.Height, button.Location.Y + button.Size.Height + 30));

        }

        void OnButtonClickInternal(object sender, EventArgs e)
        {
            ClickedTag = ((Button)sender).Tag;
        }
    }
}
