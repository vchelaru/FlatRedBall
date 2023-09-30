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
        object copiedObjectOwner;
        object copiedObjectClone;
        internal void HandleCopy()
        {
            var currentTreeNodeTag = GlueState.Self.CurrentTreeNode?.Tag;
            var currentElement = GlueState.Self.CurrentElement;

            if(currentTreeNodeTag is ReferencedFileSave rfs)
            {
                copiedObjectOwner = currentElement;
                copiedObjectClone = rfs.Clone();
            }
            else if(currentTreeNodeTag is NamedObjectSave nos)
            {
                copiedObjectOwner = currentElement;
                copiedObjectClone = nos.Clone();
            }
            else if(currentTreeNodeTag is StateSave stateSave)
            {
                copiedObjectOwner = GlueState.Self.CurrentStateSaveCategory;
                copiedObjectClone = stateSave.Clone();
            }
            else if(currentTreeNodeTag is ScreenSave screen)
            {
                copiedObjectClone = screen.CloneJson();
            }
            else if(currentTreeNodeTag is EntitySave entity)
            {
                copiedObjectClone = entity.CloneJson();
            }
            else if (currentTreeNodeTag is CustomVariable variable)
            {
                copiedObjectOwner = currentElement;
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
            else if(copiedObjectClone is StateSave asStateSave)
            {
                await GlueCommands.Self.GluxCommands.CopyStateSaveIntoElement(asStateSave, copiedObjectOwner as StateSaveCategory, GlueState.Self.CurrentElement);
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
