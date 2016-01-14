using System.Windows.Forms;
using MasterInstaller.Managers;

namespace MasterInstaller.Components.SetupComponents.FrbdkSetup
{
    public partial class FrbdkSetupControl : UserControl
    {
        public FrbdkSetupControl()
        {
            InitializeComponent();
        }

        private void CustomSetupControl_Load(object sender, System.EventArgs e)
        {
            lblPath.Text = FrbdkUpdaterManager.FrbdkInProgramFiles;
        }

        private void btnChange_Click(object sender, System.EventArgs e)
        {
            fbdPath.SelectedPath = lblPath.Text;
            if (fbdPath.ShowDialog() == DialogResult.OK)
            {
                lblPath.Text = fbdPath.SelectedPath;

                if (!lblPath.Text.EndsWith("\\") && !lblPath.Text.EndsWith("/"))
                {
                    lblPath.Text += "\\";
                }
            }
        }
    }
}
