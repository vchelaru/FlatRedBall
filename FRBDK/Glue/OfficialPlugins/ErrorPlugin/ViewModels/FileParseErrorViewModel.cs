using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeneralResponse = ToolsUtilities.GeneralResponse;

namespace OfficialPlugins.ErrorPlugin.ViewModels
{
    public class FileParseErrorViewModel : ErrorViewModel
    {
        public FilePath FilePath { get; private set; }
        public GeneralResponse GeneralResponse { get; private set; }

        public override string UniqueId => Details;

        public FileParseErrorViewModel(FilePath filePath, GeneralResponse generalResponse)
        {
            FilePath = filePath;
            GeneralResponse = generalResponse;
            Details = $"Error parsing file: {FilePath}\n{GeneralResponse.Message}";

        }

        public override void HandleDoubleClick()
        {
            // If this is a referenced file save, then go to it
            var rfs = GlueCommands.Self.FileCommands.GetReferencedFile(FilePath.Standardized);

            if(rfs != null)
            {
                GlueCommands.Self.SelectCommands.Select(rfs);
            }
            else
            {
                try
                {
                    string fileToOpen = null;
                    if(FilePath.Exists())
                    {
                        fileToOpen = FilePath.Standardized.Replace('/', '\\');
                    }
                    else
                    {
                        fileToOpen = FilePath.GetDirectoryContainingThis().Standardized.Replace('/', '\\');
                    }
                    System.Diagnostics.Process.Start(fileToOpen);
                }
                catch(Exception)
                {
                }
            }
            // else do anything? Open the file?
        }

        public override bool ReactsToFileChange(FilePath filePath)
        {
            return filePath == FilePath;
        }

        public override bool GetIfIsFixed()
        {

            var lastParseResponse = GlueCommands.Self.FileCommands.GetLastParseResponse(FilePath);

            if(lastParseResponse == null)
            {
                return true;
            }
            else if(FilePath.Exists() == false)
            {
                return true;
            }
            else
            {
                // If we're testing if it's fixed, then the file may have changed, so refresh the last parse message
                // by asking the file commands for references
                GlueCommands.Self.FileCommands.GetFilesReferencedBy(FilePath.Standardized, EditorObjects.Parsing.TopLevelOrRecursive.TopLevel);

                var doMessagesMatch = lastParseResponse.Message == GeneralResponse.Message;

                // if they don't match, then it's a different error, so this particular one is fixed:
                return doMessagesMatch == false;
            }
        }
    }
}
