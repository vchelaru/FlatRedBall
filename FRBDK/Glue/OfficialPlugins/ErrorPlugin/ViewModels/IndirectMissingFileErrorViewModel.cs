using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ErrorPlugin.ViewModels
{
    public class IndirectMissingFileErrorViewModel : ErrorViewModel
    {
        public ReferencedFileSave ReferencedFileSave { get; private set; }
        public FilePath AbsoluteMissingFile { get; private set; }
        public List<FilePath> ReferenceStack { get; private set; }

        public IndirectMissingFileErrorViewModel(ReferencedFileSave referencedFileSave, List<FilePath> referenceStack, FilePath absoluteMissingFile)
        {
            ReferencedFileSave = referencedFileSave;

            AbsoluteMissingFile = absoluteMissingFile;
            ReferenceStack = referenceStack;

            Details = $"Missing file: {AbsoluteMissingFile}";

            // reverse the stack to show the top of the stack first:
            for(int i = referenceStack.Count - 1; i > -1; i--)
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
            return ReferenceStack.Contains(filePath) || AbsoluteMissingFile == filePath;
        }

        public override bool GetIfIsFixed()
        {
            // This is fixed if:
            //  1: AbsoluteMissingFile is now on disk
            //  2: Any file in the stack no longer exists
            //  3: Any file in the stack no longer references the next file in the stack
            //  4: The ReferencedFileSave has been removed from the project

            bool isFixed = false;

            if(AbsoluteMissingFile.Exists())
            {
                isFixed = true;
            }

            if(!isFixed)
            {
                foreach(var file in ReferenceStack)
                {
                    // 2:
                    if(file.Exists() == false)
                    {
                        isFixed = true;
                        break;
                    }
                }
            }

            if(!isFixed)
            {
                // 3:
                for(int i = 0; i < ReferenceStack.Count; i++)
                {
                    var current = ReferenceStack[i];
                    FilePath next;
                    if(i + 1 < ReferenceStack.Count)
                    {
                        next = ReferenceStack[i + 1];
                    }
                    else
                    {
                        next = AbsoluteMissingFile;
                    }

                    var referencedFiles =
                        GlueCommands.Self.FileCommands.GetFilePathsReferencedBy(current.Standardized, EditorObjects.Parsing.TopLevelOrRecursive.TopLevel);

                    var stillReferences = referencedFiles.Any(item => item == next);

                    if(!stillReferences)
                    {
                        isFixed = true;
                        break;
                    }

                }
            }

            if(!isFixed)
            {
                bool isReferencedByElement = ReferencedFileSave.GetContainer() != null;
                bool isGlobalContent = false;

                if(!isReferencedByElement)
                {
                    isGlobalContent = GlueState.Self.CurrentGlueProject.GetAllReferencedFiles().Contains(ReferencedFileSave);
                }

                if(!isReferencedByElement && !isGlobalContent)
                {
                    // the root RFS has been removed
                    isFixed = true;
                }
            }

            return isFixed;

        }


        public bool Matches(ErrorViewModel other)
        {
            if (other is IndirectMissingFileErrorViewModel)
            {
                var otherAsIndirect = other as IndirectMissingFileErrorViewModel;

                if(otherAsIndirect.AbsoluteMissingFile == this.AbsoluteMissingFile && otherAsIndirect.ReferenceStack.Count == this.ReferenceStack.Count)
                {
                    var matches = true;
                    for(int i = 0; i < this.ReferenceStack.Count; i++)
                    {
                        if(this.ReferenceStack[i] != otherAsIndirect.ReferenceStack[i])
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
