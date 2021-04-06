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
    public partial class InitializationWindow : Form
    {
        public string Message
        {
            set
            {
                FlatRedBall.Glue.Managers.TaskManager.Self.OnUiThread(() =>
                {
                    this.TopLevelLabel.Text = value;
                    this.SubLabel.Text = "";

                });

            }
        }

        public string SubMessage
        {
            set
            {
                FlatRedBall.Glue.Managers.TaskManager.Self.OnUiThread(() =>
                {
                    this.SubLabel.Text = value;
                });
            }
        }

        public InitializationWindow()
        {
            InitializeComponent();
        }
    }
}
