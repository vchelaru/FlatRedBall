using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Content;

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

        public static bool IsFileSourceForThis(this ReferencedFileSave instance, FilePath filePath)
        {
            if (!string.IsNullOrEmpty(instance.SourceFile) &&
                 new FilePath(ObjectFinder.Self.MakeAbsoluteContent(instance.SourceFile)) == filePath)
            {
                return true;
            }

            return false;
        }

        public static bool IsFileSourceForThis(this ReferencedFileSave instance, string fileName)
        {
            if (!string.IsNullOrEmpty(instance.SourceFile) &&
                 FileManager.RemoveDotDotSlash( ObjectFinder.Self.MakeAbsoluteContent(instance.SourceFile)).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        // GetInstanceName, GetContainer, GetContainerType, ReferencedFileSaveToString,
        // GetIsSharedStaticEditable, both GetIsLinkedOutsideContainerFolder overloads, and
        // GetIsFileOutsideContainerFolder moved to GlueCommon.SaveClasses.ReferencedFileSaveElementExtensions
        // (#2276) - needed IObjectFinderCore.GetElementContaining widened with a ReferencedFileSave overload.

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
                string absoluteBuildTool = GlueState.Self.CurrentMainProject.Directory + buildToolFileName;

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
            var assetTypeInfo = instance.GetAssetTypeInfo();
            return
                // CSVs can use content pipeline
                // Update 1/29/2020 - no it can't:
                //instance.IsCsvOrTreatedAsCsv ||

                (!string.IsNullOrEmpty(assetTypeInfo?.ContentProcessor));
        }

        // GetAssetTypeInfo moved to GlueCommon.SaveClasses.ReferencedFileSaveAssetTypeExtensions
        // (#2276) - needed the new IAvailableAssetTypesCore seam over AvailableAssetTypes.Self.

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
