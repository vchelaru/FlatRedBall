using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EditorObjects.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Controls;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using L = Localization;

namespace FlatRedBall.Glue.Managers;

public class BuildToolAssociationManager
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
            }
            return mSelf;
        }
    }

    #endregion

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

        NewFileWindow nfw = new NewFileWindow();
        nfw.ComboBoxMessage = L.Texts.BuilderWhichForFile;

        int commandLineArgumentsId = nfw.AddTextBox(L.Texts.CliExtraArguments);
            
        bool showNoneOption = Elements.AvailableAssetTypes.Self.AllAssetTypes
            .Any(item => item.Extension == sourceExtension && string.IsNullOrEmpty(item.CustomBuildToolName));

        if(showNoneOption)
        {
            nfw.AddOption($"<{L.Texts.None}>");
        }

        foreach (BuildToolAssociation bta in btaList)
        {
            nfw.AddOption(bta);
        }

        if (btaList.Count != 0)
        {
            nfw.SelectedItem = btaList[0];
        }

        nfw.ResultName = FileManager.RemoveExtension(FileManager.RemovePath(fileName));
        DialogResult result = nfw.ShowDialog();
        extraCommandLineArguments = "";

        if (result == DialogResult.OK)
        {
            buildToolAssociation = nfw.SelectedItem as BuildToolAssociation;
            if (buildToolAssociation != null)
            {
                rfsName = nfw.ResultName;
                extraCommandLineArguments = nfw.GetValueFromId(commandLineArgumentsId);
            }
            else
            {
                userPickedNone = nfw.SelectedItem is string && (nfw.SelectedItem as string) == $"<{L.Texts.None}>";
            }
        }
        else
        {
            userCancelled = true;
        }




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