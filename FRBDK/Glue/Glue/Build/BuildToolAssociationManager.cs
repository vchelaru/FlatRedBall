using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EditorObjects.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using L = Localization;
using Glue;

namespace FlatRedBall.Glue.Managers;

public class BuildToolAssociationManager : IBuildToolAssociationCore
{
    #region Fields

    static BuildToolAssociationManager mSelf;

    #endregion

    #region Properties

    List<BuildToolAssociation> ProjectSpecificBuildTools => GlueState.Self.GlueSettingsSave.BuildToolAssociations;

    public static BuildToolAssociationManager Self
    {
        get
        {
            if (mSelf == null)
            {
                mSelf = new BuildToolAssociationManager();
                BuildToolAssociationCore.Self = mSelf;
            }
            return mSelf;
        }
    }

    #endregion

    /// <summary>
    /// Points the GlueCommon seam at this manager. Call once at startup (MainGlueWindow,
    /// GlueTestBootstrap) so GlueCommon code that only reaches build-tool associations through
    /// BuildToolAssociationCore.Self never runs ahead of the first direct call to <see cref="Self"/>
    /// in the process. Same role as DialogService.EnsureErrorReportingSeamWired.
    /// </summary>
    /// <remarks>
    /// GlueCommon can't reference this assembly (wrong direction), so it can't set the seam itself, and
    /// until this existed the seam was only assigned from the <see cref="Self"/> getter above. Nothing on
    /// the project-load path reads that getter before ProjectLoader.BuildAllOutOfDateFiles reaches
    /// ReferencedFileSaveBuildExtensions.GetIsFileOutOfDate in GlueCommon, so a fresh launch threw
    /// NullReferenceException on the first built file of any project (regression from #2315).
    /// </remarks>
    public static void EnsureBuildToolAssociationSeamWired()
    {
        BuildToolAssociationCore.Self ??= Self;
    }

    public string GetBuildToolProcessed(ReferencedFileSave referencedFileSave)
    {
        var destinationExtension = FileManager.GetExtension(referencedFileSave.Name);

        BuildToolAssociation buildToolAssociation;
        if (!string.IsNullOrEmpty(referencedFileSave.BuildTool))
        {
            buildToolAssociation = GetBuilderToolAssociationByName(referencedFileSave.BuildTool);
        }
        else
        {
            buildToolAssociation = GetBuilderToolAssociationForDestinationExtension(destinationExtension);
        }

        return buildToolAssociation?.BuildToolProcessed;
    }

    internal BuildToolAssociation GetBuilderToolAssociationForSourceExtension(string sourceExtension)
    {
        BuildToolAssociation buildToolAssociation = null;

        foreach (BuildToolAssociation bta in ProjectSpecificBuildTools)
        {
            if (bta.SourceFileType != null && bta.SourceFileType.ToLowerInvariant() == sourceExtension.ToLowerInvariant())
            {
                buildToolAssociation = bta;
                break;
            }
        }
        return buildToolAssociation;
    }

    public BuildToolAssociation GetBuilderToolAssociationForDestinationExtension(string destinationExtension)
    {
        BuildToolAssociation buildToolAssociation = null;

        foreach (BuildToolAssociation bta in ProjectSpecificBuildTools)
        {
            if (bta.DestinationFileType.ToLowerInvariant() == destinationExtension.ToLowerInvariant())
            {
                buildToolAssociation = bta;
                break;
            }
        }
        return buildToolAssociation;
    }

    public BuildToolAssociation GetBuilderToolAssociationForExtensions(string sourceExtension, string destinationExtension)
    {
        return ProjectSpecificBuildTools.FirstOrDefault(item =>
            item.SourceFileType != null && 
            item.SourceFileType.ToLowerInvariant() == sourceExtension.ToLowerInvariant() &&
            item.DestinationFileType.ToLowerInvariant() == destinationExtension.ToLowerInvariant());
    }

    internal BuildToolAssociation GetBuilderToolAssociationByName(string name)
    {
        BuildToolAssociation buildToolAssociation = null;

        foreach (var bta in ProjectSpecificBuildTools)
        {
            if (String.Equals(bta.ToString(), name, StringComparison.OrdinalIgnoreCase))
            {
                buildToolAssociation = bta;
                break;
            }
        }
        return buildToolAssociation;
    }


    public BuildToolAssociation GetBuildToolAssocationAndNameFor(string fileName, out bool userCancelled, out bool userPickedNone, out string rfsName, out string extraCommandLineArguments)
    {
        userCancelled = false;
        userPickedNone = false;
        rfsName = null;

        BuildToolAssociation buildToolAssociation = null;

        var sourceExtension = FileManager.GetExtension(fileName);

        var btaList = new List<BuildToolAssociation>();
        foreach (var bta in ProjectSpecificBuildTools)
        {
            if (bta.SourceFileType != null && String.Equals(bta.SourceFileType, sourceExtension, StringComparison.Ordinal))
            {
                btaList.Add(bta);
            }
        }

        bool showNoneOption = Elements.AvailableAssetTypes.Self.AllAssetTypes
            .Any(item => item.Extension == sourceExtension && string.IsNullOrEmpty(item.CustomBuildToolName));

        var defaultResultName = FileManager.RemoveExtension(FileManager.RemovePath(fileName));

        // Local variables to receive results from the lambda (out params can't be captured)
        bool localUserCancelled = false;
        bool localUserPickedNone = false;
        string localRfsName = null;
        string localExtraArgs = "";

        // WPF windows must be constructed and shown on the UI (STA) thread
        GlueCommands.Self.DoOnUiThread(() =>
        {
            NewFileWindow nfw = new NewFileWindow();
            nfw.ComboBoxMessage = "Build";

            int commandLineArgumentsId = nfw.AddTextBox("Enter extra command line arguments:");

            if (showNoneOption)
            {
                nfw.AddOption($"<None>");
            }

            foreach (BuildToolAssociation bta in btaList)
            {
                nfw.AddOption(bta);
            }

            if (btaList.Count != 0)
            {
                nfw.SelectedItem = btaList[0];
            }

            nfw.ResultName = defaultResultName;

            if (nfw.ShowDialog() == true)
            {
                buildToolAssociation = nfw.SelectedItem as BuildToolAssociation;
                if (buildToolAssociation != null)
                {
                    localRfsName = nfw.ResultName;
                    localExtraArgs = nfw.GetValueFromId(commandLineArgumentsId);
                }
                else
                {
                    localUserPickedNone = nfw.SelectedItem is string && (nfw.SelectedItem as string) == $"<None>";
                }
            }
            else
            {
                localUserCancelled = true;
            }
        });

        userCancelled = localUserCancelled;
        userPickedNone = localUserPickedNone;
        rfsName = localRfsName;
        extraCommandLineArguments = localExtraArgs;




        return buildToolAssociation;
    }

    public bool GetIfIsBuiltFile(string fileName)
    {
        var sourceExtension = FileManager.GetExtension(fileName);

        if (String.IsNullOrEmpty(sourceExtension))
        {
            return false;
        }

        return GlueState.Self.GlueSettingsSave.BuildToolAssociations
            .Any(item => String.Equals(item.SourceFileType, sourceExtension, StringComparison.OrdinalIgnoreCase));
    }
}
