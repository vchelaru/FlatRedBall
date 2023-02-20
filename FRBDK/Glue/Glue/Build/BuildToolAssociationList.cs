using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EditorObjects.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;

namespace EditorObjects.SaveClasses
{
    public class BuildToolAssociationList
    {
        public List<BuildToolAssociation> BuildToolList
        {
            get;
            set;
        }

        public BuildToolAssociationList()
        {
            BuildToolList = new List<BuildToolAssociation>();
        }

        public static BuildToolAssociationList FromFileXml(string fileName)
        {
            BuildToolAssociationList toReturn = null;

            toReturn = FileManager.XmlDeserialize<BuildToolAssociationList>(fileName);

            return toReturn;



        }


        public static BuildToolAssociationList FromFileCsv(string fileName)
        {

            BuildToolAssociationList toReturn = new BuildToolAssociationList();

            CsvFileManager.CsvDeserializeList(typeof(BuildToolAssociation), fileName, toReturn.BuildToolList);

            return toReturn;


        }

        public void SaveCsv(string fileName)
        {

            RuntimeCsvRepresentation rcr = RuntimeCsvRepresentation.FromList<BuildToolAssociation>(this.BuildToolList);
            CsvFileManager.Serialize(rcr, fileName);

        }

        public void ValidateBuildTools(string projectDirectory)
        {
            string buildToolErrors = string.Empty;

            var allUsedExtensions = ObjectFinder.Self.GetAllReferencedFiles().Select(item => FileManager.GetExtension(item.SourceFile))
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToHashSet();

            foreach (BuildToolAssociation bta in BuildToolList)
            {
                // if the build tool is null that's okay, we probably 
                // just need to skip it because the user has removed the
                // tool.
                if (!string.IsNullOrEmpty(bta.BuildToolProcessed) &&  
                    // Build tools may be added by plugins in anticipation of 
                    // a file type being built. If a user doesn't use that file
                    // type, then there's no need to show a warning for it:
                        allUsedExtensions.Contains(bta.SourceFileType))
                {
                    string fileToLookFor = bta.BuildToolProcessed;

                    string absoluteFileName = bta.BuildToolProcessed;
                    if (FileManager.IsRelative(absoluteFileName))
                    {
                        absoluteFileName = projectDirectory + absoluteFileName;
                    }

                    if (FileManager.FileExists(absoluteFileName) == false)
                    {
                        if (buildToolErrors.Contains(bta.BuildToolProcessed) == false)
                            buildToolErrors += bta.BuildToolProcessed + "\n";
                    }
                }

            }

            if (string.IsNullOrEmpty(buildToolErrors) == false)
            {
                buildToolErrors = "The following build tools could not be located:\n" + buildToolErrors + "\nYou will not be able to build the associated assets until this issue is resolved.";
                System.Windows.Forms.MessageBox.Show(buildToolErrors);
            
            }
        }



    }
}
