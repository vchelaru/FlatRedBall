using System.Windows;
using System.Windows.Controls;

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
