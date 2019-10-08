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
using System.Threading.Tasks;
using Gum.DataTypes.Behaviors;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.IO;

namespace GumPlugin.Managers
{
    #region Structs/Enums
    public struct GenerationResult
    {
        public bool DidSaveGenerated;
        public bool DidSaveCustom;
    }


    public enum CodeGenerationSavingBehavior
    {
        AlwaysSave,
        SaveIfGeneratedDiffers
    }

    #endregion

    public class CodeGeneratorManager : Singleton<CodeGeneratorManager>
    {
        #region Fields

        GumPlugin.CodeGeneration.IWindowCodeGenerator mIWindowCodeGenerator;

        GumPluginCodeGenerator mGumPluginCodeGenerator;
        GueDerivingClassCodeGenerator mGueDerivingClassCodeGenerator;
        GumLayerCodeGenerator mGumLayerCodeGenerator;
        GumLayerAssociationCodeGenerator gumLayerAssociationCodeGenerator;
        BehaviorCodeGenerator behaviorCodeGenerator;
        GumToFlatRedBallAttachmentCodeGenerator gumToFlatRedBallAttachmentCodeGenerator;

        #endregion

        string GumRuntimesFolder
        {
            get
            {
                return AppState.Self.GlueProjectFolder + @"GumRuntimes\"; 
            }
        }

        string GumBehaviorsFolder
        {
            get
            {
                return GumRuntimesFolder + @"Behaviors\";
            }
        }

        public CodeGeneratorManager()
        {
            mGueDerivingClassCodeGenerator = new GueDerivingClassCodeGenerator();
            behaviorCodeGenerator = new BehaviorCodeGenerator();
        }

        public void GenerateDueToFileChange(FilePath file)
        {
            string extension = file.Extension;
            if(extension == "gumx")
            {
                foreach(var screen in ObjectFinder.Self.GumProjectSave.Screens)
                {
                    GenerateCodeFor(screen);
                }
                foreach (var component in ObjectFinder.Self.GumProjectSave.Components)
                {
                    GenerateCodeFor(component);
                }
                foreach (var standard in ObjectFinder.Self.GumProjectSave.StandardElements)
                {
                    GenerateCodeFor(standard);
                }
            }
            else
            {
                var changedElement = GetElementFromFile(file);

                // Maybe the element doesn't exist - like it's a .gucx that is not part of the .gumx
                if(changedElement != null)
                {
                    GenerateDueToFileChange(changedElement);
                }

            }
        }

        private void GenerateDueToFileChange(ElementSave changedElement)
        {
            if(changedElement == null)
            {
                throw new ArgumentNullException("changedElement");
            }

            // This code is going to attempt to be more efficient than a full regeneration of the entire project.
            // It will do so by:
            // 1. Only regenerating the changed element and any elements that include it
            // 2. Only regenerating elements that include it if the element that is being generated actually has different generated code
            // #1 is good if the element being generated is not being included in other elements (like Screens)
            // #2 is good if the element being generated is included in LOTS of other elements (like core elements)

            var generationResult = GenerateCodeFor(changedElement, CodeGenerationSavingBehavior.SaveIfGeneratedDiffers);

            if (generationResult.DidSaveGenerated)
            {
                var whatContainsThisElement = ObjectFinder.Self.GetElementsReferencing(changedElement);

                foreach (var container in whatContainsThisElement)
                {
                    GenerateDueToFileChange(container);
                }

                if(changedElement is Gum.DataTypes.ScreenSave)
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

        public static ElementSave GetElementFrom(ReferencedFileSave rfs)
        {
            FilePath filePath = GlueCommands.Self.GetAbsoluteFileName(rfs);

            return GetElementFromFile(filePath);
        }

        private static ElementSave GetElementFromFile(FilePath file)
        {
            var extension = file.Extension;

            var gumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;
            var gumDirectory = FlatRedBall.IO.FileManager.GetDirectory(gumProject.FullFileName);

            var fileRelativeToGum = FlatRedBall.IO.FileManager.MakeRelative(file.Standardized, gumDirectory);

            // the file will start with something like "Components/", so remove that
            var firstForwardSlash = fileRelativeToGum.IndexOf("/");

            var fileName = fileRelativeToGum.Substring(firstForwardSlash + 1);
            fileName = FlatRedBall.IO.FileManager.RemoveExtension(fileName);

            // Gum uses the backslash:
            // Update 
            FilePath filePath = new FilePath(fileName);

            if (extension == GumProjectSave.ScreenExtension)
            {
                return gumProject.Screens.FirstOrDefault(item => item.Name.ToLowerInvariant() == filePath);
                //var screens = gumProject.ScreenReferences[0]
            }
            else if(extension == GumProjectSave.ComponentExtension)
            {
                return gumProject.Components.FirstOrDefault(item => item.Name.ToLowerInvariant() == filePath);

            }
            else if(extension == GumProjectSave.StandardExtension)
            {
                return gumProject.StandardElements.FirstOrDefault(item => item.Name.ToLowerInvariant() == filePath);

            }

            return null;
        }

        public void GenerateDerivedGueRuntimes()
        {
            if (AppState.Self.GumProjectSave == null &&
                FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject != null)
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

                var elements = AppState.Self.AllLoadedElements.ToList();

                var errors = new List<string>();
                var obj = new object();

                // This can greatly improve speed, just don't put async calls in here or it won't block 
                Parallel.ForEach(elements, (element) =>
                {
                    GenerationResult generationResult = new GenerationResult();
                    try
                    {
                        generationResult = GenerateCodeFor(element);
                    }
                    catch(Exception e)
                    {
                        lock (obj)
                        {
                            errors.Add(e.ToString());
                        }
                    }

                    if (generationResult.DidSaveGenerated)
                    {
                        string location = directoryToSave + element.Name + "Runtime.Generated.cs";
                        wasAnythingAdded |=
                            FlatRedBall.Glue.ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(
                            FlatRedBall.Glue.ProjectManager.ProjectBase, location);
                    }
                    if (generationResult.DidSaveCustom)
                    {
                        string location = directoryToSave + element.Name + "Runtime.cs";
                        wasAnythingAdded |=
                            FlatRedBall.Glue.ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(
                            FlatRedBall.Glue.ProjectManager.ProjectBase, location);
                    }
                });

                foreach(var err in errors)
                {
                    GlueCommands.Self.PrintError(err);
                }

                if (wasAnythingAdded)
                {
                    FlatRedBall.Glue.ProjectManager.SaveProjects();
                }
            }

        }

        /// <summary>
        /// Generates and saves the code for the passed Gum ElementSave (both generated and custom code template), 
        /// but does not add the resulting .cs files to the VisualStudio project.
        /// </summary>
        /// <param name="element">The element to generate.</param>
        /// <returns>Information about what was generated and saved.</returns>
        public GenerationResult GenerateCodeFor(Gum.DataTypes.ElementSave element, 
            CodeGenerationSavingBehavior savingBehavior = CodeGenerationSavingBehavior.AlwaysSave)
        {

            GenerationResult resultToReturn = new GenerationResult();

            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            string directoryToSave = GumRuntimesFolder;

            string generatedCode = mGueDerivingClassCodeGenerator.GenerateCodeFor(element);

            FilePath generatedSaveLocation = directoryToSave + element.Name + "Runtime.Generated.cs";

            if (savingBehavior == CodeGenerationSavingBehavior.AlwaysSave)
            {
                resultToReturn.DidSaveGenerated = true;
            }
            else // if(savingBehavior == CodeGenerationSavingBehavior.SaveIfGeneratedDiffers)
            {
                // We only want to save this file if what we've just generated is different than what is already on disk:
                if(!generatedSaveLocation.Exists())
                {
                    resultToReturn.DidSaveGenerated = true;
                }
                else
                {
                    var existingText = File.ReadAllText(generatedSaveLocation.FullPath);

                    resultToReturn.DidSaveGenerated = existingText != generatedCode;
                }
            }

            string customCodeSaveLocation = directoryToSave + element.Name + "Runtime.cs";
            // If it doesn't exist, overwrite it. If it does exist, don't overwrite it - we might lose
            // custom code.
            if (!System.IO.File.Exists(customCodeSaveLocation) && 
                // Standard elements don't have CustomInit  
                (element is StandardElementSave) == false)
            {
                resultToReturn.DidSaveCustom = true;
            }

            if(string.IsNullOrEmpty(generatedCode))
            {
                resultToReturn.DidSaveCustom = false;
                resultToReturn.DidSaveGenerated = false;
            }

            if (resultToReturn.DidSaveGenerated)
            {
                // in case directory doesn't exist
                var directory = generatedSaveLocation.GetDirectoryContainingThis();

                System.IO.Directory.CreateDirectory(directory.FullPath);

                GlueCommands.Self.TryMultipleTimes(() => 
                    System.IO.File.WriteAllText(generatedSaveLocation.FullPath, generatedCode));
            }

            if(resultToReturn.DidSaveCustom)
            {
                var customCode = CustomCodeGenerator.Self.GetCustomCodeTemplateCode(element);

                GlueCommands.Self.TryMultipleTimes(() =>
                    System.IO.File.WriteAllText(customCodeSaveLocation, customCode));
            }

            return resultToReturn;
        }

        public bool GenerateAndSaveRuntimeAssociations()
        {
            bool wasAdded = false;

            var shouldGenerateComponentsToFormsAssociation = false;
            var rfs = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.GetAllReferencedFiles()
                .FirstOrDefault(item => FlatRedBall.IO.FileManager.GetExtension(item.Name) == "gumx");

            if(rfs != null)
            {
                shouldGenerateComponentsToFormsAssociation = rfs.Properties.GetValue<bool>("IncludeComponentToFormsAssociation");   
                    //.SetValue(
                    //nameof(IncludeComponentToFormsAssociation), value);
            }

            string contents = GueRuntimeTypeAssociationGenerator.Self.GetRuntimeRegistrationPartialClassContents(shouldGenerateComponentsToFormsAssociation);

            string whereToSave = GumRuntimesFolder + "GumIdb.Generated.cs";

            GlueCommands.Self.TryMultipleTimes(() =>
                System.IO.File.WriteAllText(whereToSave, contents));

            wasAdded |=
                FlatRedBall.Glue.ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(
                FlatRedBall.Glue.ProjectManager.ProjectBase, whereToSave);

            return wasAdded;
        }

        public void CreateElementComponentCodeGenerators()
        {
            mIWindowCodeGenerator = new CodeGeneration.IWindowCodeGenerator();
            FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Add(mIWindowCodeGenerator);

            mGumPluginCodeGenerator = new GumPluginCodeGenerator();
            FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Add(mGumPluginCodeGenerator);

            mGumLayerCodeGenerator = new GumLayerCodeGenerator();
            FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Add(mGumLayerCodeGenerator);

            gumLayerAssociationCodeGenerator = new GumLayerAssociationCodeGenerator();
            FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Add(gumLayerAssociationCodeGenerator);

            gumToFlatRedBallAttachmentCodeGenerator = new GumToFlatRedBallAttachmentCodeGenerator();
            FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Add(gumToFlatRedBallAttachmentCodeGenerator);
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

            if (FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Contains(gumLayerAssociationCodeGenerator))
            {
                FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Remove(gumLayerAssociationCodeGenerator);
            }

            if (FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Contains(gumToFlatRedBallAttachmentCodeGenerator))
            {
                FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Remove(gumToFlatRedBallAttachmentCodeGenerator);
            }

            
        }

        public void GenerateAllBehaviors()
        {
            var gumProject = Gum.Managers.ObjectFinder.Self.GumProjectSave;

            if(gumProject?.Behaviors != null)
            {
                foreach (var behavior in  gumProject.Behaviors)
                {
                    GenerateCodeFor(behavior);
                }
            }
        }

        private void GenerateCodeFor(BehaviorSave behavior)
        {
            string directoryToSave = GumBehaviorsFolder;

            string generatedCode = behaviorCodeGenerator.GenerateInterfaceCodeFor(behavior);

            string saveLocation = directoryToSave + "I" + behavior.Name + ".Generated.cs";

            System.IO.Directory.CreateDirectory(directoryToSave);

            bool didSave = false;

            if(!string.IsNullOrEmpty(generatedCode))
            {
                try
                {
                    GlueCommands.Self.TryMultipleTimes(() =>
                        System.IO.File.WriteAllText(saveLocation, generatedCode));
                    didSave = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to generate:\n" + e);
                }
            }

            if(didSave)
            {
                // add the file to the project:
                FlatRedBall.Glue.ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(
                    FlatRedBall.Glue.ProjectManager.ProjectBase, saveLocation);

            }
        }

        private bool TrySaveMultipleTimes(string fileName, string fileContents)
        {
            bool wasSaved = false;

            var directory = FileManager.GetDirectory(fileName);
            Directory.CreateDirectory(directory);
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
