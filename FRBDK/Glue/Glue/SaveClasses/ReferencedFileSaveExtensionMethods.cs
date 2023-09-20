using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Content;

using SourceReferencingFile = FlatRedBall.Glue.Content.SourceReferencingFile;


using EditorObjects.Parsing;
using FlatRedBall.Glue.Errors;
using System.Windows.Forms;
using EditorObjects.SaveClasses;
using System.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class ReferencedFileSaveExtensionMethods
    {
        public static void FixAllTypes(this ReferencedFileSave referencedFileSave)
        {
            foreach(var property in referencedFileSave.Properties)
            {
                if (!string.IsNullOrEmpty(property.Type) && property.Value != null)
                {
                    object variableValue = property.Value;
                    var type = property.Type;

                    variableValue = CustomVariableExtensionMethods.FixValue(variableValue, type);

                    property.Value = variableValue;
                }
            }
        }
        public static string GetUnqualifiedTypeForCsv(this ReferencedFileSave referencedFileSave, string alternativeFileName = null)
        {
            string toReturn = GetTypeForCsvFile(referencedFileSave, alternativeFileName);

            if (toReturn.Contains('.'))
            {
                int startOfUnqualified = toReturn.LastIndexOf('.') + 1;
                toReturn = toReturn.Substring(startOfUnqualified);
            }

            return toReturn;
        }

        public static string GetTypeForCsvFile(this ReferencedFileSave referencedFileSave, string alternativeFileName = null)//string fileName)
        {
            if (referencedFileSave == null)
            {
                throw new ArgumentNullException("ReferencedFileSave is null - it can't be.");
            }

            string fileName = referencedFileSave.Name;
            if (!string.IsNullOrEmpty(alternativeFileName))
            {
                fileName = alternativeFileName;
            }

            if (!string.IsNullOrEmpty(referencedFileSave.UniformRowType))
            {
                return referencedFileSave.UniformRowType;
            }
            else
            {
                string className = null;

                // Make sure that the fileName is relative:
                // Wait!  There's no reason to do this.  The
                // RFS's Name property will always be relative
                // to the content project.  This is a must to make
                // projects portable so we don't have to do any processing
                // on the file name.
                //if (!FileManager.IsRelative(fileName))
                //{
                //    if (ProjectManager.ContentProject.Directory != null &&
                //        !FileManager.IsRelativeTo(ProjectManager.ContentProject.Directory, FileManager.RelativeDirectory))
                //    {
                //        fileName = FileManager.MakeRelative(fileName, ProjectManager.ContentProject.Directory);
                //    }
                //    else
                //    {
                //        fileName = FileManager.MakeRelative(fileName);
                //    }
                //}

                // Is this file using a custom class?
                CustomClassSave ccs = ObjectFinder.Self.GlueProject.GetCustomClassReferencingFile(fileName);
                if (ccs == null)
                {

                    className = FileManager.RemovePath(FileManager.RemoveExtension(fileName));
                    if (className.EndsWith("File"))
                    {
                        className = className.Substring(0, className.Length - "File".Length);
                    }

                    className = GlueState.Self.ProjectNamespace + ".DataTypes." + className;

                }
                else
                {
                    if (!string.IsNullOrEmpty( ccs.CustomNamespace) )
                    {
                        className = ccs.CustomNamespace + "." + ccs.Name;
                    }
                    else
                    {
                        className = GlueState.Self.ProjectNamespace + ".DataTypes." + ccs.Name;
                    }
                }
                return className;
            }
        }

        public static bool IsFileSourceForThis(this ReferencedFileSave instance, string fileName)
        {
            if (!string.IsNullOrEmpty(instance.SourceFile) &&
                 FileManager.RemoveDotDotSlash( ObjectFinder.Self.MakeAbsoluteContent(instance.SourceFile)).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if(instance.SourceFileCache != null)
            {
                return instance.SourceFileCache.Any(srf => String.Equals(srf.SourceFile, fileName, StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }

        /// <summary>
        /// Returns the name of variable generated in code.
        /// </summary>
        /// <param name="instance">The ReferencedFileSave to generate.</param>
        /// <returns>The name as generated.</returns>
        public static string GetInstanceName(this ReferencedFileSave instance)
        {
            if(instance.CachedInstanceName == null)
            {

                instance.CachedInstanceName =
                    FileManager.RemoveExtension(instance.Name)
                        .Replace(" ", "")
                        .Replace("-", "_")
                        // May 17, 2022
                        // File names with 
                        // invalid characters
                        // may make their way in
                        // to a project. In this case
                        // we will just strip out the invalid
                        // characters. We treat the dash as a special
                        // case since it can be converted to an underscore
                        // and still look somewhat similar. 
                        .Replace("(", "")
                        .Replace(")", "");

                if (instance.IncludeDirectoryRelativeToContainer)
                {
                    IElement container = instance.GetContainer();

                    if (container != null)
                    {
                        string directoryToMakeRelativeTo = container.Name;

                        bool isRelativeTo = FileManager.IsRelativeTo(instance.CachedInstanceName, directoryToMakeRelativeTo);

                        if (isRelativeTo)
                        {
                            instance.CachedInstanceName = FileManager.MakeRelative(instance.CachedInstanceName, directoryToMakeRelativeTo);
                            instance.CachedInstanceName = instance.CachedInstanceName.Replace("/", "_");
                        }
                        else
                        {
                            instance.CachedInstanceName = FileManager.RemovePath(instance.CachedInstanceName);
                        }

                        //string modifiedName = FileManager.Relativethis.Name;
                    }
                    else
                    {
                        // Might be something like:  "GlobalContent/FolderInGlobalContent/SceneFileInFolder"
                        if (instance.CachedInstanceName.StartsWith("GlobalContent/"))
                        {
                            instance.CachedInstanceName = instance.CachedInstanceName.Substring("GlobalContent/".Length);
                        }
                        instance.CachedInstanceName = instance.CachedInstanceName.Replace("/", "_");
                    }
                }
                else
                {

                    instance.CachedInstanceName = FileManager.RemovePath(instance.CachedInstanceName);

                }

                if(instance.CachedInstanceName.Length > 0 && char.IsDigit( instance.CachedInstanceName[0]))
                {
                    instance.CachedInstanceName = '_' + instance.CachedInstanceName;
                }

            }

            return instance.CachedInstanceName;

        }

        public static bool GetIsFileOutOfDate(this ReferencedFileSave instance, string absoluteSourceName, string absoluteDestinationName)
        {
            bool exists = System.IO.File.Exists(absoluteDestinationName);

            if (!exists || System.IO.File.GetLastWriteTime(absoluteSourceName) >
                    System.IO.File.GetLastWriteTime(absoluteDestinationName))
            {
                return true;
            }

            var buildToolAssociation = instance.GetBuildToolAssociation();

            if (buildToolAssociation != null)
            {
                string buildToolFileName = buildToolAssociation.BuildToolProcessed;
                string absoluteBuildTool = ProjectManager.ProjectBase.Directory + buildToolFileName;

                if (File.Exists(absoluteBuildTool))
                {
                    if (System.IO.File.GetLastWriteTime(absoluteBuildTool) >=
                        System.IO.File.GetLastWriteTime(absoluteDestinationName))
                    {
                        return true;
                    }
                }
            }

            return false;
            
        }

        public static bool GetCanUseContentPipeline(this ReferencedFileSave instance)
        {
            return
                // CSVs can use content pipeline
                // Update 1/29/2020 - no it can't:
                //instance.IsCsvOrTreatedAsCsv ||

                (!string.IsNullOrEmpty(instance.GetAssetTypeInfo()?.ContentProcessor));
        }

        public static string ReferencedFileSaveToString(ReferencedFileSave instance)
        {
            string containerText = "";
            if (instance.GetContainerType() == SaveClasses.ContainerType.None)
            {
                containerText = " (in GlobalContent)";
            }
            else
            {
                containerText = " (in " + instance.GetContainer() + ")";
            }
            return instance.Name + containerText;


        }

        public static GlueElement GetContainer(this ReferencedFileSave instance)
        {
            if (ObjectFinder.Self.GlueProject != null)
            {
                return ObjectFinder.Self.GetElementContaining(instance);
            }
            else
            {
                return null;
            }
        }

        public static ContainerType GetContainerType(this ReferencedFileSave instance)
        {
            IElement element = instance.GetContainer();

            if (element != null)
            {
                if (element is ScreenSave)
                {
                    return SaveClasses.ContainerType.Screen;
                }
                else
                {
                    return SaveClasses.ContainerType.Entity;
                }
            }
            else
            {
                return SaveClasses.ContainerType.None;
            }
        }

        public static AssetTypeInfo GetAssetTypeInfo(this ReferencedFileSave referencedFileSave)
        {
            string extension = FileManager.GetExtension(referencedFileSave.Name);

            if (!string.IsNullOrEmpty(referencedFileSave.RuntimeType))
            {
                // try finding one based on extension and type. If that doesn't exist, then just look at type

                var found = AvailableAssetTypes.Self.GetAssetTypeFromExtensionAndQualifiedRuntime(
                    extension, referencedFileSave.RuntimeType);

                if(found == null)
                {
                    found = AvailableAssetTypes.Self.AllAssetTypes.FirstOrDefault(item => item.QualifiedRuntimeTypeName.QualifiedType == referencedFileSave.RuntimeType);
                }

                return found;
            }
            else
            {

                return AvailableAssetTypes.Self.GetAssetTypeFromExtension(extension);
            }
        }

        public static bool GetGeneratesMember(this ReferencedFileSave instance)
        {

            bool toReturn = instance.LoadedAtRuntime && !instance.IsDatabaseForLocalizing;

            if(!instance.IsCsvOrTreatedAsCsv)
            {
                var ati = instance.GetAssetTypeInfo();

                if (ati != null &&
                    string.IsNullOrEmpty(ati.QualifiedRuntimeTypeName.QualifiedType))
                {
                    return false;
                }
            }

            return toReturn;

        }

        public static T GetProperty<T>(this ReferencedFileSave referencedFileSave, string propertyName)
        {
            var propertySave = referencedFileSave.Properties.FirstOrDefault(
                item => item.Name == propertyName);

            if(propertySave?.Value != null)
            {
                return (T)propertySave.Value;
            }
            else
            {
                return default(T);
            }
        }

        public static void SetProperty(this ReferencedFileSave referencedFileSave, string propertyName, object value)
        {
            var propertySave = referencedFileSave.Properties.FirstOrDefault(
                item => item.Name == propertyName);

            if(propertySave != null)
            {
                propertySave.Value = value;
            }
            else
            {
                propertySave = new PropertySave();
                propertySave.Value = value;
                propertySave.Name = propertyName;

                referencedFileSave.Properties.Add(propertySave);
            }
        }
    }
}
