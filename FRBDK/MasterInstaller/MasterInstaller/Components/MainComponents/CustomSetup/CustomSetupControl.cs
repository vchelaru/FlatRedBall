using System.Windows.Forms;

namespace MasterInstaller.Components.MainComponents.CustomSetup
{
    public partial class CustomSetupControl : UserControl
    {
        public CustomSetupControl()
        {
            InitializeComponent();
        }

        private void CustomSetupControl_Load(object sender, System.EventArgs e)
        {
            foreach (var component in ComponentStorage.InstallableComponents)
            {
                lvComponents.Items.Add(component.Key, component.Name, null);

                if (component.IsTypical)
                {
                    lvComponents.Items[component.Key].Checked = true;
                }
            }

            lvComponents.Refresh();
        }

        private void lvComponents_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            lblFeatureDescription.Text = "";
            if (!e.IsSelected) return;

            foreach (var component in ComponentStorage.InstallableComponents)
            {
                if (component.Key == e.Item.Name)
                {
                    lblFeatureDescription.Text = component.Description;
                    break;
                }
            }
        }
    }
}
