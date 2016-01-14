using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Plugins
{
    class GlueState : IGlueState
    {
        #region IGlueState Members

        public IElement CurrentElement
        {
            get { return EditorLogic.CurrentElement; }
        }

        #endregion
    }
}
