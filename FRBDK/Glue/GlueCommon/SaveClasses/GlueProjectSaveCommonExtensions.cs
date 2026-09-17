using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlatRedBall.Glue.Controls;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Split out of <c>GlueProjectSaveExtensionMethods</c> (in <c>Glue.csproj</c>, net8.0-windows): the
    /// project-wide load-time fix-ups and walks that only fan out over the project's screens, entities and
    /// global files and call element-level logic that already lives in GlueCommon. The two that report
    /// problems to the user do so through <see cref="IErrorReportingCore"/> (a narrow seam over
    /// <c>DialogService</c>, see that interface's doc comment) instead of <c>GlueCommands</c>. Lives here
    /// (net8.0, no WPF) so it and its tests can build and run on Linux/macOS. See issue #2276. Named
    /// differently from the original class (not a forwarding stub) to avoid a duplicate-type clash now that
    /// both assemblies are visible together via <c>Glue.csproj</c>'s <c>ProjectReference</c> to
    /// <c>GlueCommon</c>; extension method resolution doesn't care which class declares it, so existing
    /// call sites are unaffected.
    /// </summary>
    public static class GlueProjectSaveCommonExtensions
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
                    bool hasFoundState = foundEntitySave.GetStateRecursively(nos.CurrentState) != null;

                    if (!hasFoundState)
                    {
                        if (showPopupsOnFixedErrors)
                        {
                            ErrorReportingCore.Self.ShowMessage(
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

        public static void FixReferencedFileSaveContentPipelineSettings(this GlueProjectSave instance)
        {
            Parallel.ForEach(instance.Entities, (entitySave) =>
            {
                FixRfsListContentPipelineSetting(entitySave.ReferencedFiles);
            });

            Parallel.ForEach(instance.Screens, (screenSave) =>
            {
                FixRfsListContentPipelineSetting(screenSave.ReferencedFiles);
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

        public static void FixAllTypesPostLoad(this GlueProjectSave instance)
        {
            foreach (EntitySave entitySave in instance.Entities)
            {
                entitySave.FixAllTypes();
            }

            foreach (ScreenSave screen in instance.Screens)
            {
                screen.FixAllTypes();
            }

            foreach (var file in instance.GlobalFiles)
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

        /// <summary>
        /// Reports (but does not fix) named objects that share an instance name within one element.
        /// Public (it was private in Glue) so <c>GlueProjectSaveExtensionMethods.FixNamedObjects</c>, which
        /// stays in Glue.csproj, can still call it.
        /// </summary>
        public static void SearchForDuplicateNamedObjects(this GlueProjectSave instance)
        {
            List<string> names = new List<string>();
            foreach (EntitySave entitySave in instance.Entities)
            {
                names.Clear();

                foreach (NamedObjectSave nos in entitySave.NamedObjects)
                {
                    if (names.Contains(nos.InstanceName))
                    {
                        ErrorReportingCore.Self.ShowMessage("There are two objects named " + nos.InstanceName + " in the entity " + entitySave.ToString());
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
                        ErrorReportingCore.Self.ShowMessage("There are two objects named " + nos.InstanceName + " in the entity " + screenSave.ToString());
                    }
                    else
                    {
                        names.Add(nos.InstanceName);
                    }
                }
            }
        }

        public static void SearchForDuplicateEntities(this GlueProjectSave instance)
        {
            Dictionary<string, EntitySave> entitiesVisited = new Dictionary<string, EntitySave>();

            foreach (EntitySave entitySave in instance.Entities)
            {
                if (entitiesVisited.ContainsKey(entitySave.Name))
                {
                    ErrorReportingCore.Self.ShowMessage(
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
                entitySave.CleanUnusedVariablesFromStates();
            });

            Parallel.ForEach(instance.Screens, (screen) =>
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
    }
}
