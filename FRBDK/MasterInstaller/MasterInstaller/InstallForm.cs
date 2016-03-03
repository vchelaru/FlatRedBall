using System;
using System.Windows.Forms;
using MasterInstaller.Components;
using MasterInstaller.Components.MainComponents.Introduction;

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
            Controls.Add(component.MainControl);
            component.MainControl.Dock = DockStyle.Fill;
            component.MainControl.Margin = new System.Windows.Forms.Padding(0);

            await component.Show();
            //component.MovedToComponent();
        }

        //void component_MoveTo(object sender, MoveToEventArgs e)
        //{
        //    UnsetComponenet(_currentComponent);
        //    _currentComponent = e.Component;

        //    if (_currentComponent == null)
        //    {
        //        Environment.Exit(0);
        //    }
        //    else
        //    {
        //        SetComponent(_currentComponent);
        //    }
        //}

        private void UnsetComponenet(ComponentBase component)
        {
            Controls.Remove(component.MainControl);
        }
        
        private void InstallForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
