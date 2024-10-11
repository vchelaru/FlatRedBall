using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Text;
using TMXGlueLib;

namespace TiledPlugin.Errors
{
    internal class InfiniteMapNotSupportedViewModel : ErrorViewModel
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

        private void UpdateDetails()
        {
            Details = $"Map {FilePath} is an infinite map. This is not currently supported.";
        }

        public override string UniqueId => Details;

        public override void HandleDoubleClick()
        {
            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(filePath);
            GlueState.Self.CurrentReferencedFileSave = rfs;
        }

        public override bool ReactsToFileChange(FilePath filePath)
        {
            return filePath == FilePath;
        }


        public override bool GetIfIsFixed()
        {
            // fixed if:
            // 1. File has been removed from the Glue project
            var rfs = GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(FilePath);
            if (rfs == null)
            {
                return true;
            }

            // 2. TMX doesn't exist anymore
            if (FilePath.Exists() == false)
            {
                return true;
            }

            // 3. Map is no longer infinite
            var tms = TiledMapSave.FromFile(FilePath.FullPath);
            
            if (tms.Infinite != 1)
            {
                return true;
            }


            return false;

        }
    }
}
