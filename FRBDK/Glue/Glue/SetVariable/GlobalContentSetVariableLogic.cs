using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Parsing;

namespace FlatRedBall.Glue.SetVariable
{
    class GlobalContentSetVariableLogic
    {
        internal void ReactToGlobalContentChangedValue(string changedMember, object oldValue, ref bool updateTreeView)
        {
            updateTreeView = false; // currently nothing here will update the UI, so we can save some time with this
            //if (changedMember == "LoadAsynchronously")
            //{
            //    ElementViewWindow.GenerateGlobalContentFileCode();
            //}

            ContentLoadWriter.UpdateLoadGlobalContentCode();
        }
    }
}
