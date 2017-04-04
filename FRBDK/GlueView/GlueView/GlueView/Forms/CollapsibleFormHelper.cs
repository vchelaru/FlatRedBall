using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using StaffDotNet.CollapsiblePanel;
using FlatRedBall.Winforms;
using FlatRedBall.Winforms.Container;
using FlatRedBall.Glue.Plugins.Interfaces;
using GlueView.Managers;

namespace GlueView.Forms
{
    public class CollapsibleFormHelper : Singleton<CollapsibleFormHelper>
    {
        static CollapsibleFormHelper mSelf;
        CollapsibleContainerStrip mForm;

        

        public void Initialize(CollapsibleContainerStrip form)
        {
            mForm = form;


        }


        public Control AddCollapsableForm(string panelTitle, int expandedHeight, Control controlToAdd, IPlugin owner)
        {

            FlatRedBall.Winforms.CollapsibleControl cp = mForm.AddCollapsibleControlFor(controlToAdd, expandedHeight, panelTitle);
            if (owner != null)
            {
                Plugin.PluginManager.GlobalInstance.RegisterControlForPlugin(cp, owner);
            }
            return cp;
        }

        public Control AddCollapsableForm(string panelTitle, int expandedHeight, System.Windows.Controls.UserControl controlToAdd, IPlugin owner)
        {
            FlatRedBall.Winforms.CollapsibleControl cp = mForm.AddCollapsibleControlFor(controlToAdd, expandedHeight, panelTitle);
            if (owner != null)
            {
                Plugin.PluginManager.GlobalInstance.RegisterControlForPlugin(cp, owner);
            }
            return cp;
        }

        public Control GetControlByLabel(string label)
        {
            return mForm.GetControlByLabel(label);
        }

        List<Control> panelsResized = new List<Control>();
        void ResizeCollapsablePanel(object sender, EventArgs args)
        {
            int m = 3;

            Control senderAsControl = sender as Control;
            CollapsiblePanel parent = senderAsControl.Parent as CollapsiblePanel;
                int panelHeight = Math.Min(parent.ExpandedHeight, parent.Height);

            
            // Hack!  If the panel height is 20, it's being collapsed
            // Hack part 2.  I don't really know why but if I add 20 the first
            // time this is called then the collpsible panel is sized properly.
            // But after that all is sized okay if I don't add.
                
            if (parent.PanelState == PanelStateOptions.Expanded && panelHeight != 20)
            {

                int senderHeight = senderAsControl.Height;


                int bottom = 
                    senderHeight;

                if (!panelsResized.Contains(parent))
                {
                    bottom += 20;
                    panelsResized.Add(parent);
                }


                if (bottom > panelHeight)
                {
                    parent.ExpandedHeight = bottom;
                    parent.Height = parent.ExpandedHeight;
                }
                if (bottom < panelHeight)
                {
                    parent.ExpandedHeight = bottom;
                    parent.Height = parent.ExpandedHeight;
                }
            }
        }

        internal void SetCollapsedItems(List<string> collapsedPlugins)
        {
            foreach (var item in Plugin.PluginManager.GlobalInstance.PluginContainers)
            {
                var pluginControls = item.Value.Controls;

                foreach (var control in pluginControls)
                {
                    if (control is CollapsibleControl)
                    {
                        var pluginName = item.Key.GetType().FullName;
                        ((CollapsibleControl)control).IsCollapsed = collapsedPlugins.Contains(pluginName);
                    }
                }
            }
        }

        private CollapsiblePanel GetBottomPanel()
        {
            int maxY = int.MinValue;
            CollapsiblePanel bottomPanel = null;

            foreach (var control in mForm.Controls)
            {
                if (control is CollapsiblePanel)
                {
                    CollapsiblePanel cp = control as CollapsiblePanel;

                    if (cp.Location.Y > maxY)
                    {
                        maxY = cp.Location.Y;
                        bottomPanel = cp;
                    }

                }

            }

            return bottomPanel;
        }

        public List<string> GetCollapsedItems()
        {
            List<string> collapsedItems = new List<string>();

            foreach(var item in Plugin.PluginManager.GlobalInstance.PluginContainers)
            {
                var pluginControls = item.Value.Controls;

                foreach(var control in pluginControls)
                {
                    if(control is CollapsibleControl && ((CollapsibleControl)control).IsCollapsed)
                    {
                        collapsedItems.Add(item.Key.GetType().FullName);
                    }
                }
            }

            return collapsedItems;
        }
    }
}
