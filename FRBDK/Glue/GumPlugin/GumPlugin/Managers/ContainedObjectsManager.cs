using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPlugin.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.Managers
{
    public class ContainedObjectsManager : Singleton<ContainedObjectsManager>
    {
        public bool HandleTryAddContainedObjects(string absoluteFile, List<string> availableObjects)
        {
            string extension = FileManager.GetExtension(absoluteFile);
            bool isEitherScreenOrComponent = extension == GumProjectSave.ComponentExtension ||
                extension == GumProjectSave.ScreenExtension;

            // We don't want the "Entire File" option
            // Victor Chelaru Feb 24
            // Why not? This clears out
            // all entire objects! Like if
            // the user selects a .scnx file.
            // This is bad, so we should not clear
            // this unless the file is a .gumx or .gusx
            // or .gucx or .gutx
            // Update March 11, 2015
            // Actually we're no longer
            // going to use the weird "this"
            // syntax and instead just use the
            // entire file syntax.
            //if (extension == GumProjectSave.ScreenExtension ||
            //    extension == GumProjectSave.ComponentExtension ||
            //    extension == GumProjectSave.StandardExtension ||
            //    extension == GumProjectSave.ProjectExtension)
            //{
            //    availableObjects.Clear();
            //}
            // Update March 12, 2015
            // Actually we do want to clear everything if it's a .gucx, and only add the Entire File with the runtime type
            if(extension == GumProjectSave.ComponentExtension)
            {
                availableObjects.Clear();

            }

            ElementSave element = null;
            if (isEitherScreenOrComponent && System.IO.File.Exists(absoluteFile))
            {
                try
                {
                    if (extension == GumProjectSave.ComponentExtension)
                    {
                        element =
                            FileManager.XmlDeserialize<ComponentSave>(absoluteFile);
                    }
                    else
                    {
                        element =
                            FileManager.XmlDeserialize<Gum.DataTypes.ScreenSave>(absoluteFile);
                    }
                }
                catch (Exception ex)
                {
                    GlueCommands.Self.PrintOutput("Error trying to load element {} for filling available contained objects: \n" + ex.ToString());
                }
            }

            if(element != null)
            { 
                // Victor Chelaru, November 1, 2015
                // Initially I used a "this" syntax to
                // get access to the Screen casted as its
                // current type. But I didn't like the "this" 
                // syntax, so I removed it to replace it with "Entire File".
                // Unfortunately, Entire File does a simple assignment, but screens
                // are loaded as IDB's, not as their runtime type. Maybe this should change
                // in the future (which would require their runtime types to inherit from IDBs),
                // but in the meantime, I'm going to revert back to using the "this" syntax so that
                // Screens can be casted appropriately:
                //availableObjects.Add("Entire File (" + element.Name + "Runtime" + ")");
                // only add this if it's an IDB:
                var rfs = GlueState.Self.CurrentReferencedFileSave;
                if(GlueState.Self.CurrentNamedObjectSave != null && rfs == null)
                {
                    var nos = GlueState.Self.CurrentNamedObjectSave;

                    if(nos.SourceType == SourceType.File)
                    {
                        var glueElement = ObjectFinder.Self.GetElementContaining(nos);
                        rfs = glueElement?.GetReferencedFileSave(nos.SourceFile);
                    }
                }

                // August 10, 2022
                // This code was originally written assuming
                // that an object is selected and its combo box
                // is being filled. Now this method is used for other
                // situations, like determining if an object's selected
                // SourceName is contained in the file. The file should expose
                // all of its SourceNames regardless of the current object, so it
                // seems weird that the logic here uses the rfs to determine what to
                // display.
                // Therefore, we will use the RFS if it exists, otherwise we'll show everything
                var rfsAti = rfs?.GetAssetTypeInfo();
                if ( rfsAti == AssetTypeInfoManager.Self.ScreenIdbAti  || rfsAti == null)
                {
                    availableObjects.Add("this (" + 
                        GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(element) + ")");
                }
                if(rfsAti != null)
                {
                    availableObjects.Add(
                        $"Entire File ({rfsAti.RuntimeTypeName})");
                }


                foreach (var instance in element.Instances)
                {
                    var elementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(instance.BaseType);

                    string gueType = "";

                    if (GueDerivingClassCodeGenerator.Self.ShouldGenerateRuntimeFor(elementSave))
                    {
                        gueType = GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(instance, element);
                    }
                    else
                    {
                        gueType = "Gum.Wireframe.GraphicalUiElement";
                    }
                    availableObjects.Add(instance.Name + " (" + gueType + ")");
                }
            }

            return isEitherScreenOrComponent;
        }
    }
}
