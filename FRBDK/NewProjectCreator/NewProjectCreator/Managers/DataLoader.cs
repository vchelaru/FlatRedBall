using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NewProjectCreator.Managers
{
    class DataLoader
    {

        static List<PlatformProjectInfo> emptyProjects = new List<PlatformProjectInfo>();
        public static List<PlatformProjectInfo> EmptyProjects
        {
            get { return emptyProjects; }
        }

        static List<PlatformProjectInfo> starterProjects = new List<PlatformProjectInfo>();
        public static List<PlatformProjectInfo> StarterProjects
        {
            get { return starterProjects; }
        }


        public static void LoadAvailableProjectsFromCsv()
        {
            string fileName = "Content/EmptyTemplates.csv";

            string absoluteFile = FileManager.RelativeDirectory + fileName;

            if (!File.Exists(absoluteFile))
            {
                MessageBox.Show("The New Project Creator is missing a critical file called EmptyTemplates.csv.  Full path is\n\n" +
                    absoluteFile + "\n\nThis file is needed to get the list of available projects that the New Project Creator can create");
            }
            else
            {
                var type = typeof(PlatformProjectInfo);
                CsvFileManager.CsvDeserializeList(type, fileName, emptyProjects);
            }

            fileName = "Content/StarterProjects.csv";

            absoluteFile = FileManager.RelativeDirectory + fileName;

            if (!File.Exists(absoluteFile))
            {
                MessageBox.Show("The New Project Creator is missing a critical file called StarterProjects.csv.  Full path is\n\n" +
                    absoluteFile + "\n\nThis file is needed to get the list of available projects that the New Project Creator can create");
            }
            else
            {
                CsvFileManager.CsvDeserializeList(typeof(PlatformProjectInfo), fileName, starterProjects);
            }






        }
    }
}
