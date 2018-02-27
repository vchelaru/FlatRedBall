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
        List<string> dialogTreeTags;

        public CodeGenerator()
        {
            dialogTreeFileNames = new List<string>();
            dialogTreeTags = new List<string>();
        }

        internal void AddNewTrackedFile(string name, bool isGlueLoad = false)
        {
            if (dialogTreeFileNames.Contains(name) == false)
            {
                dialogTreeFileNames.Add(name);
                dialogTreeFileNames.Sort();

                if(isGlueLoad == false)
                {
                    RegenCodeDialogTreeFileName();
                }
            }

        }

        internal void AddTrackedDialogTreeTag(string[] tags, bool isGlueLoad = false)
        {
            var newTags = tags.Where(item => dialogTreeTags.Contains(item) == false).ToArray();
            if(newTags.Length > 0)
            {
                dialogTreeTags.AddRange(tags);

                dialogTreeTags.Sort();
                if(isGlueLoad == false)
                {
                    RegenCodeDialogTreeTags();
                }
            }
        }

        internal void RegenCodeDialogTreeTags()
        {
            const string fieldType = "public const string";
            var nameSpaceBlock = new CodeBlockNamespace(null, ProjectManager.ProjectBase.RootNamespace);
            var classBlock = nameSpaceBlock.Class("public partial", "DialogTree");
            var internalClassBlock = classBlock.Class("public", "Tag");
            var codeFileName = $"{FileManager.GetDirectory(GlueState.Self.CurrentGlueProjectFileName)}DialogTree.Tags.Generated.cs";

            foreach (var tag in dialogTreeTags)
            {
                internalClassBlock.Line($"{fieldType} {tag} = \"{tag}\";");
            }

            TaskManager.Self.AddAsyncTask(
                () =>
                {
                    bool wasSaved = SaveDiaogTreeCsFile(nameSpaceBlock.ToString(), codeFileName);

                    bool wasAdded = ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(ProjectManager.ProjectBase, codeFileName);

                    if (wasAdded)
                    {
                        ProjectManager.SaveProjects();
                    }
                },
                "Adding geneated file to the DialogTree file"
                );
        }

        internal void RegenCodeDialogTreeFileName()
        {
            const string fieldType = "public const string";
            var nameSpaceBlock = new CodeBlockNamespace(null, ProjectManager.ProjectBase.RootNamespace);
            var classBlock = nameSpaceBlock.Class("public partial", "DialogTree");
            var codeFileName = $"{FileManager.GetDirectory(GlueState.Self.CurrentGlueProjectFileName)}DialogTree.Generated.cs";

            foreach (var treeName in dialogTreeFileNames)
            {
                var fileName = FileManager.RemovePath(treeName);
                var fieldName = FileManager.RemoveExtension(fileName);
                var localPath = FileManager.MakeRelative(treeName);

                classBlock.Line($"{fieldType} {fieldName} = \"{localPath.ToLower()}\";");
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
