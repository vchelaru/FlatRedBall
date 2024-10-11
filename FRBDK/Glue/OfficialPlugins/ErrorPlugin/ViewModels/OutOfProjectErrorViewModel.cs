using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficialPlugins.ErrorPlugin.ViewModels
{
    public class OutOfProjectErrorViewModel : ErrorViewModel
    {
        public ReferencedFileSave ReferencedFileSave { get; private set; }
        public FilePath AbsoluteOutOfProjectFile { get; private set; }
        public List<FilePath> ReferenceStack { get; private set; }

        public override string UniqueId => Details;
        public OutOfProjectErrorViewModel(ReferencedFileSave referencedFileSave, List<FilePath> referenceStack, FilePath absoluteOutOfProjectFile)
        {
            ReferencedFileSave = referencedFileSave;
            AbsoluteOutOfProjectFile = absoluteOutOfProjectFile;
            ReferenceStack = referenceStack;

            Details = $"Referenced file is out of project: {AbsoluteOutOfProjectFile}";

            // reverse the stack to show the top of the stack first:
            for (int i = referenceStack.Count - 1; i > -1; i--)
            {
                Details += "\n\tby " + referenceStack[i];

            }
        }

        public override void HandleDoubleClick()
        {
            GlueCommands.Self.SelectCommands.Select(ReferencedFileSave);
        }

        public override bool ReactsToFileChange(FilePath filePath)
        {
            return ReferenceStack.Contains(filePath) || AbsoluteOutOfProjectFile == filePath;
        }

        public override bool GetIfIsFixed()
        {
            // This is fixed if:
            //  1: AbsoluteOutOfProjectFile is now in project
            //  2: Any file in the stack no longer exists
            //  3: Any file in the stack no longer references the next file in the stack
            //  4: The ReferencedFileSave has been removed from the project

            bool isFixed = false;

            if (GlueState.Self.CurrentGlueProject == null || 
                FileManager.IsRelativeTo(AbsoluteOutOfProjectFile.FullPath, GlueState.Self.CurrentGlueProjectDirectory) )
            {
                isFixed = true;
            }

            if (!isFixed)
            {
                foreach (var file in ReferenceStack)
                {
                    // 2:
                    if (file.Exists() == false)
                    {
                        isFixed = true;
                        break;
                    }
                }
            }

            if (!isFixed)
            {
                // 3:
                for (int i = 0; i < ReferenceStack.Count; i++)
                {
                    var current = ReferenceStack[i];
                    FilePath next;
                    if (i + 1 < ReferenceStack.Count)
                    {
                        next = ReferenceStack[i + 1];
                    }
                    else
                    {
                        next = AbsoluteOutOfProjectFile;
                    }

                    var referencedFiles =
                        GlueCommands.Self.FileCommands.GetFilePathsReferencedBy(current, EditorObjects.Parsing.TopLevelOrRecursive.TopLevel);

                    var stillReferences = referencedFiles.Any(item => item == next);

                    if (!stillReferences)
                    {
                        isFixed = true;
                        break;
                    }

                }
            }

            if (!isFixed)
            {
                bool isReferencedByElement = ReferencedFileSave.GetContainer() != null;
                bool isGlobalContent = false;

                if (!isReferencedByElement)
                {
                    isGlobalContent = GlueState.Self.CurrentGlueProject.GetAllReferencedFiles().Contains(ReferencedFileSave);
                }

                if (!isReferencedByElement && !isGlobalContent)
                {
                    // the root RFS has been removed
                    isFixed = true;
                }
            }

            return isFixed;

        }

        public bool Matches(ErrorViewModel other)
        {
            if (other is OutOfProjectErrorViewModel)
            {
                var otherAsIndirect = other as OutOfProjectErrorViewModel;

                if (otherAsIndirect.AbsoluteOutOfProjectFile == this.AbsoluteOutOfProjectFile && otherAsIndirect.ReferenceStack.Count == this.ReferenceStack.Count)
                {
                    var matches = true;
                    for (int i = 0; i < this.ReferenceStack.Count; i++)
                    {
                        if (this.ReferenceStack[i] != otherAsIndirect.ReferenceStack[i])
                        {
                            matches = false;
                            break;
                        }
                    }
                    return matches;
                }
            }
            return false;
        }
    }
}
