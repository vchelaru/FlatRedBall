using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using GlueBuilder.Managers;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;

namespace GlueBuilder.Plugins.ExportedImplementations
{
    internal class GlueCommands : IGlueCommands
    {
        FileCommands fileCommands = new FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces.FileCommands();

        public IGenerateCodeCommands GenerateCodeCommands => throw new NotImplementedException();

        public IGluxCommands GluxCommands => throw new NotImplementedException();

        public IOpenCommands OpenCommands => throw new NotImplementedException();

        public IProjectCommands ProjectCommands => throw new NotImplementedException();

        public IRefreshCommands RefreshCommands => throw new NotImplementedException();

        public ITreeNodeCommands TreeNodeCommands => throw new NotImplementedException();

        public IUpdateCommands UpdateCommands => throw new NotImplementedException();

        public IDialogCommands DialogCommands => throw new NotImplementedException();

        public IFileCommands FileCommands => fileCommands;

        public void CloseGlue()
        {
            throw new NotImplementedException();
        }

        public string GetAbsoluteFileName(ReferencedFileSave rfs)
        {
            throw new NotImplementedException();
        }

        public string GetAbsoluteFileName(string relativeFileName, bool isContent)
        {
            throw new NotImplementedException();
        }

        public void LoadProject(string fileName)
        {
            var glueProjectSave = FileManager.XmlDeserialize<GlueProjectSave>(fileName);

            ProjectManager.Self.CurrentGlueProjectSave = glueProjectSave;

            // also need to load the csproj:
            var projectFileName = FileManager.RemoveExtension(fileName) + ".csproj";

            ProjectManager.Self.MainProject = ProjectCreator.CreateProject(projectFileName);
            ProjectManager.Self.MainProject.Load(projectFileName);
        }

        public void PrintError(string output)
        {
            System.Console.Error.WriteLine(output);
        }

        public void PrintOutput(string output)
        {
            System.Console.WriteLine(output);
        }

        public void TryMultipleTimes(Action action, int numberOfTimesToTry)
        {
            throw new NotImplementedException();
        }
    }
}
