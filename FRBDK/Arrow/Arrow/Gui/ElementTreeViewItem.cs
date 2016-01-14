using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using FlatRedBall.Arrow.DataTypes;

namespace FlatRedBall.Arrow
{
    public class ElementTreeViewItem : TreeViewItem
    {
        public ArrowElementSave ArrowElementSave
        {
            get;
            set;
        }

        public void UpdateToArrowElementSave()
        {
            // Early Out
            if (ArrowElementSave == null)
            {
                return;
            }

            this.Header = ArrowElementSave.Name;
            

        }
    }
}
