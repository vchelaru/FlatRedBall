using System.Windows.Forms;

namespace MasterInstaller.Components.InstallableComponents
{
    public partial class DefaultInstallControl : UserControl
    {
        public static readonly DefaultInstallControl Self = new DefaultInstallControl();

        public DefaultInstallControl()
        {
            InitializeComponent();
        }

        public string InstallName
        {
            set { lblName.Text = value; }
        }

        public string InstallDescription
        {
            set { lblDescription.Text = value; }
        }
    }
}
