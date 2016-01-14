using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EditorObjects.SaveClasses;
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
            foreach (BuildToolAssociation bta in BuildToolList)
            {
                // if the build tool is null that's okay, we probably 
                // just need to skip it because the user has removed the
                // tool.
                if (!string.IsNullOrEmpty(bta.BuildTool))
                {
                    string fileToLookFor = bta.BuildTool;

                    string absoluteFileName = bta.BuildTool;
                    if (FileManager.IsRelative(absoluteFileName))
                    {
                        absoluteFileName = projectDirectory + absoluteFileName;
                    }

                    if (FileManager.FileExists(absoluteFileName) == false)
                    {
                        if (buildToolErrors.Contains(bta.BuildTool) == false)
                            buildToolErrors += bta.BuildTool + "\n";
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
