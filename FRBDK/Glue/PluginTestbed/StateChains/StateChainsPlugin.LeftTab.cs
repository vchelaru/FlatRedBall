using System.ComponentModel.Composition;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace PluginTestbed.StateChains
{
    [Export(typeof(ILeftTab))]
	public partial class StateChainsPlugin : ILeftTab
	{
        StateChainsPluginControl _control;
        PluginTab _tab;
        TabControl _tabControl;

        public void InitializeTab(TabControl tabControl)
        {
            _control = new StateChainsPluginControl(GlueCommands);
            _tab = new PluginTab();
            _tabControl = tabControl;

            _tab.ClosedByUser += TabClosedByUser;

            _tab.Text = @"  State Chains";
            _tab.Controls.Add(_control);
            _control.Dock = DockStyle.Fill;

            _tabControl.Controls.Add(_tab);
        }

        void TabClosedByUser(object sender)
        {
            PluginManager.ShutDownPlugin(this);
        }
	}
}
