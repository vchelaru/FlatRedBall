using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.FormHelpers;

namespace FlatRedBall.Glue.Plugins
{
    class GlueCommands : IGlueCommands
    {
        #region IGlueCommands Members

        public void RefreshUiForSelectedElement()
        {
            EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();
        }

        public void SaveGlux()
        {
            ProjectManager.SaveGlux();
        }

        #endregion
    }
}
