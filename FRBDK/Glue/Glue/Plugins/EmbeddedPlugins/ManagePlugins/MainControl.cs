using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.Rss;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.ManagePlugins
{
    public partial class MainControl : UserControl
    {
        public AllFeed AllFeed
        {
            get
            {
                return this.pluginsWindow1.AllFeed;
            }
            set
            {
                this.pluginsWindow1.AllFeed = value;
            }
        }

        public DownloadState DownloadState
        {
            get
            {
                return this.pluginsWindow1.DownloadState;
            }
            set
            {
                this.pluginsWindow1.DownloadState = value;
            }
        }

        public MainControl()
        {
            InitializeComponent();
        }

        public void RefreshCheckboxes()
        {
            this.pluginsWindow1.RefreshCheckBoxes();
        }
    }
}
