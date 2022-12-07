using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Errors
{
    public abstract class FileErrorViewModel : ErrorViewModel
    {
        FilePath filePath;
        public FilePath FilePath
        {
            get => filePath;
            set
            {
                filePath = value;
                UpdateDetails();
            }
        }

        public abstract void UpdateDetails();

        public override string UniqueId => Details;

        public override void HandleDoubleClick()
        {
            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(filePath);
            GlueState.Self.CurrentReferencedFileSave = rfs;
        }

        public override bool ReactsToFileChange(FilePath filePath) =>
            filePath == FilePath;

        public override bool GetIfIsFixed()
        {
            // fixed if:
            // 1. File has been removed from the Glue project
            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(FilePath);
            if (rfs == null)
            {
                return true;
            }

            // 2. File doesn't exist anymore
            if (FilePath.Exists() == false)
            {
                return true;
            }

            return false;
        }
    }
}