using FlatRedBall.Gum.Converters;
using FlatRedBall.IO;
using Gum;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gum.Managers
{
    public class ExportManager : Singleton<ExportManager>
    {
        GumProjectToGlueProject mConverter = new GumProjectToGlueProject();

        public void PerformExport(object sender, EventArgs args)
        {
            // let's preserve file names
            bool preserveCase = FileManager.PreserveCase;
            FileManager.PreserveCase = true;

            string gluxFolderLocation = @"C:\FlatRedBallProjects\FRBDK\GumToGlueTestProject\GumToGlueTestProject\GumToGlueTestProject\";

            var result = mConverter.ToGlueProjectSave(ProjectManager.Self.GumProjectSave, gluxFolderLocation);


            string whereToSave = gluxFolderLocation + "GumToGlueTestProject.Gum.Generated.glux";


            FileManager.XmlSerialize(result, whereToSave);
            // save this now

            FileManager.PreserveCase = preserveCase;
        }
    }
}
