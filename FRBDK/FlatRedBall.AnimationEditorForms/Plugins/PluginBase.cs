using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms.Plugins
{
    public abstract class PluginBase
    {
        Dictionary<ToolStripMenuItem, ToolStripMenuItem> toolStripItemsAndParents = new Dictionary<ToolStripMenuItem, ToolStripMenuItem>();

        public abstract void StartUp();


        protected ToolStripMenuItem AddMenuItemTo(string whatToAdd, string container, Action eventHandler, int preferredIndex = -1)
        {
            ToolStripMenuItem menuItem = new ToolStripMenuItem(whatToAdd, null, (not, used) => eventHandler());
            ToolStripMenuItem itemToAddTo = GetItem(container);
            toolStripItemsAndParents.Add(menuItem, itemToAddTo);


            if (preferredIndex == -1)
            {
                itemToAddTo.DropDownItems.Add(menuItem);
            }
            else
            {
                int indexToInsertAt = System.Math.Min(preferredIndex, itemToAddTo.DropDownItems.Count);

                itemToAddTo.DropDownItems.Insert(indexToInsertAt, menuItem);
            }

            return menuItem;
        }

        private ToolStripMenuItem GetItem(string container)
        {
            var menuStrip = MainControl.Self.MenuStrip.Items.FirstOrDefault(item => (item as ToolStripItem).Text == container);
            return menuStrip as ToolStripMenuItem;
        }
    }
}
