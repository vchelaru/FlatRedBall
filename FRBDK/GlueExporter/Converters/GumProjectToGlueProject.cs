using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using FlatRedBall.Glue.SaveClasses;

using GlueScreenSave = FlatRedBall.Glue.SaveClasses.ScreenSave;
using GlueElement = FlatRedBall.Glue.SaveClasses.IElement;
using GumElement = global::Gum.DataTypes.ElementSave;
using GumStateSave = global::Gum.DataTypes.Variables.StateSave;
using FlatRedBall.Utilities;

namespace FlatRedBall.Gum.Converters
{
    #region CopiedFileReference class
    public class CopiedFileReference
    {
        public string Source;
        public string Destination;
    }
    #endregion

    public class GumProjectToGlueProject
    {
        #region Fields

        // "Current" values storing the context of conversion, so we don't have to pass this information throughout methods
        string mGluxFolder;
        GumProjectSave mGumProjectSave;
        GlueProjectSave mGlueProjectSave;
        GumInstanceToGlueNamedObjectSave mInstanceToNos = new GumInstanceToGlueNamedObjectSave();

        #endregion

        public GlueProjectSave ToGlueProjectSave(GumProjectSave gumProjectSave, string gluxFolder)
        {
            mGluxFolder = gluxFolder;
            mGumProjectSave = gumProjectSave;
            mInstanceToNos.GumProjectSave = mGumProjectSave;

            GlueProjectSave toReturn = new GlueProjectSave();
            mGlueProjectSave = toReturn;

            var copiedFiles = CopyExternalFilesToProjects();

            AddScreensAndEntities(copiedFiles);

            return toReturn;
        }

        private Dictionary<string, CopiedFileReference> CopyExternalFilesToProjects()
        {
            string contentFolder = GetContentFolder();


            Dictionary<string, CopiedFileReference> allSourceFiles = GetAllSourceFiles();
            CopySourceFiles(allSourceFiles, contentFolder);

            return allSourceFiles;

        }

        private string GetContentFolder()
        {
            string projectName = FileManager.RemovePath(mGluxFolder).Replace("\\", "");

            string contentFolder = FileManager.GetDirectory(mGluxFolder) + projectName + "Content\\";
            return contentFolder;
        }



        private void CopySourceFiles(Dictionary<string, CopiedFileReference> allSourceFiles, string contentFolder)
        {
            string commonFolder = "";
            string copiedFolder = contentFolder + "CopiedFromGum\\";

            if (allSourceFiles.Count != 0)
            {
                commonFolder = FileManager.GetDirectory(allSourceFiles.Keys.First());
            }

            foreach (var key in allSourceFiles.Keys)
            {
                while (!string.IsNullOrEmpty(commonFolder) && !FileManager.IsRelativeTo(key, commonFolder))
                {
                    commonFolder = FileManager.GetDirectory(commonFolder);
                }
            }
            string copyingErrors = null;
            foreach (var kvp in allSourceFiles)
            {
                try
                {
                    var value = kvp.Value;

                    string relativeSource = FileManager.MakeRelative(value.Source, commonFolder);

                    value.Destination = copiedFolder + relativeSource;
                    string destinationDirectory = FileManager.GetDirectory(value.Destination);
                    if (!Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }
                    if (File.Exists(value.Source))
                    {
                        // If the destination is read-only, let's just force copy over it
                        FileInfo fileInfo = new FileInfo(value.Destination);

                        if (fileInfo.IsReadOnly && fileInfo.Exists)
                        {
                            fileInfo.IsReadOnly = false;
                        }

                        File.Copy(value.Source, value.Destination, true);
                    }
                }
                catch (Exception e)
                {
                    copyingErrors += "Error copying file: " + kvp.Value.Source + "\n" + e.Message + "\n";
                }
            }

            if (!string.IsNullOrEmpty(copyingErrors))
            {
                MessageBox.Show(copyingErrors);
            }
        }

        private Dictionary<string, CopiedFileReference> GetAllSourceFiles()
        {
            Dictionary<string, CopiedFileReference> copiedFiles = new Dictionary<string, CopiedFileReference>();

            string gumProjectFolder = FileManager.GetDirectory( mGumProjectSave.FullFileName);
            foreach (var standardElement in mGumProjectSave.StandardElements)
            {
                var states = standardElement.States;

                FillUsedFilesFromState(copiedFiles, gumProjectFolder, states);
            }

            foreach (var component in mGumProjectSave.Components)
            {
                var states = component.States;

                FillUsedFilesFromState(copiedFiles, gumProjectFolder, states);
            }

            foreach (var screen in mGumProjectSave.Screens)
            {
                var states = screen.States;

                FillUsedFilesFromState(copiedFiles, gumProjectFolder, states);
            }

            return copiedFiles;
        }

        private static void FillUsedFilesFromState(Dictionary<string, CopiedFileReference> copiedFiles, string gumProjectFolder, List<global::Gum.DataTypes.Variables.StateSave> states)
        {
            foreach (var state in states)
            {
                foreach (var variable in state.Variables.Where(item => item.Name == "SourceFile" || item.GetRootName() == "SourceFile"))
                {
                    if (variable != null && !string.IsNullOrEmpty(variable.Value as string))
                    {
                        string relativeValue = variable.Value as string;
                        if (!FileManager.IsRelative(relativeValue))
                        {
                            relativeValue = FileManager.MakeRelative(relativeValue, gumProjectFolder);
                        }
                        string fullPath = FileManager.RemoveDotDotSlash(gumProjectFolder + relativeValue);



                        if (!copiedFiles.ContainsKey(fullPath.ToLower()))
                        {
                            CopiedFileReference reference = new CopiedFileReference();
                            reference.Source = fullPath;
                            reference.Destination = "";
                            copiedFiles.Add(fullPath.ToLower(), reference);
                        }
                    }
                }
            }
        }

        private void AddScreensAndEntities(Dictionary<string, CopiedFileReference> copiedFiles)
        {
            foreach (var component in mGumProjectSave.Components)
            {
                var entity = ToGlueEntitySave(component, copiedFiles);


                mGlueProjectSave.Entities.Add(entity);
            }

            foreach (var screen in mGumProjectSave.Screens)
            {
                var glueScreen = ToGlueScreenSave(screen, copiedFiles);

                mGlueProjectSave.Screens.Add(glueScreen);
            }
        }

        private GlueScreenSave ToGlueScreenSave(global::Gum.DataTypes.ScreenSave screen, Dictionary<string, CopiedFileReference> copiedFiles)
        {
            GlueScreenSave toReturn = new GlueScreenSave();
            toReturn.Name = "Screens\\" + screen.Name;

            // Make RFS's first so we can rename NOS's if necessary
            AddReferencedFilesToElement(screen, toReturn, copiedFiles);

            mInstanceToNos.AddNamedObjectSavesToGlueElement(screen, toReturn, copiedFiles);
            return toReturn;

        }


        private EntitySave ToGlueEntitySave(ComponentSave component, Dictionary<string, CopiedFileReference> copiedFiles)
        {
            EntitySave toReturn = new EntitySave();
            toReturn.Name = "Entities\\" + component.Name;

            // Make RFS's first so we can rename NOS's if necessary
            AddReferencedFilesToElement(component, toReturn, copiedFiles);

            mInstanceToNos.AddNamedObjectSavesToGlueElement(component, toReturn, copiedFiles);
            return toReturn;
        }

        private void AddReferencedFilesToElement(GumElement from, GlueElement to, Dictionary<string, CopiedFileReference> copiedFiles)
        {
            string gumDirectory = FileManager.GetDirectory(mGumProjectSave.FullFileName);
            string contentDirectory = GetContentFolder();
            foreach (GumStateSave stateSave in from.States)
            {

                foreach (VariableSave variable in stateSave.Variables.Where(item=>item.IsFile && item.Value != null && item.SetsValue))
                {
                    string sourceFile = (string)variable.Value;

                    string absoluteFile = FileManager.RemoveDotDotSlash(gumDirectory + sourceFile).ToLower();

                    if (copiedFiles.ContainsKey(absoluteFile))
                    {
                        string destination = copiedFiles[absoluteFile].Destination;
                        string rfsName = FileManager.MakeRelative(destination, contentDirectory);
                        if (!to.ReferencedFiles.Any(item => item.Name == rfsName))
                        {
                            // create a RFS here
                            ReferencedFileSave rfs = new ReferencedFileSave();
                            rfs.Name = rfsName;
                            rfs.RuntimeType = "Microsoft.Xna.Framework.Graphics.Texture2D";
                            to.ReferencedFiles.Add(rfs);
                        }
                    }
                }
            }

        }

    }
}
