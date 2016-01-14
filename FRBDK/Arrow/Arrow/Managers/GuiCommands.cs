using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using FlatRedBall.Arrow.DataTypes;
using FlatRedBall.Instructions.Reflection;

namespace FlatRedBall.Arrow.Managers
{
    public class GuiCommands
    {
        ItemsControl mAllElementsTreeView;
        TreeView mSingleElementTreeView;


        internal void Initialize(ItemsControl treeView, TreeView singleElementTreeView)
        {
            mAllElementsTreeView = treeView;
            mSingleElementTreeView = singleElementTreeView;
        }
        
        
        public void RefreshAll()
        {

        }

    }
}
