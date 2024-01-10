using FlatRedBall.AnimationEditorForms.CommandsAndState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms.Plugins.FrameShapePlugin
{
    class MainFrameShapePlugin : PluginBase
    {

        public override void StartUp()
        {
            AddMenuItemTo("Add Rectangle", "Add", HandleAddRectangle);
            AddMenuItemTo("Add Circle", "Add", HandleAddCircle);
        }

        private void HandleAddRectangle()
        {
            if(AppState.Self.CurrentFrame != null)
            {
                AppCommands.Self.AddAxisAlignedRectangle(AppState.Self.CurrentFrame);
            }
        }


        private void HandleAddCircle()
        {
            if (AppState.Self.CurrentFrame != null)
            {
                AppCommands.Self.AddCircle(AppState.Self.CurrentFrame);
            }
        }
    }
}
