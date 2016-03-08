using System;
using System.Windows.Forms;
using MasterInstaller.Components;
using MasterInstaller.Components.MainComponents.Introduction;
using System.Windows.Forms.Integration;

namespace MasterInstaller
{
    public partial class InstallForm : Form
    {
        private ComponentBase _currentComponent;

        MainFlow mainFlow;

        public InstallForm()
        {
            InitializeComponent();
        }

        private void InstallForm_Load(object sender, EventArgs e)
        {
            Show();

            mainFlow = new MainFlow();
            mainFlow.MainForm = this;

            mainFlow.StartFlow();

        }

        public async void SetComponent(ComponentBase component)
        {
            _currentComponent = component;
            Controls.Clear();

            var newControl = component.MainControl;

            if (newControl == null)
            {
                throw new Exception("The component " + component.GetType().Name + " does not define a Control.  Every component needs a control");
            }


            var host = new ElementHost();
            host.Child = newControl;

            Controls.Add(host);
            host.Dock = DockStyle.Fill;
            host.Margin = new System.Windows.Forms.Padding(0);

            await component.Show();
        }

        private void InstallForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
