using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin.DataGeneration
{
    public static class FormsControlAdder
    {
        static List<string> ContentItems = new List<string>
        {
            "UISpriteSheet.png"
        };

        public static string EmbeddedProjectRoot { get; set; } = "GumPluginCore.Embedded.EmbeddedObjectGumProject";

        public static async Task SaveComponents(Assembly assembly)
        {
            var gumDirectory = FileManager.GetDirectory(
                GumProjectManager.Self.GetGumProjectFileName());
            var componentDestination = gumDirectory + @"Components\";


            Dictionary<string, FilePath> resourceToFileDestinations = new Dictionary<string, FilePath>();

            var resourcesInAssembly = assembly.GetManifestResourceNames();

            string defaultFormsPrefix = EmbeddedProjectRoot + ".Components.";
            foreach(var resourceSource in resourcesInAssembly)
            {
                var isFormsComponent = resourceSource.StartsWith(defaultFormsPrefix) && resourceSource.EndsWith(".gucx");

                if(isFormsComponent)
                {
                    var noPrefixName = resourceSource.Substring(defaultFormsPrefix.Length);

                    var withoutExtension = noPrefixName.Substring(0, noPrefixName.Length - ".gucx".Length);

                    var split = withoutExtension.Split('.');

                    var destination = componentDestination + String.Join('/', split) + ".gucx";

                    resourceToFileDestinations[resourceSource] = destination;
                }
            }

            var contentDestination = gumDirectory;

            foreach(var file in ContentItems)
            {
                var resourceName = EmbeddedProjectRoot + "/" + file;
                resourceToFileDestinations[resourceName.Replace("/", ".")] = contentDestination + file;
            }

            var existingFiles = resourceToFileDestinations.Values.Where(item => item.Exists()).ToArray();

            if(existingFiles.Length > 0)
            {
                var message = "The following files will be overwritten:";

                foreach(var item in existingFiles)
                {
                    message += "\n" + item.RelativeTo(GlueState.Self.CurrentGlueProjectDirectory);
                }

                message += "\n\nSave Anyway?";

                var result = System.Windows.Forms.MessageBox.Show(message,
                    "Overwrite?",
                    System.Windows.Forms.MessageBoxButtons.YesNo);

                var shouldSave = result == System.Windows.Forms.DialogResult.Yes;

                if(shouldSave)
                {
                    foreach(var kvp in resourceToFileDestinations)
                    {
                        try
                        {
                            FileManager.SaveEmbeddedResource(assembly, kvp.Key, kvp.Value.FullPath);
                        }
                        catch (Exception e)
                        {
                            GlueCommands.Self.PrintError($"Could not add component {kvp.Key}:\n{e}");
                        }
                    }
                }
            }

            var wasAnythingAdded = false;
            // Now that everything is on disk, add the files to the Gum project if necessary
            await TaskManager.Self.AddAsync(() =>
            {
                foreach(var file in resourceToFileDestinations.Values)
                {
                    if(file.Extension == "gucx")
                    {
                        var isComponentAlreadyPartOfProject =
                            GumPluginCommands.Self.IsComponentFileReferenced(file.FullPath);

                        if(!isComponentAlreadyPartOfProject && file.Exists())
                        {
                            GumPluginCommands.Self.AddComponent(file.FullPath);
                            wasAnythingAdded = true;
                        }

                    }
                }

                UpdateTextStateCategory();

            }, "Updating Gum project with Forms Components");

            if(wasAnythingAdded)
            {
                await GumPluginCommands.Self.SaveGumxAsync(saveAllElements: false);
            }

        }

        public static async Task SaveBehaviors(Assembly assembly)
        {
            var names = assembly.GetManifestResourceNames();

            var gumDirectory = FileManager.GetDirectory(
                GumProjectManager.Self.GetGumProjectFileName());

            var behaviorDestination = gumDirectory + "Behaviors\\";

            var prefix = EmbeddedProjectRoot + ".Behaviors.";

            foreach(var resource in names)
            {
                var isBehavior = resource.StartsWith(prefix) && resource.EndsWith(".behx");

                if(isBehavior)
                {
                    var noPath = resource.Substring(prefix.Length);

                    var destination = behaviorDestination + noPath;

                    FileManager.SaveEmbeddedResource(assembly, resource, destination);

                    var behaviorSave = FileManager.XmlDeserialize<BehaviorSave>(destination);

                    var project = AppState.Self.GumProjectSave;

                    project.Behaviors.RemoveAll(item => item.Name == behaviorSave.Name);
                    GumPluginCommands.Self.AddBehavior(behaviorSave);
                }
            }
            await GumPluginCommands.Self.SaveGumxAsync();
        }

        private static void UpdateTextStateCategory()
        {
            var textStandard = AppState.Self.GumProjectSave.StandardElements
                .FirstOrDefault(item => item.Name == "Text");

            if(textStandard != null)
            {
                var added = false;

                var category = textStandard
                    .Categories.FirstOrDefault(item => item.Name == "ColorCategory");

                if(category == null)
                {
                    category = new Gum.DataTypes.Variables.StateSaveCategory();
                    category.Name = "ColorCategory";
                    textStandard.Categories.Add(category);

                    added = true;
                }

                var grayState = category.States
                    .FirstOrDefault(item => item.Name == "Gray");

                if(grayState == null)
                {
                    grayState = new Gum.DataTypes.Variables.StateSave();
                    grayState.Name = "Gray";
                    category.States.Add(grayState);

                    grayState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
                    {
                        Type = "int",
                        Name = "Blue",
                        Value = 208,
                        Category = "Rendering",
                        SetsValue = true
                    });

                    grayState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
                    {
                        Type = "int",
                        Name = "Green",
                        Value = 208,
                        Category = "Rendering",
                        SetsValue = true
                    });

                    grayState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
                    {
                        Type = "int",
                        Name = "Red",
                        Value = 208,
                        Category = "Rendering",
                        SetsValue = true
                    });

                    grayState.Initialize();

                    added = true;
                }

                var blackState = category.States
                    .FirstOrDefault(item => item.Name == "Black");

                if(blackState == null)
                {
                    blackState = new Gum.DataTypes.Variables.StateSave();
                    blackState.Name = "Black";
                    category.States.Add(blackState);

                    blackState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
                    {
                        Type = "int",
                        Name = "Blue",
                        Value = 49,
                        Category = "Rendering",
                        SetsValue = true
                    });

                    blackState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
                    {
                        Type = "int",
                        Name = "Green",
                        Value = 49,
                        Category = "Rendering",
                        SetsValue = true
                    });

                    blackState.Variables.Add(new Gum.DataTypes.Variables.VariableSave
                    {
                        Type = "int",
                        Name = "Red",
                        Value = 49,
                        Category = "Rendering",
                        SetsValue = true
                    });

                    blackState.Initialize();

                    added = true;
                }

                if(added)
                {
                    foreach(var state in textStandard.AllStates)
                    {
                        state.ConvertEnumerationValuesToInts();
                    }
                    // This won't work in Core because it uses enum values
                    GumPluginCommands.Self.SaveStandardElement(textStandard);
                    foreach (var state in textStandard.AllStates)
                    {
                        state.FixEnumerations();
                    }
                }
            }
        }
    }
}
