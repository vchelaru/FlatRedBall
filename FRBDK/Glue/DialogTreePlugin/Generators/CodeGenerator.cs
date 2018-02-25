using FlatRedBall.Glue.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.IO;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue;
using DialogTreePlugin.Controllers;

namespace DialogTreePlugin.Generators
{
    public class CodeGenerator : Singleton<CodeGenerator>
    {
        List<string> dialogTreeFileNames;

        public CodeGenerator()
        {
            dialogTreeFileNames = new List<string>();
        }

        internal void UpdateTrackedDialogTreeFiles(IEnumerable<string> dialogTrees)
        {
            var newfileNames = dialogTrees.Where(item => dialogTreeFileNames.Contains(item) == false).ToArray();
            dialogTreeFileNames.AddRange(newfileNames);

            if(newfileNames.Length > 0)
            {
                RegenCode();
            }
        }

        internal void AddNewTrackedFile(string name, bool isGlueLoad = false)
        {
            bool needToRegenCode = false;
            if (dialogTreeFileNames.Contains(name) == false)
            {
                dialogTreeFileNames.Add(name);
                needToRegenCode = true;
            }

            if(needToRegenCode && !isGlueLoad)
            {
                RegenCode();
            }
        }

        internal void RegenCode()
        {
            const string fieldType = "public const string";
            var nameSpaceBlock = new CodeBlockNamespace(null, ProjectManager.ProjectBase.RootNamespace);
            var classBlock = nameSpaceBlock.Class("public partial", "DialogTree");
            var codeFileName = $"{FileManager.GetDirectory(GlueState.Self.CurrentGlueProjectFileName)}DialogTree.Generated.cs";

            foreach (var treeName in dialogTreeFileNames)
            {
                var fileName = FileManager.RemovePath(treeName);
                var fieldName = FileManager.RemoveExtension(fileName);

                classBlock.Line($"{fieldType} {fieldName} = \"{fieldName}\";");
            }

            TaskManager.Self.AddAsyncTask(
                () =>
                {
                    bool wasSaved = SaveDiaogTreeCsFile(nameSpaceBlock.ToString(), codeFileName);

                    bool wasAdded = ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(ProjectManager.ProjectBase, codeFileName);

                    if(wasAdded)
                    {
                        ProjectManager.SaveProjects();
                    }
                },
                "Adding geneated file to the DialogTree file"
                );
        }

        private bool SaveDiaogTreeCsFile(string fileContents, string fileName)
        {
            bool wasSaved = false;

            var directory = FileManager.GetDirectory(fileName);
            
            const int timesToTry = 4;
            int timesTried = 0;
            while (true)
            {
                try
                {
                    System.IO.File.WriteAllText(fileName, fileContents);
                    wasSaved = true;
                    break;
                }
                catch (Exception exception)
                {
                    timesTried++;

                    if (timesTried >= timesToTry)
                    {
                        FlatRedBall.Glue.Plugins.PluginManager.ReceiveError("Error trying to save generated file:\n" +
                            exception.ToString());
                        break;
                    }
                }
            }

            return wasSaved;
        }
    }
}
