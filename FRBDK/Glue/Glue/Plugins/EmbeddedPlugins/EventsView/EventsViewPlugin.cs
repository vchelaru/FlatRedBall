using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Controls;
using GlueControls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueControls;
using GlueControls.ViewModels.Event;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.EventsView
{
    [Export(typeof(PluginBase))]
    public class EventsViewPlugin : EmbeddedPlugin
    {


        private System.Windows.Forms.Integration.ElementHost mWpfHost;
        TabControl mContainer; // This is the tab control for all tabs on the left
        PluginTab mTab; // This is the tab that will hold our control
        EventItemList mEventItemList;

        public override void StartUp()
        {

            //elementHost1.Child = this.testWpfControl1;


            //this.InitializeCenterTabHandler = HandleTabInitialize;
            //this.ReactToItemSelectHandler = HandleItemSelect;
        }

        private void HandleItemSelect(TreeNode selectedTreeNode)
        {
            bool shouldShow = selectedTreeNode.IsRootEventsNode();

            if (shouldShow)
            {

                
                EventsViewModel evm = new EventsViewModel();
                evm.BackingElement = GlueState.Self.CurrentElement;


                mEventItemList.DataContext = evm;



                if (!mContainer.Controls.Contains(mTab))
                {
                    mContainer.Controls.Add(mTab);

                    mContainer.SelectTab(mContainer.Controls.Count - 1);
                }

            }
            else if(mContainer.Controls.Contains(mTab))
            {
                mContainer.Controls.Remove(mTab);
            }
        }

        private void HandleTabInitialize(System.Windows.Forms.TabControl tabControl)
        {
            mContainer = tabControl;
            mTab = new PluginTab();
            mWpfHost = new System.Windows.Forms.Integration.ElementHost();
            mWpfHost.Dock = System.Windows.Forms.DockStyle.Fill;
            mWpfHost.Name = "EventsWpfHost";

            mEventItemList = new EventItemList();


            mWpfHost.Child = mEventItemList;

            mTab.Controls.Add(mWpfHost);
        }
    }
}
