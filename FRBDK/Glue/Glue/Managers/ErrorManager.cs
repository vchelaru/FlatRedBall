using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Managers
{
    public class ErrorManager
    {
        public static void HandleCheckErrors()
        {

            /////////////////////////EARLY OUT/////////////////////////
            if (ProjectManager.GlueProjectSave == null)
            {
                return;
            }
            //////////////////////END EARLY OUT//////////////////////


            bool errorFound = false;

            // This gets confused when it finds generated files for things like Gum.  
            // I don't think anyone uses this functionality anymore so we should retire
            // it.
            //errorFound = CheckForMissingNonGeneratedPartials();

            #region Loop through all NamedObjectSaves and see if they are on any Layer that doens't really exist

            foreach (ScreenSave screenSave in ProjectManager.GlueProjectSave.Screens)
            {
                foreach (NamedObjectSave nos in screenSave.NamedObjects)
                {
                    if (!string.IsNullOrEmpty(nos.LayerOn))
                    {
                        NamedObjectSave layerNos = screenSave.GetNamedObjectRecursively(nos.LayerOn);

                        if (layerNos == null || layerNos.IsLayer == false)
                        {
                            MessageBox.Show("The object " + nos + " references the layer " + nos.LayerOn + ", but this Layer doesn't exist.");
                            errorFound = true;
                        }
                    }
                }
            }

            foreach (EntitySave entitySave in ProjectManager.GlueProjectSave.Entities)
            {
                foreach (NamedObjectSave nos in entitySave.NamedObjects)
                {
                    if (!string.IsNullOrEmpty(nos.LayerOn))
                    {
                        NamedObjectSave layerNos = entitySave.GetNamedObjectRecursively(nos.LayerOn);

                        if (layerNos == null || layerNos.IsLayer == false)
                        {
                            MessageBox.Show("The object " + nos + " references the layer " + nos.LayerOn + ", but this Layer doesn't exist.");
                            errorFound = true;
                        }
                    }
                }
            }




            #endregion

            #region Loop through all Tunneled variables to see if they reference a variable that really doesn't exist

            foreach (ScreenSave screenSave in ProjectManager.GlueProjectSave.Screens)
            {
                errorFound |= CheckForMissingTunnelReferences(screenSave);
            }

            foreach (EntitySave entitySave in ProjectManager.GlueProjectSave.Entities)
            {
                errorFound |= CheckForMissingTunnelReferences(entitySave);
            }


            #endregion

            if (!errorFound)
            {
                MessageBox.Show("No errors found.");
            }
        }

        private static bool CheckForMissingNonGeneratedPartials()
        {
            bool errorFound = false;

            #region  Loop through every .Generated.cs file to see if it has an associated object
            foreach (var buildItem in ProjectManager.ProjectBase.EvaluatedItems)
            {
                if (buildItem.UnevaluatedInclude.Contains(".generated.cs", StringComparison.OrdinalIgnoreCase))
                {
                    #region Prepare the "name" for checking

                    // See if there is an item for this
                    string name =
                        FileManager.RemoveExtension(FileManager.RemoveExtension(buildItem.UnevaluatedInclude));
                    name = name.Replace(".", "/");

                    #endregion

                    #region Is it a generated class that is always created?

                    // These are okay becacuse Glue makes them no matter what:
                    if (name == "Performance\\PoolList" ||
                        name == "Performance\\IPoolable" ||
                        name == "Performance\\IEntityFactory" ||
                        name == "GlobalContent")
                    {
                        continue;
                    }

                    #endregion

                    EntitySave entitySave = ObjectFinder.Self.GetEntitySave(name);

                    if (entitySave != null)
                    {
                        continue;
                    }

                    if (buildItem.UnevaluatedInclude.EndsWith("Factory.Generated.cs"))
                    {
                        // See if there is an Entity with this name without the Factory
                        string nameOfEntityReferencedByFactory =
                            FileManager.RemovePath(name.Substring(0, name.Length - "Factory".Length));

                        entitySave = ObjectFinder.Self.GetEntitySaveUnqualified(nameOfEntityReferencedByFactory);

                        if (entitySave != null)
                        {
                            continue;
                        }
                    }


                    ScreenSave screenSave = ObjectFinder.Self.GetScreenSave(name);

                    if (screenSave != null)
                    {
                        continue;
                    }

                    string className = FileManager.RemovePath(name);

                    ReferencedFileSave csvRfs = ObjectFinder.Self.GetFirstCsvUsingClass(className);

                    if (csvRfs != null)
                    {
                        continue;
                    }

                    string error = "Could not find matching Glue element for\n\n" + buildItem.UnevaluatedInclude;

                    errorFound = true;
                    // If we got here, then that means that the .Generated.cs file really isn't referenced, so let's show a error
                    System.Windows.Forms.MessageBox.Show(error);
                }
            }
            #endregion
            return errorFound;
        }

        private static bool CheckForMissingTunnelReferences(IElement asIElement)
        {
            bool errorFound = false;

            foreach (CustomVariable variable in asIElement.CustomVariables)
            {
                if (!string.IsNullOrEmpty(variable.SourceObject))
                {
                    NamedObjectSave nos = asIElement.GetNamedObjectRecursively(variable.SourceObject);

                    if (nos == null)
                    {
                        errorFound = true;

                        MessageBox.Show("The variable " + variable.Name + " references " + variable.SourceObject + " but " +
                            "this object doesn't exist.");
                    }
                    else
                    {
                        List<string> availableMembers = ExposedVariableManager.GetExposableMembersFor(nos).Select(item => item.Member).ToList();

                        if (!availableMembers.Contains(variable.SourceObjectProperty))
                        {
                            errorFound = true;
                            MessageBox.Show("The variable " + variable.Name + " references the property " + variable.SourceObjectProperty +
                                "in " + asIElement.ToString() + " which does not exist.");
                        }
                    }
                }
            }
            return errorFound;
        }


    }
}
