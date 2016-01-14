using System;
using System.Windows.Forms;
using MasterInstaller.Components;
using MasterInstaller.Components.MainComponents.Introduction;

namespace MasterInstaller
{
    public partial class InstallForm : Form
    {
        private ComponentBase _currentComponent;

        public InstallForm()
        {
            InitializeComponent();
        }

        private void InstallForm_Load(object sender, EventArgs e)
        {
            Show();
            _currentComponent = new IntroductionComponent();
            SetComponent(_currentComponent);
        }

        private void SetComponent(ComponentBase component)
        {
            tlpMain.Controls.Clear();

            tlpMain.Controls.Add(panelButtons, 0, 1);
            var newControl = component.MainControl;

            if (newControl == null)
            {
                throw new Exception("The component " + component.GetType().Name + " does not define a Control.  Every component needs a control");
            }
            tlpMain.Controls.Add(component.MainControl, 0, 0);
            component.MainControl.Dock = DockStyle.Fill;
            component.MainControl.Margin = new System.Windows.Forms.Padding(0);

            component.NextChanged += NextChanged_Handler;
            component.PreviousChanged += PreviousChanged_Handler;
            component.MoveToNext += btnNext_Click;
            component.MoveTo += component_MoveTo;

            component.MovedToComponent();
        }

        void component_MoveTo(object sender, MoveToEventArgs e)
        {
            if (!_currentComponent.MovingNextFromComponent())
            {
                return;
            }
            UnsetComponenet(_currentComponent);
            _currentComponent = e.Component;

            if (_currentComponent == null)
            {
                Environment.Exit(0);
            }
            else
            {
                SetComponent(_currentComponent);
            }
        }

        private void UnsetComponenet(ComponentBase component)
        {
            tlpMain.Controls.Remove(component.MainControl);

            component.NextChanged -= NextChanged_Handler;
            component.PreviousChanged -= PreviousChanged_Handler;
            component.MoveToNext -= btnNext_Click;
            component.MoveTo -= component_MoveTo;
        }

        private void PreviousChanged_Handler(object sender, EnableButtonEventArgs e)
        {
            btnBack.Enabled = e.ButtonEnabled;
            btnBack.Text = string.IsNullOrEmpty(e.ButtonText) ? "< Back" : e.ButtonText;
        }

        private void NextChanged_Handler(object sender, EnableButtonEventArgs e)
        {
            btnNext.Enabled = e.ButtonEnabled;
            btnNext.Text = string.IsNullOrEmpty(e.ButtonText) ? "Next >" : e.ButtonText;
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (!_currentComponent.MovingBackFromComponent())
            {
                return;
            }
            UnsetComponenet(_currentComponent);
            _currentComponent = _currentComponent.PreviousComponent;
            SetComponent(_currentComponent);
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (!_currentComponent.MovingNextFromComponent())
            {
                return;
            }
            UnsetComponenet(_currentComponent);
            _currentComponent = _currentComponent.NextComponent;

            if (_currentComponent == null)
            {
                Environment.Exit(0);
            }
            else
            {
                SetComponent(_currentComponent);
            }
        }

        private void InstallForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
