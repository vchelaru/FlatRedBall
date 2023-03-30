using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using System.IO;
using EditorObjects.SaveClasses;
using System.Windows.Forms;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Content;
using FlatRedBall.Glue.Facades;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Managers;
using SourceReferencingFile = FlatRedBall.Glue.Content.SourceReferencingFile;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.SaveClasses
{
    public static class ReferencedFileSaveExtensionMethodsGlue
    {
        public static IProjectValues ProjectValues
        {
            get;
            set;
        }


        public static string GetRelativePath(this ReferencedFileSave referencedFileSave)
        {
            return ProjectManager.ContentDirectoryRelative + referencedFileSave.Name;
        }


        public static bool GetIsBuiltFileOutOfDate(this ReferencedFileSave instance)
        {
            if (ProjectValues == null)
            {
                throw new Exception("ProjectValues must be set before using ReferencedFileSaveExtensionMethods");
            }

            string contentDirectory = ProjectValues.ContentDirectory;
            bool returnValue = false;
            if (!string.IsNullOrEmpty(instance.SourceFile))
            {

                returnValue |= instance.GetIsFileOutOfDate(contentDirectory + instance.SourceFile, contentDirectory + instance.Name);
            }

            if(instance.SourceFileCache != null)
            {
                foreach (SourceReferencingFile srf in instance.SourceFileCache)
                {
                    returnValue |= instance.GetIsFileOutOfDate(
                        contentDirectory + srf.SourceFile,
                        contentDirectory + srf.DestinationFile);
                }
            }


            return returnValue;
        }


        public static string PerformExternalBuild(this ReferencedFileSave instance)
        {
            return PerformExternalBuild(instance, false);
        }

        public static string PerformExternalBuild(this ReferencedFileSave instance, bool runAsync)
        {
            string error = "";
            if (!string.IsNullOrEmpty(instance.SourceFile))
            {
                string absoluteDestinationName = GlueCommands.Self.GetAbsoluteFileName(instance.Name, true);
                string absoluteSourceName = GlueCommands.Self.GetAbsoluteFileName(instance.SourceFile, true);

                error = instance.PerformBuildOnFile(absoluteSourceName, absoluteDestinationName, runAsync);
            }


            if(instance.SourceFileCache != null)
            {
                foreach (SourceReferencingFile srf in instance.SourceFileCache)
                {
                    string absoluteSourceName = GlueCommands.Self.GetAbsoluteFileName(srf.SourceFile, true);
                    string absoluteDestinationName = GlueCommands.Self.GetAbsoluteFileName(srf.DestinationFile, true);

                    string resultOfBuild = instance.PerformBuildOnFile(absoluteSourceName, absoluteDestinationName, runAsync);

                    if (!string.IsNullOrEmpty(resultOfBuild))
                    {
                        error += resultOfBuild;
                    }
                }

            }

            return error;
        }

        public static void RefreshSourceFileCache(this ReferencedFileSave instance, bool buildOnMissingFile, out string error)
        {
            error = null;
            string verboseError = "";
            string fullName = GlueCommands.Self.GetAbsoluteFileName(instance);

            try
            {

                // If it has a SourceFile, then no need to throw an error here, it will be built
                if (!FileManager.FileExists(fullName) && string.IsNullOrEmpty(instance.SourceFile ))
                {

                }
                else
                {

                    instance.SourceFileCache = ContentParser.GetSourceReferencingFilesReferencedByAsset(fullName, TopLevelOrRecursive.Recursive, ErrorBehavior.ContinueSilently, ref error, ref verboseError);

                    bool hasErrorOccurred = !string.IsNullOrEmpty(error);

                    if (!string.IsNullOrEmpty(verboseError))
                    {
                        int m = 3;
                    }

                    // If the file doesn't exist, no reason to do this, just build it
                    if (hasErrorOccurred)
                    {
                        bool forceError = false;

                        if (buildOnMissingFile && (!string.IsNullOrEmpty(instance.SourceFile) || instance.SourceFileCache?.Count != 0))
                        {
                            string subError = instance.PerformExternalBuild(runAsync:false);

                            if (!string.IsNullOrEmpty(subError))
                            {
                                error += "\nTried to build the file, but also got this error:\n" + subError;
                                // This will be returned through the "out"
                                //ErrorReporter.ReportError(fullName, error, forceError);
                            }
                            else if (System.IO.File.Exists(fullName))
                            {

                            }

                        }
                        else
                        {
                            string message = "";
                            if (!FileManager.FileExists(fullName))
                            {
                                message = "Could not find the file\n\n" + fullName + "\n\n" +
                                        "Glue will not be able to properly track dependencies and add the necessary files " +
                                        "to your project until this file is added.\n\nThis often happens if someone forgot to " +
                                        "check something in to Subversion.";
                            }
                            else
                            {
                                message = "Error tracking dependencies for\n\n" + fullName + "\n\nError details:\n\n" +
                                    error;
                            }
                            error = message;
                        }
                    }

                }

                if(instance.SourceFileCache != null)
                {
                    ContentParser.EliminateDuplicateSourceReferencingFiles(instance.SourceFileCache);
                }
            }
            catch (Exception e)
            {

                error = "Error getting referenced files for\n\n" +
                    fullName + "\n\nError:\n\n" + e.ToString();

            }
        }

        private static string PerformBuildOnFile(this ReferencedFileSave instance, string absoluteSourceName, string absoluteDestinationName, bool runAsync)
        {

            bool doesFileExist = FileManager.FileExists(absoluteSourceName);

            string error = "";

            if (!doesFileExist)
            {
                MessageBox.Show("Could not find the following source file:\n\n" +
                    absoluteSourceName);

            }
            else
            {


                #region Find the BuildToolAssociation
                BuildToolAssociation buildToolAssociation = 
                    GetBuildToolAssociation(instance);
                #endregion

                string destinationExtension = FileManager.GetExtension(instance.Name);

                #region If there is no BuildToolAssociation, tell the user that's the case
                if (buildToolAssociation == null)
                {
                    error = "Could not find the build tool for the source file\n" + absoluteSourceName + "\nwith destination extension\n" + destinationExtension;

                    error += "\nThere are " + GlueState.Self.GlueSettingsSave.BuildToolAssociations.Count + " build tools registered with Glue";

                    foreach (BuildToolAssociation bta in GlueState.Self.GlueSettingsSave.BuildToolAssociations)
                    {
                        error += "\n\"" + bta.SourceFileType + "\" -> " +  bta.BuildToolProcessed + " -> \"" + bta.DestinationFileType + "\"";
                    }

                    System.Windows.Forms.MessageBox.Show(error);
                }

                #endregion

                #region else, there is one, so let's do the build

                else
                {
                    #region Call the process (the build tool)



                    try
                    {
                        // Something could have screwed up the relative directory, so let's reset it here
                        FileManager.RelativeDirectory = ProjectManager.ProjectBase.Directory;

                        error = buildToolAssociation.PerformBuildOn(absoluteSourceName, absoluteDestinationName, instance.AdditionalArguments, PluginManager.ReceiveOutput, PluginManager.ReceiveError, runAsync);
                    }
                    catch (FileNotFoundException fnfe)
                    {
                        System.Windows.Forms.MessageBox.Show("Can't find the file:\n" + fnfe.FileName);
                    }
                    catch (Exception e)
                    {
                        System.Windows.Forms.MessageBox.Show("There was an error building the file\n" + absoluteSourceName + "\n\n" +
                            e.Message);
                    }
                    #endregion

                }

                #endregion
            }

            return error;

        }

        public static BuildToolAssociation GetBuildToolAssociation(this ReferencedFileSave instance)
        {
           string destinationExtension =
                                FileManager.GetExtension(instance.Name);
            // See if there is a tool that this is built with

            BuildToolAssociation buildToolAssociation = null;

            if (!string.IsNullOrEmpty(instance.BuildTool))
            {
                buildToolAssociation = BuildToolAssociationManager.Self.GetBuilderToolAssociationByName(instance.BuildTool);
            }
            else
            {
                buildToolAssociation = BuildToolAssociationManager.Self.GetBuilderToolAssociationForDestinationExtension(destinationExtension);
            }

            return buildToolAssociation;
        }

        public static AssetTypeInfo GetAssetTypeInfoForProjectSpecificFile(this ReferencedFileSave instance, ProjectSpecificFile psf)
        {
            string extension = FileManager.GetExtension(psf.FilePath);
            AssetTypeInfo thisAssetTypeInfo = instance.GetAssetTypeInfo();

            foreach (AssetTypeInfo assetTypeInfo in AvailableAssetTypes.Self.AllAssetTypes)
            {
                if (assetTypeInfo.Extension == extension &&
                    assetTypeInfo.RuntimeTypeName == thisAssetTypeInfo.RuntimeTypeName)
                {
                    return assetTypeInfo;
                }

            }
            return null;
        }

    }
}
