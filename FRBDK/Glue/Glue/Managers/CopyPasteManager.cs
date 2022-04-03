using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Managers
{
    public class CopyPasteManager : Singleton<CopyPasteManager>
    {
        object copiedObjectClone;
        internal void HandleCopy()
        {
            var currentTreeNodeTag = GlueState.Self.CurrentTreeNode?.Tag;

            if(currentTreeNodeTag is ReferencedFileSave rfs)
            {
                copiedObjectClone = rfs.Clone();
            }
            else if(currentTreeNodeTag is NamedObjectSave nos)
            {
                copiedObjectClone = nos.Clone();
            }
        }
        internal void HandlePaste()
        {
            if(copiedObjectClone is ReferencedFileSave asRfs)
            {
                GlueCommands.Self.GluxCommands.DuplicateAsync(asRfs, GlueState.Self.CurrentElement);
            }
            else if(copiedObjectClone is NamedObjectSave asNos)
            {
                GlueCommands.Self.GluxCommands.CopyNamedObjectIntoElement(asNos, GlueState.Self.CurrentElement);
            }
        }
    }
}
