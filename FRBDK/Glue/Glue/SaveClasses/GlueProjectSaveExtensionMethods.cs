using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using KellermanSoftware.CompareNetObjects;
using FlatRedBall.IO;
using System.Xml.Serialization;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Managers;
using EditorObjects.SaveClasses;
using System.Threading.Tasks;
using FlatRedBall.Glue.FormHelpers;
using EditorObjects.IoC;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using Newtonsoft.Json;
using FlatRedBall.Glue.VSHelpers.Projects;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class GlueProjectSaveExtensionMethods
    {
        public static void RemoveInvalidStatesFromNamedObjects(this GlueProjectSave glueProjectSave, bool showPopupsOnFixedErrors)
        {
            foreach (EntitySave entitySave in glueProjectSave.Entities)
            {
                foreach (NamedObjectSave nos in entitySave.NamedObjects)
                {

                    glueProjectSave.TryToRemoveInvalidState(showPopupsOnFixedErrors, entitySave, nos);
                }
            }

            foreach (ScreenSave screenSave in glueProjectSave.Screens)
            {
                foreach (NamedObjectSave nos in screenSave.NamedObjects)
                {
                    glueProjectSave.TryToRemoveInvalidState(showPopupsOnFixedErrors, screenSave, nos);
                }
            }
        }

        private static void TryToRemoveInvalidState(this GlueProjectSave glueProjectSave, bool showPopupsOnFixedErrors, IElement containingElement, NamedObjectSave nos)
        {
            if (nos.SourceType == SourceType.Entity && !string.IsNullOrEmpty(nos.SourceClassType) && !string.IsNullOrEmpty(nos.CurrentState))
            {
                EntitySave foundEntitySave = glueProjectSave.GetEntitySave(nos.SourceClassType);

                if (foundEntitySave != null)
                {
                    bool hasFoundState = false;

                    hasFoundState = foundEntitySave.GetStateRecursively(nos.CurrentState) != null;

                    if (!hasFoundState)
                    {
                        if (showPopupsOnFixedErrors)
                        {
                            Container.Get<IGlueCommands>().DialogCommands.ShowMessageBox(
                                "The Object " + nos.InstanceName + " in " + containingElement.Name + " uses the invalid state " + nos.CurrentState +
                                "\nRemoving this current State");
                        }

                        nos.CurrentState = null;

                    }
                }
            }
        }

        public static void PostLoadInitialize(this GlueProjectSave glueProjectSave, out string errors)
        {
            errors = null;

            foreach (ScreenSave screenSave in glueProjectSave.Screens)
            {
                try
                {
                    screenSave.PostLoadInitialize();
                }
                catch (Exception e)
                {
                    errors += "Error post-initialize in Screen " + screenSave.Name + ": " + e.Message;
                }
            }
            foreach (EntitySave entitySave in glueProjectSave.Entities)
            {
                try
                {
                    entitySave.PostLoadInitialize();
                }
                catch (Exception e)
                {
                    errors += "Error post-initialize in Screen " + entitySave.Name + ": " + e.Message;
                }
            }
            
        }

        public static ReferencedFileSave AddReferencedFileSave(IElement element, string directoryPath, 
            string fileName, 
            AssetTypeInfo resultAssetTypeInfo, object option, out string errorMessage)
        {
            if(string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            char invalidCharacter;
            ReferencedFileSave rfs = null;
            errorMessage = null;

            #region Get directory

            string directoryRelativeToContent = "";

            if (!String.IsNullOrEmpty(directoryPath))
            {
                //string directory = directoryTreeNode.GetRelativePath().Replace("/", "\\");

                directoryRelativeToContent = directoryPath;
            }
            else if (element != null)
            {
                //string directory = elementToAddTo.GetRelativePath().Replace("/", "\\");

                directoryRelativeToContent = element.Name.Replace(@"/", @"\") + "\\";
            }
            else
            {
                directoryRelativeToContent = "GlobalContent/";
            }


            #endregion

            bool userCancelled = false;


            #region If there is some reason why the file name won't, then work show a message box

            string whyIsntValid;

            if (!NameVerifier.IsReferencedFileNameValid(fileName, resultAssetTypeInfo, rfs, element, out whyIsntValid))
            {
                errorMessage = "Invalid file name:\n" + fileName + "\n" + whyIsntValid;
            }
            else if (resultAssetTypeInfo == null)
            {
                errorMessage = "You must select a valid type for your new file.";
            }
            else if (GlueCommands.Self.GluxCommands.GetReferencedFileSaveFromFile(directoryRelativeToContent + fileName + "." + resultAssetTypeInfo.Extension) != null)
            {
                errorMessage = "There is already a file named\n\n" + directoryRelativeToContent + fileName + "." + resultAssetTypeInfo.Extension;
            }
            // TODO:  This currently checks for an exact match, but we should prevent different files (like a .emix and .scnx) from having the same name
            else if (element != null &&
                element.GetReferencedFileSaveRecursively(directoryRelativeToContent + fileName + "." + resultAssetTypeInfo.Extension) != null)
            {
                errorMessage = "There is already a file named " + fileName;
            }
            // need to check global content for duplicates.  This is not implemented yet.
            #endregion

            #region Else, create the file

            else
            {
                string createdFile = PluginManager.CreateNewFile(resultAssetTypeInfo, option,
                    directoryRelativeToContent, fileName);

                if(createdFile == null)
                {
                    throw new NullReferenceException(nameof(createdFile));
                }

                // let's just re-route this 
                // to the code that adds existing
                // files now that we have a file and
                // that's exactly what we're doing.
                rfs = AddExistingFileManager.Self.AddSingleFile(createdFile, ref userCancelled, option, elementToAddTo:element);

                if (rfs == null && !userCancelled)
                {
                    throw new NullReferenceException("The RFS shouldn't be null");
                }

            }

            #endregion

            if (option != null)
            {
                if (option is bool)
                {
                    // do nothing
                }
                else
                {
                    var propertySave = new PropertySave
                    { Name = "CreationOptions", Value = JsonConvert.SerializeObject(option) };

                    rfs.Properties.Add(propertySave);

                }
            }




            return rfs;
        }

        public static void FixErrors(this GlueProjectSave instance, bool showPopupsOnFixedErrors)
        {
            instance.SortScreens();

            instance.CleanUnusedVariablesFromStates();

            instance.FixReferencedFileErrors(showPopupsOnFixedErrors);

            // We do this in Glue in a separate step - do we want to do it here too?
            //instance.FixEnumerationValues();

            instance.FixNamedObjects();

            instance.SearchForDuplicateEntities();

            instance.VerifyAllElementsCustomCodeIsPartOfProject();
        }

        public static void FixReferencedFileErrors(this GlueProjectSave instance, bool showPopupsOnFixedErrors)
        {
            List<ReferencedFileSave> rfsList = null;


            foreach (EntitySave entitySave in instance.Entities)
            {
                rfsList = entitySave.ReferencedFiles;

                instance.FixReferencedFileBackSlash(rfsList);

            }

            foreach (ScreenSave screenSave in instance.Screens)
            {
                rfsList = screenSave.ReferencedFiles;

                instance.FixReferencedFileBackSlash(rfsList);
            }

            rfsList = instance.GlobalFiles;
            instance.FixReferencedFileBackSlash(rfsList);

            //The methods below should be broken up and moved above
            //to improve performance.
            instance.FixEmptyReferencedFileSaves(showPopupsOnFixedErrors);

            instance.FixReferencedFileSaveContentPipelineSettings();
        }

        public static void FixReferencedFileSaveContentPipelineSettings(this GlueProjectSave instance)
        {
            Parallel.ForEach(instance.Entities, (entitySave) =>
            //foreach (EntitySave entitySave in instance.Entities)
            {
                List<ReferencedFileSave> rfsList = entitySave.ReferencedFiles;

                FixRfsListContentPipelineSetting(rfsList);
            });


            Parallel.ForEach(instance.Screens, (screenSave) =>
            //foreach (ScreenSave screenSave in instance.Screens)
            {
                List<ReferencedFileSave> rfsList = screenSave.ReferencedFiles;

                FixRfsListContentPipelineSetting(rfsList);
            });

            FixRfsListContentPipelineSetting(instance.GlobalFiles);
        }

        private static void FixRfsListContentPipelineSetting(List<ReferencedFileSave> rfsList)
        {
            foreach (ReferencedFileSave rfs in rfsList)
            {
                if (rfs.GetAssetTypeInfo() != null &&
                    rfs.GetAssetTypeInfo().MustBeAddedToContentPipeline &&
                    rfs.UseContentPipeline == false)
                {
                    rfs.UseContentPipeline = true;
                }
            }
        }

        public static IEnumerable<GlueElement> AllElements(this GlueProjectSave instance)
        {
            foreach (ScreenSave screen in instance.Screens)
            {
                yield return screen;
            }
            foreach (EntitySave entity in instance.Entities)
            {
                yield return entity;
            }
        }

        private static void FixEmptyReferencedFileSaves(this GlueProjectSave instance, bool showPopupsOnFixedErrors)
        {
            Parallel.ForEach(instance.Entities, (entitySave) =>
            //foreach (EntitySave entitySave in instance.Entities)
            {
                for (int i = entitySave.ReferencedFiles.Count - 1; i > -1; i--)
                {
                    ReferencedFileSave rfs = entitySave.ReferencedFiles[i];

                    if (string.IsNullOrEmpty(rfs.Name))
                    {
                        Container.Get<IGlueCommands>().PrintOutput("Removing an empty file from " + entitySave.ToString());
                        entitySave.ReferencedFiles.RemoveAt(i);
                    }
                }
            });

            Parallel.ForEach(instance.Screens, (screen) =>
            //foreach (ScreenSave screen in instance.Screens)
            {
                for (int i = screen.ReferencedFiles.Count - 1; i > -1; i--)
                {
                    ReferencedFileSave rfs = screen.ReferencedFiles[i];

                    if (string.IsNullOrEmpty(rfs.Name))
                    {
                        Container.Get<IGlueCommands>().PrintOutput("Removing an empty file from " + screen.ToString());
                        screen.ReferencedFiles.RemoveAt(i);
                    }
                }
            });
        }

        public static void FixAllTypes(this GlueProjectSave instance)
        {
            foreach (EntitySave entitySave in instance.Entities)
            {
                entitySave.FixAllTypes();

            }

            foreach (ScreenSave screen in instance.Screens)
            {
                screen.FixAllTypes();
            }

            foreach(var file in instance.GlobalFiles)
            {
                file.FixAllTypes();
            }
        }

        public static void FixEnumerationValues(this GlueProjectSave instance)
        {
            foreach (EntitySave entitySave in instance.Entities)
            {
                entitySave.FixEnumerationValues();

            }

            foreach (ScreenSave screen in instance.Screens)
            {
                screen.FixEnumerationValues();
            }
        }

        public static void ConvertEnumerationValuesToInts(this GlueProjectSave instance)
        {
            foreach (EntitySave entitySave in instance.Entities)
            {
                entitySave.ConvertEnumerationValuesToInts();

            }

            foreach (ScreenSave screen in instance.Screens)
            {
                screen.ConvertEnumerationValuesToInts();
            }
        }

        public static ComparisonResult ReloadUsingComparison(this GlueProjectSave instance, string fileName, out GlueProjectSave otherGlueProjectSave)
        {
            bool succeeded = true;

            // try loading multiple times just in case the file is being written by another application

            int timesTried = 0;
            int maxTries = 5;
            otherGlueProjectSave = null;
            while (timesTried < maxTries)
            {
                try
                {
                    // When we load this thing, we need to have all of its variables
                    // prepared for runtime.  This means things like fixing enumerations,
                    // which requires usage of the ObjectFinder.  Therefore we need to tell
                    // the ObjectFinder to use this GlueProjectSave, do the initializeation, then
                    // go back to the old one.
                    
                    otherGlueProjectSave = GlueProjectSaveExtensions.Load(fileName);

                    GlueProjectSave savedOld = ObjectFinder.Self.GlueProject;
                    ObjectFinder.Self.GlueProject = otherGlueProjectSave;
                    otherGlueProjectSave.FixAllTypes();

                    ObjectFinder.Self.GlueProject = savedOld;


                    break;
                }
                catch (Exception e)
                {
                    timesTried++;
                    if (timesTried < maxTries)
                    {
                        System.Threading.Thread.Sleep(25);

                    }
                    else
                    {
                        GlueCommands.Self.DialogCommands.ShowMessageBox("There was an error loading the .glux:\n\n" + e.ToString());
                        otherGlueProjectSave = null;
                        succeeded = false;
                    }
                }
            }

            if (succeeded)
            {
                var compareObjects = new CompareLogic();

                compareObjects.Config.MembersToIgnore.Add("TypedMembers");
                compareObjects.Config.MembersToIgnore.Add("UsesTranslation");
                compareObjects.Config.MembersToIgnore.Add("ContainerType");
                compareObjects.Config.MembersToIgnore.Add("ImageWidth");
                compareObjects.Config.MembersToIgnore.Add("ImageHeight");
                compareObjects.Config.MembersToIgnore.Add("EquilibriumParticleCount");
                compareObjects.Config.MembersToIgnore.Add("BurstParticleCount");
                compareObjects.Config.MembersToIgnore.Add("RuntimeType");
                compareObjects.Config.MembersToIgnore.Add("EventSave");
                compareObjects.Config.MembersToIgnore.Add("SharedCodeFullFileName");
                compareObjects.Config.AttributesToIgnore.Add(typeof(XmlIgnoreAttribute));
                compareObjects.Config.MaxDifferences = int.MaxValue;

                var compareResult = compareObjects.Compare(instance, otherGlueProjectSave);
                return compareResult;

            }
            else
            {
                return null;
            }
        }

        static void VerifyAllElementsCustomCodeIsPartOfProject(this GlueProjectSave instance)
        {
            foreach (var screen in instance.Screens)
            {
                VerifyElementMembershipInProject(screen);
            }
            foreach (var entity in instance.Entities)
            {
                VerifyElementMembershipInProject(entity);
            }
        }

        private static void VerifyElementMembershipInProject(IElement element)
        {
            string fileName = element.Name + ".cs";

            string absoluteCodeFile = FileManager.RelativeDirectory + fileName;

            if (ProjectManager.ProjectBase.GetItem(absoluteCodeFile) == null)
            {
                ((VisualStudioProject)ProjectManager.ProjectBase.CodeProject).AddCodeBuildItem(absoluteCodeFile);
            }
        }

        private static void SearchForDuplicateNamedObjects(this GlueProjectSave instance)
        {
            List<string> names = new List<string>();
            foreach (EntitySave entitySave in instance.Entities)
            {
                names.Clear();

                foreach (NamedObjectSave nos in entitySave.NamedObjects)
                {
                    if (names.Contains(nos.InstanceName))
                    {
                        GlueCommands.Self.DialogCommands.ShowMessageBox("There are two objects named " + nos.InstanceName + " in the entity " + entitySave.ToString());
                    }
                    else
                    {
                        names.Add(nos.InstanceName);
                    }

                }

            }


            foreach (ScreenSave screenSave in instance.Screens)
            {
                names.Clear();

                foreach (NamedObjectSave nos in screenSave.NamedObjects)
                {
                    if (names.Contains(nos.InstanceName))
                    {
                        GlueCommands.Self.DialogCommands.ShowMessageBox("There are two objects named " + nos.InstanceName + " in the entity " + screenSave.ToString());
                    }
                    else
                    {
                        names.Add(nos.InstanceName);
                    }
                }
            }
        }


        public static void FixNamedObjects(this GlueProjectSave glueProjectSave)
        {
            glueProjectSave.SearchForDuplicateNamedObjects();

            glueProjectSave.FixBackSlashSourcefiles();

            glueProjectSave.FixAttachmentProperties();

            glueProjectSave.FixMissingDerivedNamedObjectsAndVariables();
        }


        public static void SearchForDuplicateEntities(this GlueProjectSave instance)
        {
            Dictionary<string, EntitySave> entitiesVisited = new Dictionary<string, EntitySave>();

            foreach (EntitySave entitySave in instance.Entities)
            {
                if (entitiesVisited.ContainsKey(entitySave.Name))
                {
                    Container.Get<IGlueCommands>().DialogCommands.ShowMessageBox(
                        "The GLUX file contains duplicate entires for\n\n" + entitySave.Name +
                        "\n\nYou should close Glue, open the GLUX in a text editor, remove one of the duplicates, then save the GLUX file");
                }
                else
                {
                    entitiesVisited.Add(entitySave.Name, entitySave);
                }
            }
        }

        public static void CleanUnusedVariablesFromStates(this GlueProjectSave instance)
        {
            Parallel.ForEach(instance.Entities, (entitySave) =>
            {
                //foreach (EntitySave entitySave in instance.Entities)
                //{
                entitySave.CleanUnusedVariablesFromStates();
            });


            Parallel.ForEach(instance.Screens, (screen) =>
            //foreach (ScreenSave screen in instance.Screens)
            {
                screen.CleanUnusedVariablesFromStates();
            });
        }

        public static void FixAttachmentProperties(this GlueProjectSave instance)
        {
            foreach (EntitySave entitySave in instance.Entities)
            {
                foreach (var nos in entitySave.NamedObjects)
                {
                    nos.AttachToCamera = false;
                }
            }
        }

        public static void FixMissingDerivedNamedObjectsAndVariables(this GlueProjectSave instance)
        {
            foreach (var screen in instance.Screens)
            {
                GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(screen);
            }
            foreach (var entity in instance.Entities)
            {
                GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(entity);
            }
        }
    }
}
