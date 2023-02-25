using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
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
            else if (currentTreeNodeTag is CustomVariable variable)
            {
                copiedObjectClone = variable.Clone();
            }
            else if(GlueState.Self.CurrentTreeNode.IsFolderForGlobalContentFiles())
            {
                copiedObjectClone = GlueState.Self.CurrentTreeNode.GetRelativeFilePath();
            }
        }
        internal async Task HandlePaste()
        {
            if(copiedObjectClone is ReferencedFileSave asRfs)
            {
                var currentTreeNode = GlueState.Self.CurrentTreeNode;
                FilePath desiredFolder = null;
                if(currentTreeNode.IsFolderInFilesContainerNode() || currentTreeNode.IsFolderForGlobalContentFiles())
                {
                    desiredFolder = GlueState.Self.ContentDirectoryPath + currentTreeNode.GetRelativeFilePath();
                }
                await GlueCommands.Self.GluxCommands.DuplicateAsync(asRfs, GlueState.Self.CurrentElement, desiredFolder);
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
            else if (copiedObjectClone is CustomVariable variable)
            {
                await GlueCommands.Self.GluxCommands.CopyCustomVariableToGlueElement(variable, GlueState.Self.CurrentElement);
            }
            else if(copiedObjectClone is string sourceFolderRelative)
            {
                var sourceFolderAbsolute = GlueState.Self.ContentDirectoryPath + sourceFolderRelative;
                var currentTreeNode = GlueState.Self.CurrentTreeNode;
                FilePath destinationFolder = null;
                if (currentTreeNode.IsFolderInFilesContainerNode() || currentTreeNode.IsFolderForGlobalContentFiles())
                {
                    destinationFolder = GlueState.Self.ContentDirectoryPath + currentTreeNode.GetRelativeFilePath();
                }
                if(destinationFolder != null)
                {
                    await GlueCommands.Self.FileCommands.PasteFolder(sourceFolderAbsolute, destinationFolder);
                }
            }
        }
    }
}
