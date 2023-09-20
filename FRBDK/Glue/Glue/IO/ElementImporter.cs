using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.IO.Zip;
using FlatRedBall.IO;
using System.IO;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Ionic.Zip;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Managers;
using System.Threading.Tasks;
using L = Localization;

namespace FlatRedBall.Glue.IO;

class ElementImporter
{
    public static void AskAndImportGroup()
    {
        OpenFileDialog dialog = new OpenFileDialog();
        dialog.Filter = "Glue Group (*.ggpz)|*.ggpz";

        var dialogResult = dialog.ShowDialog();

        if (dialogResult == DialogResult.OK)
        {
            string fileName = dialog.FileName;

            // First let's unzip the file:
            string unpackDirectory = FileManager.UserApplicationDataForThisApplication + "GroupUnzip\\";

            if (Directory.Exists(unpackDirectory))
            {
                FileManager.DeleteDirectory(unpackDirectory);
            }
            Directory.CreateDirectory(unpackDirectory);

            List<string> foundFiles = new List<string>();

            using (ZipFile zip1 = ZipFile.Read(fileName))
            {
                foreach (ZipEntry zipEntry in zip1)
                {
                    string directory = FileManager.GetDirectory(zipEntry.FileName, RelativeType.Relative);

                    zipEntry.Extract(unpackDirectory, ExtractExistingFileAction.OverwriteSilently);

                    foundFiles.Add(unpackDirectory + zipEntry.FileName);
                }
            }

            // Now that all files have been unzipped let's loop through them and
            // import them
            foreach (string zipFile in foundFiles)
            {

                string relativeToUnzipRoot = FileManager.MakeRelative(zipFile, unpackDirectory);

                if (relativeToUnzipRoot.StartsWith($"Entities/", StringComparison.OrdinalIgnoreCase))
                {
                    relativeToUnzipRoot = relativeToUnzipRoot[$"Entities/".Length..];
                }
                else if (relativeToUnzipRoot.StartsWith($"Screens/", StringComparison.OrdinalIgnoreCase))
                {
                    relativeToUnzipRoot = relativeToUnzipRoot[$"Screens/".Length..];
                }

                // remove the file name (like Entity.entz) to get the directory
                var directory = FileManager.GetDirectory(relativeToUnzipRoot, RelativeType.Relative);

                ImportElementFromFile(zipFile, false, directory);
            }
        }
    }

    public static void ShowImportElementUi(ITreeNode currentTreeNode)
    {

        // import screen, import entity, import element
        #region Open the File Dialog to ask the user which file to add
        OpenFileDialog openFileDialog = new OpenFileDialog();

        openFileDialog.Multiselect = false;

        if (currentTreeNode?.IsRootEntityNode() == true ||
            currentTreeNode?.IsFolderForEntities() == true)
        {
            openFileDialog.Filter = "Exported Entities (*.entz)|*.entz";
        }
        else
        {
            openFileDialog.Filter = "Exported Screens (*.scrz)|*.scrz";
        }


        #endregion

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            string fileName = openFileDialog.FileName;
            TaskManager.Self.Add(() => ImportElementFromFile(fileName, true), $"Import Entity {fileName}");
        }

    }

    public static async Task<GlueElement> ImportElementFromFile(string fileName, bool moveToSelectedFolderTreeNode, string subDirectory = null)
    {
        string unpackDirectory;

        List<string> filesToAddToContent;
        List<string> codeFilesInZip;

        Zipper.UnzipScreenOrEntityImport(fileName, out unpackDirectory, out filesToAddToContent, out codeFilesInZip);

        string elementName = FileManager.RemovePath(FileManager.RemoveExtension(fileName));
        if (!string.IsNullOrEmpty(subDirectory))
        {
            elementName = subDirectory + elementName;
            elementName = elementName.Replace("/", "\\");
        }

        #region Get XML extension

        string extension = "";
        if (FileManager.GetExtension(fileName) == "entz")
        {
            extension = "entx";
        }
        else
        {
            extension = "scrx";
        }

        #endregion

        string desiredNamespace = "";
        GlueElement newElement = null;
            
        if (extension == "entx")
        {
            var result = await ImportEntity(unpackDirectory, filesToAddToContent, codeFilesInZip, elementName, extension, moveToSelectedFolderTreeNode, desiredNamespace, newElement);
            desiredNamespace = result.desiredNamespace;
            newElement = result.newElement;
        }
        else
        {
            var result = await ImportScreen(unpackDirectory, filesToAddToContent, codeFilesInZip, elementName, extension, moveToSelectedFolderTreeNode, desiredNamespace, newElement);
            desiredNamespace = result.desiredNamespace;
            newElement = result.newElement;
        }

        ResolveElementReferences(newElement);

        #region Refresh and save everything

        GlueCommands.Self.DoOnUiThread(() =>
        {
            GlueState.Self.CurrentElement = newElement;

            GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
        });

        GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
        GlueCommands.Self.ProjectCommands.SaveProjects();
        GluxCommands.Self.SaveGlux();

        #endregion

        return newElement;
    }

    private static async Task<(string desiredNamespace, GlueElement newElement)> ImportScreen(string unpackDirectory, List<string> filesToAddToContent, 
        List<string> codeFiles, string elementName, string extension, bool moveToSelectedFolderTreeNode, string desiredNamespace, GlueElement newElement)
    {
        var result = await ImportElement<ScreenSave>(unpackDirectory, filesToAddToContent, codeFiles, elementName, extension, desiredNamespace, newElement);

        PluginManager.ReactToImportedElement(newElement);

        return (desiredNamespace, newElement);
    }

    private static async Task<(string desiredNamespace, GlueElement newElement)> ImportEntity(string unpackDirectory, List<string> filesToAddToContent, 
        List<string> codeFilesInZip, string elementName, string extension, 
        bool moveToSelectedFolderTreeNode, string desiredNamespace, GlueElement newElement)
    {
        var result = await ImportElement<EntitySave>(unpackDirectory, filesToAddToContent, codeFilesInZip, elementName, extension, desiredNamespace, newElement);
        string targetCs = result.targetCs;
        desiredNamespace = result.desiredNamespace;
        newElement = result.newElement;

        EntitySave entitySave = (EntitySave)newElement;

        var shouldSave = false;
        GlueCommands.Self.DoOnUiThread(() =>
        {
            var treeNode = GlueState.Self.CurrentTreeNode;
            if (moveToSelectedFolderTreeNode && treeNode?.IsFolderForEntities() == true)
            {
                var directory = treeNode.GetRelativeFilePath();
                GlueCommands.Self.GluxCommands.MoveEntityToDirectory(entitySave, directory);

                shouldSave = true;
            }
        });

        PluginManager.ReactToImportedElement(entitySave);

        if(shouldSave)
        {
            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(newElement);
            GluxCommands.Self.SaveGlux();
        }

        return (desiredNamespace, newElement);
    }

    private static async Task<(string targetCs, string desiredNamespace, GlueElement newElement)> ImportElement<T>(string unpackDirectory, List<string> filesToAddToContent, 
        List<string> codeFilesInZip, string elementName, string extension, string desiredNamespace, GlueElement newElement) where T : GlueElement
    {
        var whatToDeserialize = unpackDirectory + FileManager.RemovePath(elementName) + "." + extension;

        newElement = FileManager.XmlDeserialize<T>(whatToDeserialize);

        #region Get "Entities" or "Screens" depending on whether we're importing a Screen or Entity
        string ScreensOrEntities = null;


        if (newElement is EntitySave)
        {
            ScreensOrEntities = "Entities";
        }
        else
        {
            ScreensOrEntities = "Screens";
        }
        #endregion

        #region Create necessary directories
        string subdirectory = null;

        subdirectory = ScreensOrEntities + "/";
        if (!string.IsNullOrEmpty(elementName))
        {
            subdirectory += FileManager.GetDirectory(elementName, RelativeType.Relative);
        }
        string destinationDirectory = FileManager.RelativeDirectory + subdirectory;
        Directory.CreateDirectory(destinationDirectory);

        string contentDestinationDirectory =
            FileManager.GetDirectory(GlueCommands.Self.GetAbsoluteFileName("a.scnx", true)) + subdirectory + FileManager.RemovePath(elementName) + "/";
        Directory.CreateDirectory(contentDestinationDirectory);
        #endregion

        CreateNecessaryCustomClasses(newElement);

        FixImportedCustomVariableTypes(newElement);
        string targetCs;
        ReplaceCodeFileNamespaces(unpackDirectory, codeFilesInZip, elementName, out desiredNamespace, subdirectory, destinationDirectory, out targetCs);

        CopyContentToDestinationFolder(unpackDirectory, filesToAddToContent, contentDestinationDirectory);

        int startOfName = ProjectManager.ProjectNamespace.Length + 1 + (ScreensOrEntities + ".").Length;

        string prependToName = null;

        if (startOfName < desiredNamespace.Length)
        {
            prependToName = desiredNamespace.Substring(startOfName, desiredNamespace.Length - startOfName);
        }
        if (!string.IsNullOrEmpty(prependToName))
        {
            newElement.Name = ScreensOrEntities + "\\" + prependToName + "\\" + newElement.ClassName;
        }


        #region Prepend or remove "Content/" if necessary.  This is needed depending on which project type the file is being exported from and imported to

        // Store off the old name to new name
        // association when and if ReferencedFileSaves
        // are changed by adding/removing "Content/".
        Dictionary<string, string> oldNameNewNameDictionary = new Dictionary<string, string>();

        foreach (ReferencedFileSave rfs in newElement.ReferencedFiles)
        {
            if (rfs.Name.StartsWith("Content/"))
            {
                string newName = rfs.Name.Substring("Content/".Length);
                oldNameNewNameDictionary.Add(rfs.Name, newName);
                rfs.SetNameNoCall(newName);
            }
        }

        // We may have changed the Name of ReferencedFileSaves
        // If so, we need to see if any NamedObjectSaves reference
        // these ReferencedFileSaves, and if so change those as well.
        foreach (NamedObjectSave nos in newElement.NamedObjects)
        {
            if (!string.IsNullOrEmpty(nos.SourceFile) && oldNameNewNameDictionary.ContainsKey(nos.SourceFile))
            {
                nos.SourceFile = oldNameNewNameDictionary[nos.SourceFile];
            }

            foreach (NamedObjectSave containedNos in nos.ContainedObjects)
            {
                if (!string.IsNullOrEmpty(containedNos.SourceFile) && oldNameNewNameDictionary.ContainsKey(containedNos.SourceFile))
                {
                    containedNos.SourceFile = oldNameNewNameDictionary[containedNos.SourceFile];
                }

            }
        }

        #endregion

        string directory = FileManager.GetDirectory(newElement.Name);
        AddAdditionalCodeFilesToProject(codeFilesInZip, directory);

        AddContentFilesToProject(newElement);

        GlueCommands.Self.ProjectCommands.SaveProjects();


        #region Add the Screen or Entity to the ProjectManager

        if (newElement is ScreenSave)
        {
            await GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen((ScreenSave)newElement, suppressAlreadyExistingFileMessage: true);
        }
        else
        {
            GlueCommands.Self.GluxCommands.EntityCommands.AddEntity((EntitySave)newElement, true);
        }

        #endregion

        return (targetCs, desiredNamespace, newElement);
    }

    private static void AddContentFilesToProject(GlueElement newElement)
    {
        foreach(var rfs in newElement.ReferencedFiles)
        {
            GlueCommands.Self.ProjectCommands.UpdateFileMembershipInProject(rfs);
        }
    }

    private static void CopyContentToDestinationFolder(string unpackDirectory, List<string> filesToAddToContent, string contentDestinationDirectory)
    {
        foreach (string fileToCopyUnmodified in filesToAddToContent)
        {
            string fileToCopy = fileToCopyUnmodified;

            DialogResult dialogResult = DialogResult.Yes;

            string destinationFolder = contentDestinationDirectory;

            if (fileToCopy.StartsWith("__external/"))
            {
                destinationFolder = FileManager.GetDirectory(GlueCommands.Self.GetAbsoluteFileName("a.scnx", true));
                int indexOfSlash = fileToCopy.IndexOf("/");
                fileToCopy = fileToCopy.Substring(indexOfSlash + 1);
            }

            if (File.Exists(destinationFolder + fileToCopy))
            {
                dialogResult = MessageBox.Show("The following file already exists:\n\n" +
                                               destinationFolder + fileToCopy + "\n\nOverwrite this file?", "Overwrite file?",
                    MessageBoxButtons.YesNo);
            }

            if (dialogResult == DialogResult.Yes)
            {
                string directoryToCreate = FileManager.GetDirectory(destinationFolder + fileToCopy);

                Directory.CreateDirectory(directoryToCreate);

                File.Copy(unpackDirectory + fileToCopyUnmodified, destinationFolder + fileToCopy, true);
            }
        }
    }

    private static void ReplaceCodeFileNamespaces(string unpackDirectory, List<string> codeFilesInZip, string elementName, out string desiredNamespace, string subdirectory, string destinationDirectory, out string targetCs)
    {
        targetCs = destinationDirectory + FileManager.RemovePath(elementName) + ".cs";
        desiredNamespace = ProjectManager.ProjectNamespace + "." + subdirectory.Substring(0, subdirectory.Length - 1).Replace("/", ".").Replace("\\", ".");

        string rootToReplace = null;
        string rootToReplaceWith = ProjectManager.ProjectNamespace;

        foreach (string codeFile in codeFilesInZip)
        {
            string source = unpackDirectory + FileManager.RemovePath(codeFile);
            string target = destinationDirectory + FileManager.RemovePath(codeFile);
            string contents = FileManager.FromFileText(source);
            string replacedNamespace;

            contents = CodeWriter.ReplaceNamespace(contents, desiredNamespace, out replacedNamespace);

            if (string.IsNullOrEmpty(rootToReplace))
            {
                if (replacedNamespace.Contains('.'))
                {
                    replacedNamespace = replacedNamespace.Substring(0, replacedNamespace.IndexOf('.'));
                }
                rootToReplace = replacedNamespace;
            }

            // Let's look for using statements for this namespace and replace them
            contents = contents.Replace("using " + rootToReplace, "using " + rootToReplaceWith);

            FileManager.SaveText(contents, target);

        }
    }

    private static void FixImportedCustomVariableTypes(GlueElement newElement)
    {
        var regularExpression = new System.Text.RegularExpressions.Regex(@".+\.DataTypes\..+");
        foreach(var customVariable in newElement.CustomVariables)
        {
            var customVariableType = customVariable.Type;

            var match = regularExpression.Match(customVariableType);
            if (match?.Success == true)
            {
                var firstDot = customVariableType.IndexOf('.');
                var newType = GlueState.Self.ProjectNamespace + customVariableType.Substring(firstDot);
                customVariable.Type = newType;
            }

        }
    }

    private static void CreateNecessaryCustomClasses(GlueElement newElement)
    {
        var glueProject = GlueState.Self.CurrentGlueProject;
        var projectCustomClasses = glueProject.CustomClasses;

        // See if there are any custom classes here which don't exist on the project
        if(newElement.CustomClassesForExport != null)
        {
            foreach (var customClassFromElement in newElement.CustomClassesForExport)
            {
                var existingCustomClass = projectCustomClasses.FirstOrDefault(item => item.Name == customClassFromElement.Name);

                if(existingCustomClass == null)
                {
                    // just add it directly:
                    projectCustomClasses.Add(customClassFromElement);
                }
                else
                {
                    // add any CSVs that reference it:
                    foreach(var referencingCsvs in customClassFromElement.CsvFilesUsingThis)
                    {
                        if(
                            // this is an RFS in this entity
                            newElement.ReferencedFiles.Any(item => item.Name == referencingCsvs) && 
                            // it's not already referenced (somehow) - so we don't get dupes
                            existingCustomClass.CsvFilesUsingThis.Contains(referencingCsvs) == false)
                        {
                            existingCustomClass.CsvFilesUsingThis.Add(referencingCsvs);
                        }

                        // note - currently the entity import will not bring over other properties on the custom class, only
                        // the CSV reference. As of Apr 18 2021 Vic doesn't know if we need to expand on this...
                    }
                }
            }
        }
    }

    private static void AddAdditionalCodeFilesToProject(List<string> codeFilesInZip, string directory)
    {
        var project = GlueState.Self.CurrentMainProject;
        foreach (var relativeCodeFile in codeFilesInZip)
        {
            var codeFilePath = new FilePath(directory + relativeCodeFile);

            GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(codeFilePath, false);
        }
    }

    private static void ResolveElementReferences(IElement newElement)
    {
        #region See if the newly-added IElement has any NamedObjectSaves that reference Entities, and if so try to fix those references

        foreach (NamedObjectSave nos in newElement.AllNamedObjects)
        {
            if (nos.SourceType == SourceType.Entity)
            {
                //todo:  handle lists too
                if (!string.IsNullOrEmpty(nos.SourceClassType))
                {
                    List<IElement> candidates = ObjectFinder.Self.GetElementsUnqualified(FileManager.RemovePath(nos.SourceClassType));

                    if (candidates.Count == 0)
                    {
                        MessageBox.Show(newElement.ToString() + " has an object named " + nos.InstanceName + " which references an Entity " + nos.SourceClassType + "\n\n" +
                                        "Could not find a matching Entity.  Your project may not run properly until this issue is resolved.");
                    }
                    else
                    {
                        MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                        mbmb.MessageText = "Glue found possible matches for the object " + nos.InstanceName + " which expects the type " + nos.SourceClassType;

                        foreach(IElement candidate in candidates)
                        {
                            mbmb.AddButton("Use " + candidate.ToString(), DialogResult.OK, candidate);
                        }
                        mbmb.AddButton("Don't do anything", DialogResult.Cancel);

                        DialogResult result = mbmb.ShowDialog();

                        if (result == DialogResult.OK)
                        {
                            IElement referenceToSet = (IElement) mbmb.ClickedTag;

                            nos.SourceClassType = referenceToSet.Name;
                            nos.UpdateCustomProperties();


                            // The nos type has changed, so we should try to resolve enums
                            nos.FixEnumerationTypes();
                        }
                    }
                }

            }
        }

        #endregion

        #region See if this IElement fixes any existing invalid references

        foreach (IElement element in ProjectManager.GlueProjectSave.Entities)
        {
            if (element != newElement)
            {
                SeeIfElementSolvesMissingReferences(element, newElement);
            }
        }
        foreach (IElement element in ProjectManager.GlueProjectSave.Screens)
        {
            if (element != newElement)
            {
                SeeIfElementSolvesMissingReferences(element, newElement);
            }
        }


        #endregion
    }

    private static void SeeIfElementSolvesMissingReferences(IElement elementToCheck, IElement newElement)
    {
        string newElementUnqualifiedName = FileManager.RemovePath(newElement.Name);

        foreach (NamedObjectSave nos in elementToCheck.AllNamedObjects)
        {
            IElement fulfillingIElement = null;

            // todo:  Handle lists too
            if (!string.IsNullOrEmpty(nos.SourceClassType) && (nos.SourceType == SourceType.Entity))
            {
                fulfillingIElement = ObjectFinder.Self.GetIElement(nos.SourceClassType);

                if (fulfillingIElement == null)
                {
                    string unqualifiedName = FileManager.RemovePath(nos.SourceClassType);

                    if (unqualifiedName == newElementUnqualifiedName)
                    {
                        // This new element may fulfill the requirements.  Let's ask the user if we should do that
                        DialogResult change = MessageBox.Show(nos.ToString() + " has a missing reference.  Use the new Entity " + newElement.ToString() + "?",
                            "Update reference?", MessageBoxButtons.YesNo);

                        if (change == DialogResult.Yes)
                        {
                            nos.SourceClassType = newElement.Name;
                            nos.UpdateCustomProperties();

                        }
                    }
                }
                else if (fulfillingIElement == newElement)
                {
                    MessageBox.Show("The new element will now fulfill the previously-broken reference for " + nos.ToString());

                    // Setting the value to itself will cause the properties to be refreshed
                    nos.SourceClassType = newElement.Name;
                    nos.UpdateCustomProperties();
                }
            }

        }
    }
}