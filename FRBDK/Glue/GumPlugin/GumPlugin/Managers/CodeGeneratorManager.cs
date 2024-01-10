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
using GumPluginCore.CodeGeneration;
using FlatRedBall.Glue.Plugins;

namespace GumPlugin.Managers;

#region Structs/Enums
public struct GenerationResult
{
    public bool DidSaveGeneratedGumRuntime;
    public bool DidSaveCustomGumRuntime;

    public bool DidSaveGeneratedForms;
    public bool DidSaveCustomForms;
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
    FormsObjectCodeGenerator formsObjectCodeGenerator;
    GueDerivingClassCodeGenerator mGueDerivingClassCodeGenerator;
    GumLayerCodeGenerator mGumLayerCodeGenerator;
    GumLayerAssociationCodeGenerator gumLayerAssociationCodeGenerator;
    GumCollidableCodeGenerator gumCollidableCodeGenerator;
    public BehaviorCodeGenerator BehaviorCodeGenerator;
    GumToFlatRedBallAttachmentCodeGenerator gumToFlatRedBallAttachmentCodeGenerator;

    #endregion

    FilePath GumRuntimesFolder => AppState.Self.GlueProjectFolder + @"GumRuntimes\"; 
    FilePath FormsFolder => AppState.Self.GlueProjectFolder + @"Forms\";

    public FilePath CustomRuntimeCodeLocationFor(ElementSave gumElement) => GumRuntimesFolder + gumElement.Name + "Runtime.cs";
    public FilePath GeneratedRuntimeCodeLocationFor(ElementSave gumElement) =>
        GumRuntimesFolder + gumElement.Name + "Runtime.Generated.cs";
    public FilePath CustomFormsCodeLocationFor(ElementSave gumElement)
    {
        var subfolder = gumElement is Gum.DataTypes.ScreenSave ? "Screens/"
        : gumElement is ComponentSave ? "Components/"
        : "Standard/";
        return FormsFolder + subfolder + gumElement.Name + "Forms.cs";
    }

    public FilePath GeneratedFormsCodeLocationFor(ElementSave gumElement)
    {
        var subfolder = gumElement is Gum.DataTypes.ScreenSave ? "Screens/"
        : gumElement is ComponentSave ? "Components/"
        : "Standard/";

        return FormsFolder + subfolder + gumElement.Name + "Forms.Generated.cs";
    }

    FilePath GumBehaviorsFolder => GumRuntimesFolder + @"Behaviors\";

    public CodeGeneratorManager()
    {
        mGueDerivingClassCodeGenerator = new GueDerivingClassCodeGenerator();
        BehaviorCodeGenerator = new BehaviorCodeGenerator();
    }

    public void GenerateDueToFileChange(FilePath file)
    {
        string extension = file.Extension;
        if(extension == "gumx")
        {
            foreach (var screen in ObjectFinder.Self.GumProjectSave.Screens)
            {
                GenerateDueToFileChangeTask(screen, saveProjects:false);
            }
            foreach (var component in ObjectFinder.Self.GumProjectSave.Components)
            {
                GenerateDueToFileChangeTask(component, saveProjects: false);
            }
            foreach (var standard in ObjectFinder.Self.GumProjectSave.StandardElements)
            {
                GenerateDueToFileChangeTask(standard, saveProjects: false);
            }
            GlueCommands.Self.ProjectCommands.MakeGeneratedCodeItemsNested();
            GlueCommands.Self.ProjectCommands.SaveProjects();
        }
        else
        {
            var changedElement = GetElementFromFile(file);

            // Maybe the element doesn't exist - like it's a .gucx that is not part of the .gumx
            if(changedElement != null)
            {
                GenerateDueToFileChangeTask(changedElement, saveProjects: false);

                GlueCommands.Self.ProjectCommands.MakeGeneratedCodeItemsNested();
                GlueCommands.Self.ProjectCommands.SaveProjects();

            }

        }
    }

    public void GenerateDueToFileChangeTask(ElementSave element, bool saveProjects)
    {
        if(AppState.Self.GumProjectSave != null)
        {
            TaskManager.Self.Add(() => GenerateDueToFileChange(element, saveProjects),
                $"Generating Gum {element}", TaskExecutionPreference.AddOrMoveToEnd);
        }
    }

    private void GenerateDueToFileChange(ElementSave changedElement, bool saveProjects)
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

        var generationResult = GenerateCodeFor(changedElement, saveProjects);

        if (generationResult.DidSaveGeneratedGumRuntime)
        {
            var whatContainsThisElement = ObjectFinder.Self.GetElementsReferencing(changedElement);

            foreach (var container in whatContainsThisElement)
            {
                GenerateDueToFileChangeTask(container, saveProjects);
            }

            if(changedElement is Gum.DataTypes.ScreenSave)
            {
                foreach(var screenSave in ObjectFinder.Self.GumProjectSave.Screens)
                {
                    if(screenSave != changedElement && screenSave.IsOfType(changedElement.Name))
                    {
                        GenerateDueToFileChangeTask(screenSave, saveProjects);
                    }
                }
            }


            if (changedElement is ComponentSave)
            {
                foreach (var component in ObjectFinder.Self.GumProjectSave.Components)
                {
                    if (component != changedElement && component.IsOfType(changedElement.Name))
                    {
                        GenerateDueToFileChangeTask(component, saveProjects);
                    }
                }
            }
        }

        GenerateAndSaveRuntimeAssociations();
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

        ///////////////////Early Out//////////////////////////
        if(gumProject == null)
        {
            return null;
        }
        /////////////////End Early Out////////////////////////

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

    public async Task GenerateDerivedGueRuntimesAsync(bool forceReload = false)
    {
        await TaskManager.Self.AddAsync(() =>
        {
            if ((forceReload || AppState.Self.GumProjectSave == null) &&
                FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject != null)
            {
                var rfs = FlatRedBall.Glue.Elements.ObjectFinder.Self.GlueProject.GetAllReferencedFiles()
                    .FirstOrDefault(item => FlatRedBall.IO.FileManager.GetExtension(item.Name) == "gumx");

                if (rfs != null)
                {
                    string fullFileName = GlueState.Self.ContentDirectory + rfs.Name;

                    string gumXDirectory = FlatRedBall.IO.FileManager.GetDirectory(fullFileName);

                    FileReferenceTracker.Self.LoadGumxIfNecessaryFromDirectory(gumXDirectory, forceReload);
                }
            }

            if (Gum.Managers.ObjectFinder.Self.GumProjectSave != null)
            {
                var directoryToSave = GumRuntimesFolder;

                System.IO.Directory.CreateDirectory(directoryToSave.FullPath);

                GenerateAndSaveRuntimeAssociations();

                GenerateAllElements(directoryToSave);
            }

        }, "Generating all Gum runtimes code");
    }

    private void GenerateAllElements(FilePath directoryToSave)
    {
        var elements = AppState.Self.AllLoadedElements.ToList();

        var errors = new List<string>();
        var obj = new object();


        foreach (var element in elements)
        //Parallel.ForEach(elements, (element) =>
        {
            try
            {
                GenerateDueToFileChangeTask(element, saveProjects:false);
            }
            catch (Exception e)
            {
                lock (obj)
                {
                    errors.Add(e.ToString());
                }
            }
        }
        //);

        foreach (var err in errors)
        {
            GlueCommands.Self.PrintError(err);
        }

        GlueCommands.Self.ProjectCommands.MakeGeneratedCodeItemsNested();
        GlueCommands.Self.ProjectCommands.SaveProjects();

    }
    /// <summary>
    /// Generates and saves the code for the passed Gum ElementSave (both generated and custom code template), 
    /// but does not add the resulting .cs files to the VisualStudio project.
    /// </summary>
    /// <param name="element">The element to generate.</param>
    /// <returns>Information about what was generated and saved.</returns>
    public GenerationResult GenerateCodeFor(Gum.DataTypes.ElementSave element, bool saveProjects)
    {
        GenerationResult resultToReturn = new GenerationResult();

        ///////////////early out////////////////
        var gumRuntimesFolder = GumRuntimesFolder;
        if(gumRuntimesFolder == null)
        {
            return resultToReturn;
        }
        /////////end early out///////////////



        var subfolder = element is Gum.DataTypes.ScreenSave ? "Screens/"
            : element is ComponentSave ? "Components/"
            : "Standard/";

        CodeGenerationSavingBehavior savingBehavior = CodeGenerationSavingBehavior.SaveIfGeneratedDiffers;

        if (element == null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        var shouldSaveProject = false;

        // Do generated first to see if it fails. If it does (if the return values are null), then don't bother
        // creating the custom code files.
        string generatedGumRuntimeCode = mGueDerivingClassCodeGenerator.GenerateCodeFor(element);

        var shouldGeneratedFormsBeInProject = false;
        string generatedFormsCode = FormsClassCodeGenerator.Self.GenerateCodeFor(element);


        // Do custom before generated so the generated.cs file can be generated under custom
        #region Custom Gum Runtime

        bool shouldCustomGumBeInProject = false;

        string customGumRuntimeSaveLocation = CustomRuntimeCodeLocationFor(element).FullPath;
        if(string.IsNullOrEmpty(generatedGumRuntimeCode))
        {
            resultToReturn.DidSaveCustomGumRuntime = false;
            shouldCustomGumBeInProject = false;
        }
        // If it doesn't exist, overwrite it. If it does exist, don't overwrite it - we might lose
        // custom code.
        else if (!System.IO.File.Exists(customGumRuntimeSaveLocation) && 
            // Standard elements don't have CustomInit  
            (element is StandardElementSave) == false)
        {
            resultToReturn.DidSaveCustomGumRuntime = true;
            shouldCustomGumBeInProject = true;
        }
        if (resultToReturn.DidSaveCustomGumRuntime)
        {
            shouldCustomGumBeInProject = true;
            var customCode = CustomCodeGenerator.Self.GetCustomGumRuntimeCustomCode(element);

            var directory = FileManager.GetDirectory(customGumRuntimeSaveLocation);
            System.IO.Directory.CreateDirectory(directory);

            GlueCommands.Self.TryMultipleTimes(() =>
                System.IO.File.WriteAllText(customGumRuntimeSaveLocation, customCode));
        }

        if(shouldCustomGumBeInProject)
        { 
            bool wasAnythingAdded =
                FlatRedBall.Glue.ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(
                GlueState.Self.CurrentMainProject, customGumRuntimeSaveLocation);
            if (wasAnythingAdded)
            {
                shouldSaveProject = true;
            }
        }
        #endregion

        #region Custom Forms

        // Check if generated code is created. If not, don't create a custom.
        // Generated may not be created if this maps to a standard Forms object
        // like Button.
        if (shouldGeneratedFormsBeInProject)
        { 
            string customFormsSaveLocation = CustomFormsCodeLocationFor(element).FullPath; 
            var customFormsCode = CustomCodeGenerator.Self.GetCustomFormsCodeTemplateCode(element);

            if(string.IsNullOrEmpty(customFormsCode))
            {
                resultToReturn.DidSaveCustomForms = false;
            }
            else if(!System.IO.File.Exists(customFormsSaveLocation))
            {
                resultToReturn.DidSaveCustomForms = true;
            }

            if (resultToReturn.DidSaveCustomForms)
            {

                var directory = FileManager.GetDirectory(customFormsSaveLocation);
                System.IO.Directory.CreateDirectory(directory);

                GlueCommands.Self.TryMultipleTimes(() =>
                    System.IO.File.WriteAllText(customFormsSaveLocation, customFormsCode));
            
                bool wasAnythingAdded =
                    FlatRedBall.Glue.ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(
                    GlueState.Self.CurrentMainProject, customFormsSaveLocation);
                if (wasAnythingAdded)
                {
                    shouldSaveProject = true;
                }
            }
        }

        #endregion

        #region Generated Gum Runtime

        bool shouldGeneratedGumBeInProject = false;


        FilePath generatedSaveLocation = GeneratedRuntimeCodeLocationFor(element);

        if(string.IsNullOrEmpty(generatedGumRuntimeCode))
        {
            resultToReturn.DidSaveGeneratedGumRuntime = false;
            shouldGeneratedGumBeInProject = false;
        }
        else if (savingBehavior == CodeGenerationSavingBehavior.AlwaysSave)
        {
            resultToReturn.DidSaveGeneratedGumRuntime = true;
            shouldGeneratedGumBeInProject = true;
        }
        else // if(savingBehavior == CodeGenerationSavingBehavior.SaveIfGeneratedDiffers)
        {
            shouldGeneratedGumBeInProject = true;
            // We only want to save this file if what we've just generated is different than what is already on disk:
            if (!generatedSaveLocation.Exists())
            {
                resultToReturn.DidSaveGeneratedGumRuntime = true;
            }
            else
            {
                string existingText = null;
                try
                {
                    existingText = File.ReadAllText(generatedSaveLocation.FullPath);
                }
                catch
                {
                    // no biggie, we'll just re-generate it
                }

                resultToReturn.DidSaveGeneratedGumRuntime = existingText != generatedGumRuntimeCode;
            }
        }
        if (resultToReturn.DidSaveGeneratedGumRuntime)
        {
            // in case directory doesn't exist
            System.IO.Directory.CreateDirectory(generatedSaveLocation.GetDirectoryContainingThis().FullPath);

            GlueCommands.Self.TryMultipleTimes(() => 
                System.IO.File.WriteAllText(generatedSaveLocation.FullPath, generatedGumRuntimeCode));
        }

        if(shouldGeneratedGumBeInProject)
        {
            bool wasAnythingAdded =
                FlatRedBall.Glue.ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(
                GlueState.Self.CurrentMainProject, generatedSaveLocation.FullPath);

            if (wasAnythingAdded)
            {
                shouldSaveProject = true;
            }
        }

        #endregion

        #region Generated Forms

        FilePath generatedFormsSaveLocation = GeneratedFormsCodeLocationFor(element);

        if(string.IsNullOrEmpty(generatedFormsCode))
        {
            resultToReturn.DidSaveGeneratedForms = false;
            shouldGeneratedFormsBeInProject = false;
        }
        else if(savingBehavior == CodeGenerationSavingBehavior.AlwaysSave)
        {
            resultToReturn.DidSaveGeneratedForms = true;
            shouldGeneratedFormsBeInProject = true;
        }
        else
        {
            shouldGeneratedFormsBeInProject = true;
            if (!generatedFormsSaveLocation.Exists())
            {
                resultToReturn.DidSaveGeneratedForms = true;
            }
            else
            {
                var existingText = File.ReadAllText(generatedFormsSaveLocation.FullPath);

                resultToReturn.DidSaveGeneratedForms = existingText != generatedFormsCode;
            }
        }

        if(resultToReturn.DidSaveGeneratedForms)
        {
            // in case it doesn't exist
            System.IO.Directory.CreateDirectory(generatedFormsSaveLocation.GetDirectoryContainingThis().FullPath);

            GlueCommands.Self.TryMultipleTimes(() =>
                System.IO.File.WriteAllText(generatedFormsSaveLocation.FullPath, generatedFormsCode));
        }

        if(shouldGeneratedFormsBeInProject)
        {
            bool wasAnythingAdded =
                FlatRedBall.Glue.ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(
                GlueState.Self.CurrentMainProject, generatedFormsSaveLocation.FullPath);

            if(wasAnythingAdded)
            {
                shouldSaveProject = true;
            }
        }

        #endregion

        if (shouldSaveProject && saveProjects)
        {
            // November 21, 2023 - this isnt working but I'm not sure why - will investigate some time in the future.
            // The Forms generated file wasn't embedded.
            GlueCommands.Self.ProjectCommands.MakeGeneratedCodeItemsNested();
            GlueCommands.Self.ProjectCommands.SaveProjects();
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
            GlueState.Self.CurrentMainProject, whereToSave);

        if(wasAdded)
        {
            GlueCommands.Self.ProjectCommands.SaveProjects();
        }

        return wasAdded;
    }

    public void CreateCodeGenerators(PluginBase plugin)
    {
        mIWindowCodeGenerator = new CodeGeneration.IWindowCodeGenerator();
        plugin.RegisterCodeGenerator(mIWindowCodeGenerator);

        mGumPluginCodeGenerator = new GumPluginCodeGenerator();
        plugin.RegisterCodeGenerator(mGumPluginCodeGenerator);

        formsObjectCodeGenerator = new FormsObjectCodeGenerator();
        plugin.RegisterCodeGenerator(formsObjectCodeGenerator);

        mGumLayerCodeGenerator = new GumLayerCodeGenerator();
        plugin.RegisterCodeGenerator(mGumLayerCodeGenerator);

        gumLayerAssociationCodeGenerator = new GumLayerAssociationCodeGenerator();
        plugin.RegisterCodeGenerator(gumLayerAssociationCodeGenerator);

        gumCollidableCodeGenerator = new GumCollidableCodeGenerator();
        plugin.RegisterCodeGenerator(gumCollidableCodeGenerator);

        gumToFlatRedBallAttachmentCodeGenerator = new GumToFlatRedBallAttachmentCodeGenerator();
        plugin.RegisterCodeGenerator(gumToFlatRedBallAttachmentCodeGenerator);

        plugin.RegisterCodeGenerator(new GumGame1CodeGeneratorEarly());
        plugin.RegisterCodeGenerator(new GumGame1CodeGenerator());

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

        if (FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Contains(formsObjectCodeGenerator))
        {
            FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Remove(formsObjectCodeGenerator);
        }

        if (FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Contains(mGumLayerCodeGenerator))
        {
            FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Remove(mGumLayerCodeGenerator);
        }

        if (FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Contains(gumLayerAssociationCodeGenerator))
        {
            FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Remove(gumLayerAssociationCodeGenerator);
        }

        if (FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Contains(gumCollidableCodeGenerator))
        {
            FlatRedBall.Glue.Parsing.CodeWriter.CodeGenerators.Remove(gumCollidableCodeGenerator);
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
                GenerateCodeFor(behavior, saveProjects:false);
            }
        }

        GlueCommands.Self.ProjectCommands.SaveProjects();
    }

    private void GenerateCodeFor(BehaviorSave behavior, bool saveProjects)
    {
        var directoryToSave = GumBehaviorsFolder;

        string generatedCode = BehaviorCodeGenerator.GenerateInterfaceCodeFor(behavior);

        FilePath saveLocation = directoryToSave + "I" + behavior.Name + ".Generated.cs";

        System.IO.Directory.CreateDirectory(saveLocation.GetDirectoryContainingThis().FullPath);

        var shouldAddToProject = false;

        if(!string.IsNullOrEmpty(generatedCode))
        {
            shouldAddToProject = true;
            try
            {
                GlueCommands.Self.TryMultipleTimes(() =>
                    System.IO.File.WriteAllText(saveLocation.FullPath, generatedCode));
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to generate:\n" + e);
            }
        }

        if(shouldAddToProject)
        {
            // add the file to the project:
            var didAdd = FlatRedBall.Glue.ProjectManager.CodeProjectHelper.AddFileToCodeProjectIfNotAlreadyAdded(
                GlueState.Self.CurrentMainProject, saveLocation.FullPath);

            if(didAdd && saveProjects)
            {
                GlueCommands.Self.ProjectCommands.SaveProjects();
            }
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
