using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfDataUi;

namespace GlueFormsCore.Controls
{
    public class StyledGroupBox : GroupBox
    {
        public StyledGroupBox()
        {
            this.Resources = MainPanelControl.ResourceDictionary;

            //this.Style = "GroupBoxStyle";

            var style = this.TryFindResource("GroupBoxStyle") as Style;
            if (style != null)
            {
                this.Style = style;
            }
        }
    }
}
