using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;
using System.ComponentModel;
using System.Drawing;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Instructions;
using FlatRedBall.Glue.GuiDisplay.Facades;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using Microsoft.Xna.Framework;
using FlatRedBall.Glue.Plugins;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace FlatRedBall.Glue.SaveClasses
{
    public static partial class CustomVariableExtensionMethods
    {
        static IGlueState GlueState => EditorObjects.IoC.Container.Get<IGlueState>();
        static IGlueCommands GlueCommands => EditorObjects.IoC.Container.Get<IGlueCommands>();


        public static bool GetIsCsv(this CustomVariable customVariable)
        {
            if (customVariable.Type == null)
            {
                return false;
            }
            if (customVariable.Type.EndsWith(".csv"))
            {
                return true;
            }
            else if (customVariable.Type.EndsWith(".txt"))
            {
                //ReferencedFileSave rfs = ObjectFinder.Self.GetReferencedFileSaveFromFile(customVariable.Type);
                throw new NotImplementedException("Need to implement checking if a custom variable is CSV from Text");

            }
            else
            {
                return IsTypeOfAnyCsvInProject(GlueState.CurrentGlueProject, customVariable.Type);
            }
        }

        // The row types of every CSV in the project, while a CacheCsvTypes scope is open.
        static HashSet<string> cachedCsvTypes;

        /// <summary>
        /// Remembers the row type of every CSV in the project until the returned scope is disposed, so GetIsCsv
        /// stops walking every file per variable. Only for passes that do not change files or custom classes,
        /// like generating all code.
        /// </summary>
        public static IDisposable CacheCsvTypes()
        {
            var previous = cachedCsvTypes;
            var types = new HashSet<string>();
            var project = GlueState.CurrentGlueProject;
            if (project != null)
            {
                foreach (var file in project.GetAllReferencedFiles())
                {
                    if (file.IsCsvOrTreatedAsCsv)
                    {
                        types.Add(file.GetTypeForCsvFile());
                    }
                }
            }
            cachedCsvTypes = types;
            return new CsvTypeCacheScope(previous);
        }

        private sealed class CsvTypeCacheScope : IDisposable
        {
            private readonly HashSet<string> _previous;
            public CsvTypeCacheScope(HashSet<string> previous) => _previous = previous;
            public void Dispose() => cachedCsvTypes = _previous;
        }

        // Walks the project's file lists in place: code generation asks this for every variable, and copying every
        // file into a new list per call was a large part of the cost.
        private static bool IsTypeOfAnyCsvInProject(GlueProjectSave project, string type)
        {
            var cached = cachedCsvTypes;
            if (cached != null) return type != null && cached.Contains(type);

            if (project == null) return false;

            bool IsCsvOfType(List<ReferencedFileSave> files)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    if (file.IsCsvOrTreatedAsCsv && file.GetTypeForCsvFile() == type)
                    {
                        return true;
                    }
                }
                return false;
            }

            for (int i = 0; i < project.Entities.Count; i++)
            {
                if (IsCsvOfType(project.Entities[i].ReferencedFiles)) return true;
            }
            for (int i = 0; i < project.Screens.Count; i++)
            {
                if (IsCsvOfType(project.Screens[i].ReferencedFiles)) return true;
            }
            return IsCsvOfType(project.GlobalFiles);
        }

        public static bool GetIsListCsv(this CustomVariable customVariable)
        {
            if (customVariable.GetIsCsv())
            {
                string fullFileName = GlueState.ContentDirectory + customVariable.Type;
                ReferencedFileSave foundRfs = GlueCommands.FileCommands.GetReferencedFile(fullFileName);

                if (foundRfs != null)
                {
                    return foundRfs.CreatesDictionary == false;
                }
            }

            return false;
        }

        // GetIsVariableState, GetIsVariableStateAndCategory, GetIsBaseElementType, GetEntityNameDefiningThisTypeCategory,
        // SetDefaultValueAccordingToType and GetDefaultValueAccordingToType moved to
        // GlueCommon.SaveClasses.CustomVariableTypeExtensions (#2276).

        // FixAllTypes moved to GlueCommon.SaveClasses.CustomVariableTypeExtensions (#2276) -
        // PluginManager.TryAssignPreferredDisplayerFromName routes through the IPluginManagerCore seam.

        // FixValue moved to GlueCommon.SaveClasses.CustomVariableCommonExtensions (#2276) - pure
        // type-conversion logic, zero coupling.

        // FixEnumerationTypes, ConvertEnumerationValuesToInts, GetIsEnumeration, GetIsAnimationChain, GetIsFile,
        // GetIsObjectType and GetRuntimeType moved to GlueCommon.SaveClasses.CustomVariableTypeExtensions (#2276).

        // GetDefiningCustomVariable moved to GlueCommon.SaveClasses.CustomVariableTypeExtensions (#2276).

        public static bool GetIsExposingVariable(this CustomVariable customVariable, IElement container)
        {
            bool isExposedExistingMember = false;

            if (container is EntitySave)
            {
                isExposedExistingMember =
                    ExposedVariableManager.IsMemberDefinedByEntity(customVariable.Name, container as EntitySave);
            }
            else if (container is ScreenSave)
            {
                isExposedExistingMember = customVariable.Name == "CurrentState";
            }

            return isExposedExistingMember;
        }

        // GetIsTunneling moved to GlueCommon.SaveClasses.CustomVariableTypeExtensions (#2276).

        public static bool GetIsNewVariable(this CustomVariable customVariable, IElement container)
        {
            return customVariable.GetIsTunneling() == false && customVariable.GetIsExposingVariable(container) == false;
        }

        // CustomVariableToString moved to GlueCommon.SaveClasses.CustomVariableTypeExtensions (#2276).

        // HasAccompanyingVelocityConsideringTunneling and GetIsSourceFile moved to
        // GlueCommon.SaveClasses.CustomVariableTypeExtensions (#2276) - GetNamedObjectRecursively moved
        // with them, and GlueState.CurrentGlueProject is ObjectFinderCore.Self.GlueProject.
    }
    

}
