using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace OfficialPlugins.FrbUpdater
{
    public partial class FrbUpdaterPluginForm : Form
    {
        const bool ShowCurrent = false;

        private FrbUpdaterSettings _settings;
        private readonly FrbUpdaterPlugin _plugin;

        public FrbUpdaterPluginForm(FrbUpdaterPlugin plugin)
        {
            StartPosition = FormStartPosition.Manual;

            InitializeComponent();
            _plugin = plugin;
        }

        private void BuildMenu()
        {
            cbSyncTo.Items.Clear();

            cbSyncTo.Items.Add("Daily Build");
        }


        private void FrbUpdaterPluginForm_Load(object sender, EventArgs e)
        {
            BuildMenu();
            _settings = FrbUpdaterSettings.LoadSettings();
            cbSyncTo.Text = _settings.SelectedSource;
            if (!ShowCurrent && cbSyncTo.Text == "Current")
            {
                cbSyncTo.Text = "Daily Build";
            }
            chkAutoUpdate.Checked = _settings.AutoUpdate;
        }

        private void chkAutoUpdate_CheckedChanged(object sender, EventArgs e)
        {
            _settings.AutoUpdate = chkAutoUpdate.Checked;
        }

        private void btnUpdater_Click(object sender, EventArgs e)
        {
            var inList = false;

            inList = cbSyncTo.Items.Cast<string>().Any( item => cbSyncTo.Text == item);


            if (!inList)
            {
                MessageBox.Show(@"Must pick source to sync to.");
                return;
            }

            _settings.SelectedSource = cbSyncTo.Text;
            _settings.AutoUpdate = chkAutoUpdate.Checked;
            _settings.SaveSettings();

            var window = new UpdateWindow(_plugin);

            if (window.Owner == null)
                window.TopMost = true;
            window.Show(GlueCommands.Self.DialogCommands.Win32Window);
            Close();
        }

        private void FrbUpdaterPluginForm_Shown(object sender, EventArgs e)
        {

            // This will be set in OnShow after all controls 
            // have been added because we want to center the control 
            // where the mouse is.
            Location = new Point(FrbUpdaterPluginForm.MousePosition.X - this.Width/2, FrbUpdaterPluginForm.MousePosition.Y - this.Height/2);
        }
    }
}
