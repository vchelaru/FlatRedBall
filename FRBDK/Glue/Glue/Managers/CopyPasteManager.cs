using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            else if(currentTreeNodeTag is ScreenSave screen)
            {
                copiedObjectClone = screen.Clone();
            }
            else if(currentTreeNodeTag is EntitySave entity)
            {
                copiedObjectClone = entity.Clone();
            }
        }
        internal async Task HandlePaste()
        {
            if(copiedObjectClone is ReferencedFileSave asRfs)
            {
                await GlueCommands.Self.GluxCommands.DuplicateAsync(asRfs, GlueState.Self.CurrentElement);
            }
            else if(copiedObjectClone is NamedObjectSave asNos)
            {
                var response = await GlueCommands.Self.GluxCommands.CopyNamedObjectIntoElement(asNos, GlueState.Self.CurrentElement);
                if(response.Succeeded == false)
                {
                    GlueCommands.Self.PrintError(response.Message);
                }
            }
            else if(copiedObjectClone is GlueElement element)
            {
                await GlueCommands.Self.GluxCommands.CopyGlueElement(element);
            }
        }
    }
}
