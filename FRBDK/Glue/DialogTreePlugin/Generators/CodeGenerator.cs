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
            var didAddTag = false;
            foreach(var tag in tags)
            {
                if(dialogTreeTags.Contains(tag) == false)
                {
                    dialogTreeTags.Add(tag);
                    didAddTag = true;
                }
            }

            dialogTreeTags.Sort();
            if(didAddTag && isGlueLoad == false)
            {
                RegenCodeDialogTreeTags();
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

            //AutoGen the deserialization function.
            //We do not need a try catch since we know the deserialization will work.
            var deserializeFunction = classBlock.Function("public static Rootobject", "DeserializeDialogTree", "string fileName");
            deserializeFunction.Line("var fileStream = new System.IO.FileStream(fileName, System.IO.FileMode.Open);");
            deserializeFunction.Line("var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Rootobject));");
            deserializeFunction.Line("var toReturn = (Rootobject)serializer.ReadObject(fileStream);");
            deserializeFunction.Line("fileStream.Close();");
            deserializeFunction.Line("return toReturn;");

            var getFileFromNameFunction = classBlock.Function("public static Rootobject", "GetFileFromName", "string fileName");
            getFileFromNameFunction.Line("Rootobject toReturn = null;");
            var switchStatement = getFileFromNameFunction.Switch("fileName");

            var destroyFunction = classBlock.Function("public static void", "ClearTrees", string.Empty);

            foreach (var treeName in dialogTreeFileNames)
            {
                var fileName = FileManager.RemovePath(treeName);
                var fieldName = FileManager.RemoveExtension(fileName);
                var localPath = FileManager.MakeRelative(treeName);

                classBlock.Line($"private static Rootobject m{fieldName};");
                var property = classBlock.Property("public static Rootobject", $"{fieldName}");
                var get = property.Get();
                get.If($"m{fieldName} == null").Line($"m{fieldName} = DeserializeDialogTree(\"{localPath.ToLower()}\");");
                get.Line($"return m{fieldName};");

                destroyFunction.Line($"m{fieldName} = null;");

                //classBlock.Line($"{fieldType} {fieldName} = \"{localPath.ToLower()}\";");

                switchStatement.Line($"case \"{fieldName}\":");
                switchStatement.Line($"    toReturn = DialogTree.{fieldName};");
                switchStatement.Line($"    break;");
            }

            getFileFromNameFunction.Line("return toReturn;");

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
