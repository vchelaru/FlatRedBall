using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms.Plugins.FrameShapePlugin
{
    class MainFrameShapePlugin
    {
        public void StartUp()
        {
            //AddMenuItemTo("Add Rectangle", "Add", HandleAddRectangle());
        }

        private string HandleAddRectangle()
        {
            throw new NotImplementedException();
        }

        // move this to base:
        //void AddMenuItemTo(string )

        void AddMenuItemTo(string whatToAdd, Action eventHandler, string container, int preferredIndex)
        {
            //ToolStripMenuItem menuItem = new ToolStripMenuItem(whatToAdd, null, (not, used) => eventHandler());
            //ToolStripMenuItem itemToAddTo = GetItem(container);
            //toolStripItemsAndParents.Add(menuItem, itemToAddTo);


            //if (preferredIndex == -1)
            //{
            //    itemToAddTo.DropDownItems.Add(menuItem);
            //}
            //else
            //{
            //    int indexToInsertAt = System.Math.Min(preferredIndex, itemToAddTo.DropDownItems.Count);

            //    itemToAddTo.DropDownItems.Insert(indexToInsertAt, menuItem);
            //}

            //return menuItem;
        }

    }
}
