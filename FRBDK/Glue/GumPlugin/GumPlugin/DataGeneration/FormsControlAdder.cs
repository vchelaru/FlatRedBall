using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
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

        public static void SaveComponents(Assembly assembly)
        {
            var names = assembly.GetManifestResourceNames();


            var gumDirectory = FileManager.GetDirectory(
                GumProjectManager.Self.GetGumProjectFileName());
            var componentDestination = gumDirectory + @"Components\DefaultForms\";

            var componentFiles = FormsControlInfo.AllControls.Select(item => item.ComponentFile)
                .Where(item => !string.IsNullOrEmpty(item))
                .ToArray();

            var addedFileDestinations = new List<FilePath>();

            var resourcesInAssembly = assembly.GetManifestResourceNames();

            foreach(var resource in resourcesInAssembly)
            {
                const string defaultFormsPrefix = "GumPluginCore.Embedded.EmbeddedObjectGumProject.Components.DefaultForms.";
                var isFormsComponent = resource.StartsWith(defaultFormsPrefix) && resource.EndsWith(".gucx");

                if(isFormsComponent)
                {
                    var noPrefixName = resource.Substring(defaultFormsPrefix.Length);

                    var destination = componentDestination + noPrefixName;

                    addedFileDestinations.Add(destination);

                    var shouldSave = true;

                    int m = 3;

                    if (System.IO.File.Exists(destination))
                    {
                        var result = System.Windows.Forms.MessageBox.Show($"The file {destination} already exists. Save anyway?",
                            "Overwrite?",
                            System.Windows.Forms.MessageBoxButtons.YesNo);

                        shouldSave = result == System.Windows.Forms.DialogResult.Yes;
                    }

                    if (shouldSave)
                    {
                        try
                        {
                            FileManager.SaveEmbeddedResource(assembly, resource, destination);
                        }
                        catch (Exception e)
                        {
                            GlueCommands.Self.PrintError($"Could not add component {resource}:\n{e}");
                        }
                    }
                }
            }

            var contentDestination = gumDirectory;

            foreach(var file in ContentItems)
            {
                var resourceName = "GumPluginCore/Embedded/EmbeddedObjectGumProject/" + file;

                try
                {
                    FileManager.SaveEmbeddedResource(assembly, resourceName.Replace("/", "."), contentDestination + file);
                }
                catch(Exception e)
                {
                    GlueCommands.Self.PrintOutput($"Skipping adding file {file}:\n{e.ToString()}");
                }
            }

            // Now that everything is on disk, add the files to the Gum project if necessary
            TaskManager.Self.Add(() =>
            {
                var wasAnythingAdded = false;

                foreach(var file in addedFileDestinations)
                {
                    if(file.Extension == "gucx")
                    {
                        var isComponentAlreadyPartOfProject =
                            AppCommands.Self.IsComponentFileReferenced(file.FullPath);

                        if(!isComponentAlreadyPartOfProject && file.Exists())
                        {
                            AppCommands.Self.AddComponent(file.FullPath);
                            wasAnythingAdded = true;
                        }

                    }
                }

                UpdateTextStateCategory();

                if(wasAnythingAdded)
                {
                    AppCommands.Self.SaveGumx(saveAllElements: false);
                }
            }, "Updating Gum project with Forms Components");


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
                    AppCommands.Self.SaveStandardElement(textStandard);
                    foreach (var state in textStandard.AllStates)
                    {
                        state.FixEnumerations();
                    }
                }
            }
        }
    }
}
