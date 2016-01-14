using FlatRedBall.Glue.Managers;
using FlatRedBall.IO;
using Gum.DataTypes;
using Gum.Managers;
using GumPlugin.CodeGeneration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GumPlugin.Managers
{
    public enum CodeGenerationSavingBehavior
    {
        AlwaysSave,
        SaveIfGeneratedDiffers
    }

    public class CodeGeneratorManager : Singleton<CodeGeneratorManager>
    {
        #region Fields

        GumPlugin.CodeGeneration.IWindowCodeGenerator mIWindowCodeGenerator;

        GumPluginCodeGenerator mGumPluginCodeGenerator;
        GueDerivingClassCodeGenerator mGueDerivingClassCodeGenerator;
        GumLayerCodeGenerator mGumLayerCodeGenerator;

        #endregion

        string GumRuntimesFolder
        {
            get
            {
                return AppState.Self.GlueProjectFolder + @"GumRuntimes\"; 
            }
        }

        public CodeGeneratorManager()
        {
            mGueDerivingClassCodeGenerator = new GueDerivingClassCodeGenerator();
        }

        public void GenerateDueToFileChange(string file)
        {
            var changedElement = GetElementFromFile(file);

            GenerateDueToFileChange(changedElement);

        }

        private void GenerateDueToFileChange(ElementSave changedElement)
        {
            // This code is going to attempt to be more efficient than a full regeneration of the entire project.
            // It will do so by:
            // 1. Only regenerating the changed element and any elements that include it
            // 2. Only regenerating elements that include it if the element that is being generated actually has different generated code
            // #1 is good if the element being generated is not being included in other elements (like Screens)
            // #2 is good if the element being generated is included in LOTS of other elements (like core elements)

            bool wasGenerated = GenerateCodeFor(changedElement, CodeGenerationSavingBehavior.SaveIfGeneratedDiffers);

            if (wasGenerated)
            {
                var whatContainsThisElement = ObjectFinder.Self.GetElementsReferencing(changedElement);

                foreach (var container in whatContainsThisElement)
                {
                    GenerateDueToFileChange(container);
                }

                if(changedElement is ScreenSave)
                {
                    foreach(var screenSave in ObjectFinder.Self.GumProjectSave.Screens)
                    {
                        if(screenSave != changedElement && screenSave.IsOfType(changedElement.Name))
                        {
                            GenerateDueToFileChange(screenSave);
                        }
                    }
                }


                if (changedElement is ComponentSave)
                {
                    foreach (var component in ObjectFinder.Self.GumProjectSave.Components)
                    {
                        if (component != changedElement && component.IsOfType(changedElement.Name))
                        {
                            GenerateDueToFileChange(component);
                        }
                    }
                }
            }
        }

        private ElementSave GetElementFromFile(string file)
        {
            var extension = FlatRedBall.IO.FileManager.GetExtension(file);

            var gumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;
            var gumDirectory = FlatRedBall.IO.FileManager.GetDirectory(gumProject.FullFileName);

            var fileRelativeToGum = FlatRedBall.IO.FileManager.MakeRelative(file, gumDirectory);

            // the file will start with something like "Components/", so remove that
            var firstForwardSlash = fileRelativeToGum.IndexOf("/");

            var fileName = fileRelativeToGum.Substring(firstForwardSlash + 1);
            fileName = FlatRedBall.IO.FileManager.RemoveExtension(fileName);

            // Gum uses the backslash:
            fileName = fileName.Replace("/", "\\").ToLowerInvariant();

            if (extension == GumProjectSave.ScreenExtension)
            {
                return gumProject.Screens.FirstOrDefault(item => item.Name.ToLowerInvariant() == fileName);
                //var screens = gumProject.ScreenReferences[0]
            }
            else if(extension == GumProjectSave.ComponentExtension)
            {
                return gumProject.Components.FirstOrDefault(item => item.Name.ToLowerInvariant() == fileName);

            }
            else if(extension == GumProjectSave.StandardExtension)
            {
                return gumProject.StandardElements.FirstOrDefault(item => item.Name.ToLowerInvariant() == fileName);

            }

            return null;
        }

        public void GenerateDerivedGueRuntimes()
        {
            if (AppState.Self.GumProjectSave == null)
            {
                var rfs = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.GetAllReferencedFiles()
                    .FirstOrDefault(item => FlatRedBall.IO.FileManager.GetExtension(item.Name) == "gumx");

                if (rfs != null)
                {
                    string fullFileName = FlatRedBall.Glue.ProjectManager.ContentDirectory + rfs.Name;

                    string gumXDirectory = FlatRedBall.IO.FileManager.GetDirectory(fullFileName);

                    FileReferenceTracker.Self.LoadGumxIfNecessaryFromDirectory(gumXDirectory);
                }
            }

            if (Gum.Managers.ObjectFinder.Self.GumProjectSave != null)
            {

                string directoryToSave = GumRuntimesFolder;

                System.IO.Directory.CreateDirectory(directoryToSave);

                bool wasAnythingAdded = false;

                wasAnythingAdded |= GenerateAndSaveRuntimeAssociations();

                foreach (var element in AppState.Self.AllLoadedElements)
                {
                    bool wasSaved = GenerateCodeFor(element);

                    if (wasSaved)
                    {
                        string location = directoryToSave + element.Name + "Runtime.Generated.cs";
                        wasAnythingAdded |=
                            FlatRedBall.Glue.ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(
                            FlatRedBall.Glue.ProjectManager.ProjectBase, location);
                    }
                }

                if (wasAnythingAdded)
                {
                    FlatRedBall.Glue.ProjectManager.SaveProjects();
                }
            }

        }

        /// <summary>
        /// Generates and saves the code for the passed Gum ElementSave, but does not add the resulting .cs file to the VisualStudio project.
        /// </summary>
        /// <param name="element">The element to generate.</param>
        /// <returns></returns>
        public bool GenerateCodeFor(Gum.DataTypes.ElementSave element, CodeGenerationSavingBehavior savingBehavior = CodeGenerationSavingBehavior.AlwaysSave)
        {
            bool wasSaved = false;

            string directoryToSave = GumRuntimesFolder;

            string generatedCode = mGueDerivingClassCodeGenerator.GenerateCodeFor(element);

            bool shouldSave;

            string saveLocation = directoryToSave + element.Name + "Runtime.Generated.cs";

            if(savingBehavior == CodeGenerationSavingBehavior.AlwaysSave)
            {
                shouldSave = true;
            }
            else // if(savingBehavior == CodeGenerationSavingBehavior.SaveIfGeneratedDiffers)
            {
                // We only want to save this file if what we've just generated is different than what is already on disk:
                if(!System.IO.File.Exists(saveLocation))
                {
                    shouldSave = true;
                }
                else
                {
                    var existingText = File.ReadAllText(saveLocation);

                    shouldSave = existingText != generatedCode;
                }
            }

            if (!string.IsNullOrEmpty(generatedCode) && shouldSave)
            {
                var directory = FileManager.GetDirectory(saveLocation);
                Directory.CreateDirectory(directory);
                const int timesToTry = 4;
                int timesTried = 0;
                while (true)
                {
                    try
                    {
                        System.IO.File.WriteAllText(saveLocation, generatedCode);
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

            }

            return wasSaved;
        }

        private bool GenerateAndSaveRuntimeAssociations()
        {
            bool wasAdded = false;

            string contents = GueRuntimeTypeAssociationGenerator.Self.GetRuntimeRegistrationPartialClassContents();

            string whereToSave = GumRuntimesFolder + "GumIdb.Generated.cs";

            System.IO.File.WriteAllText(whereToSave, contents);

            wasAdded |=
                FlatRedBall.Glue.ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(
                FlatRedBall.Glue.ProjectManager.ProjectBase, whereToSave);

            return wasAdded;
        }

        public void CreateCodeGenerators()
        {
            mIWindowCodeGenerator = new CodeGeneration.IWindowCodeGenerator();
            FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Add(mIWindowCodeGenerator);

            mGumPluginCodeGenerator = new GumPluginCodeGenerator();
            FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Add(mGumPluginCodeGenerator);

            mGumLayerCodeGenerator = new GumLayerCodeGenerator();
            FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Add(mGumLayerCodeGenerator);
        }

        internal void RemoveCodeGenerators()
        {
            if (FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Contains(mIWindowCodeGenerator))
            {
                FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Remove(mIWindowCodeGenerator);
            }

            if (FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Contains(mGumPluginCodeGenerator))
            {
                FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Remove(mGumPluginCodeGenerator);
            }

            if (FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Contains(mGumLayerCodeGenerator))
            {
                FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Remove(mGumLayerCodeGenerator);
            }

        }
    }
}
