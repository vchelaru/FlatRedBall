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
        static List<string> ComponentFiles = new List<string>
        {
            "Button",
            "CheckBox",
            "ColoredFrame",
            "ComboBox",
            "ListBox",
            "ListBoxItem",
            "RadioButton",
            "ScrollBar",
            "ScrollBarThumb",
            "ScrollViewer",
            "Slider",
            "TextBox",
            "ToggleButton",
            "TreeView",
            "TreeViewItem",
            "TreeViewToggleButton",
            "UserControl"
        };

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


            foreach (var file in ComponentFiles)
            {
                // example:
                // "GumPlugin.Embedded.EmbeddedObjectGumProject.Components.DefaultFormsButton.gucx"
                var resourceName = "GumPlugin/Embedded/EmbeddedObjectGumProject/Components/DefaultForms/" + file + ".gucx";
                FileManager.SaveEmbeddedResource(assembly, resourceName.Replace("/", "."), componentDestination + file + ".gucx");

            }

            var contentDestination = gumDirectory;

            foreach(var file in ContentItems)
            {
                var resourceName = "GumPlugin/Embedded/EmbeddedObjectGumProject/" + file;

                FileManager.SaveEmbeddedResource(assembly, resourceName.Replace("/", "."), contentDestination + file);
            }

            // Now that everything is on disk, add the files to the Gum project if necessary
            TaskManager.Self.AddSync(() =>
            {
                var wasAnythingAdded = false;

                foreach(var component in ComponentFiles)
                {
                    var absoluteFile = new FilePath(componentDestination + component + ".gucx");

                    var isComponentAlreadyPartOfProject =
                        AppCommands.Self.IsComponentFileReferenced(absoluteFile);

                    if(!isComponentAlreadyPartOfProject)
                    {
                        AppCommands.Self.AddComponent(absoluteFile);
                        wasAnythingAdded = true;
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
                    AppCommands.Self.SaveStandardElement(textStandard);
                }
            }
        }
    }
}
