using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Content;
using GluePropertyGridClasses.Interfaces;

using SourceReferencingFile = FlatRedBall.Glue.Content.SourceReferencingFile;

#if GLUE

using FlatRedBall.Glue.Facades;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Errors;
using System.Windows.Forms;
using EditorObjects.SaveClasses;
using System.IO;
using FlatRedBall.Glue.Plugins;
#endif

namespace FlatRedBall.Glue.SaveClasses
{
    public static class ReferencedFileSaveExtensionMethods
    {
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

                    className = EditorObjects.IoC.Container.Get<IVsProjectState>().DefaultNamespace + ".DataTypes." + className;

                }
                else
                {
                    if (!string.IsNullOrEmpty( ccs.CustomNamespace) )
                    {
                        className = ccs.CustomNamespace + "." + ccs.Name;
                    }
                    else
                    {
                        className = EditorObjects.IoC.Container.Get<IVsProjectState>().DefaultNamespace + ".DataTypes." + ccs.Name;
                    }
                }
                return className;
            }
        }

        public static bool IsFileSourceForThis(this ReferencedFileSave instance, string fileName)
        {
            if (!string.IsNullOrEmpty(instance.SourceFile) &&
                 FileManager.RemoveDotDotSlash( ObjectFinder.Self.MakeAbsoluteContent(instance.SourceFile) ).ToLower() == fileName)
            {
                return true;
            }

            if(instance.SourceFileCache != null)
            {
                foreach (SourceReferencingFile srf in instance.SourceFileCache)
                {
                    if (srf.SourceFile == fileName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static string GetInstanceName(this ReferencedFileSave instance)
        {
            string toReturn =
                FileManager.RemoveExtension(instance.Name).Replace(" ", "").Replace("-", "_");

            if (instance.IncludeDirectoryRelativeToContainer)
            {
                IElement container = instance.GetContainer();

                if (container != null)
                {
                    string directoryToMakeRelativeTo = container.Name;

                    bool isRelativeTo = FileManager.IsRelativeTo(toReturn, directoryToMakeRelativeTo);

                    if (isRelativeTo)
                    {
                        toReturn = FileManager.MakeRelative(toReturn, directoryToMakeRelativeTo);
                        toReturn = toReturn.Replace("/", "_");
                    }
                    else
                    {
                        toReturn = FileManager.RemovePath(toReturn);
                    }

                    //string modifiedName = FileManager.Relativethis.Name;
                }
                else
                {
                    // Might be something like:  "GlobalContent/FolderInGlobalContent/SceneFileInFolder"
                    if (toReturn.StartsWith("GlobalContent/"))
                    {
                        toReturn = toReturn.Substring("GlobalContent/".Length);
                    }
                    toReturn = toReturn.Replace("/", "_");
                }
            }
            else
            {

                toReturn = FileManager.RemovePath(toReturn);

            }

            return toReturn;

        }

        public static bool GetIsFileOutOfDate(this ReferencedFileSave instance, string absoluteSourceName, string absoluteDestinationName)
        {
            bool exists = System.IO.File.Exists(absoluteDestinationName);

            if (!exists || System.IO.File.GetLastWriteTime(absoluteSourceName) >
                    System.IO.File.GetLastWriteTime(absoluteDestinationName))
            {
                return true;
            }

#if GLUE
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
#endif
            return false;
            
        }

        public static bool GetCanUseContentPipeline(this ReferencedFileSave instance)
        {
            return
                // CSVs can use content pipeline
                instance.IsCsvOrTreatedAsCsv ||

                (instance.GetAssetTypeInfo() != null &&
                !string.IsNullOrEmpty(instance.GetAssetTypeInfo().ContentProcessor));
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

        public static IElement GetContainer(this ReferencedFileSave instance)
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

        public static AssetTypeInfo GetAssetTypeInfo(this ReferencedFileSave instance)
        {
            string extension = FileManager.GetExtension(instance.Name);

            if (!string.IsNullOrEmpty(instance.RuntimeType))
            {
                // try finding one based on extension and type. If that doesn't exist, then just look at type

                var found = AvailableAssetTypes.Self.GetAssetTypeFromExtensionAndQualifiedRuntime(
                    extension, instance.RuntimeType);

                if(found == null)
                {
                    found = AvailableAssetTypes.Self.AllAssetTypes.FirstOrDefault(item => item.QualifiedRuntimeTypeName.QualifiedType == instance.RuntimeType);
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
