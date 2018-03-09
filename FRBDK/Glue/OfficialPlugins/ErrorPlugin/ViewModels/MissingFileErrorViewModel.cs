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
    public class MissingFileErrorViewModel : ErrorViewModel
    {
        public ReferencedFileSave ReferencedFileSave { get; private set; }
        public FilePath AbsoluteMissingFile { get; private set; }
        
        public MissingFileErrorViewModel(ReferencedFileSave referencedFileSave)
        {
            ReferencedFileSave = referencedFileSave;

            AbsoluteMissingFile = GlueCommands.Self.GetAbsoluteFileName(referencedFileSave);

            Details = $"Missing file: {AbsoluteMissingFile} needed by {referencedFileSave}";
            
        }

        public override void HandleDoubleClick()
        {
            GlueCommands.Self.SelectCommands.Select(ReferencedFileSave);
        }

        public override bool ReactsToFileChange(FilePath filePath)
        {
            return filePath == AbsoluteMissingFile;
        }


        public override bool GetIfIsFixed()
        {
            var isFixed = false;
            if(AbsoluteMissingFile.Exists())
            {
                isFixed = true;
            }

            if(!isFixed)
            {
                bool isReferencedByElement = ReferencedFileSave.GetContainer() != null;
                bool isGlobalContent = false;

                if (!isReferencedByElement)
                {
                    isGlobalContent = GlueState.Self.CurrentGlueProject.GetAllReferencedFiles().Contains(ReferencedFileSave);
                }

                if (!isReferencedByElement && !isGlobalContent)
                {
                    // the RFS has been removed
                    isFixed = true;
                }
            }

            return isFixed;
        }
    }
}
