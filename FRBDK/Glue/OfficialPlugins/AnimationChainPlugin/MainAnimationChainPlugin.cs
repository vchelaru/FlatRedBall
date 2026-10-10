using FlatRedBall;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.AnimationChainPlugin.Errors;
using OfficialPlugins.AnimationChainPlugin.Managers;
using OfficialPlugins.ContentPreview.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Navigation;

using FileManager = ToolsUtilities.FileManager;

namespace OfficialPlugins.AnimationChainPlugin;

[Export(typeof(PluginBase))]
public class MainAnimationChainPlugin : PluginBase
{
    #region Fields/Properties

    public override string FriendlyName => "Animation Chain Plugin";

    public override Version Version => new Version(1, 0);

    public static MainAnimationChainPlugin Self { get; private set; }

    AchxManager _achxManager;

    #endregion

    public MainAnimationChainPlugin()
    {
        _achxManager = new AchxManager();
    }

    public override void StartUp()
    {
        Self = this;
        AssignEvents();
        this.AddErrorReporter(new AnimationChainErrorReporter());

        AchxManager.Initialize(this);
    }

    private void AssignEvents()
    {
        this.ReactToNewFileHandler += HandleNewFile;
        this.ReactToFileChange += HandleFileChanged;
        this.ReactToNamedObjectChangedValue += NamedObjectVariableChangeLogic.HandleNamedObjectChangedValue;
        this.TryHandleTreeNodeDoubleClicked += TryHandleDoubleClick;
        this.ReactToItemsSelected += HandleItemsSelected;
        this.ReactToLoadedGluxEarly += HandleLoadedGluxEarly;
        this.ReactToUnloadedGlux += HandleUnloadedGlux;
        this.IsHandlingHotkeys += GetIfIsHandlingHotkeys;
        //this.FillWithReferencedFiles += HandleFillWithReferencedFiles;
        this.FillWithReferencedFiles += HandleFillWithReferencedFilesNew;
        this.AddNewFileOptionsHandler += HandleAddNewFileOptions;
    }

    private void HandleAddNewFileOptions(CustomizableNewFileWindow newFileWindow)
    {
        // we can always add Atlas support
        var viewModel = newFileWindow.ViewModel;

        AssetTypeInfo? spineAtlasAti = AvailableAssetTypes.Self.AllAssetTypes
            .FirstOrDefault(item => item.QualifiedRuntimeTypeName.QualifiedType == "Spine.Atlas");

        if(spineAtlasAti != null)
        {
            viewModel.AllOptions.Add(spineAtlasAti);
        }
    }


    // See HandleFillWithReferencedFilesNew for info on why this isn't used
    private ToolsUtilities.GeneralResponse HandleFillWithReferencedFiles(FilePath path, List<FilePath> list)
    {
        if(path.Extension == "achx")
        {
            if(path.Exists())
            {
                var acls = AnimationChainListSave.FromFile(path.FullPath);
                var newReferencedFiles = acls.GetReferencedFiles(RelativeType.Absolute).Select(item => new FilePath(item)).ToList();

                list.AddRange(newReferencedFiles);

                return ToolsUtilities.GeneralResponse.SuccessfulResponse;
            }
            else
            {
                return ToolsUtilities.GeneralResponse.UnsuccessfulWith("File does not exist: " + path.FullPath);
            }
        }
        else
        {
            return ToolsUtilities.GeneralResponse.SuccessfulResponse;
        }
    }

    // Deadvivors has a lot of .achx files and 
    // loading the project can be a bit slow. This
    // method attempts to speed up the .achx file reference
    // tracking by looping through the lines and looking for
    // the <TextureName> XML tag. This seems to be faster than
    // XML loading - my tests loaded a file 1000 times and it went
    // from 0.7 seconds to 0.2 seconds. This is a little less flexible
    // since it assumes TextureName rather than relying on reusable reference
    // tracking, but modern .achx files only use this.
    internal ToolsUtilities.GeneralResponse HandleFillWithReferencedFilesNew(FilePath path, HashSet<FilePath> list)
    {
        if (path.Extension == "achx")
        {
            if (path.Exists())
            {
                var directory = path.GetDirectoryContainingThis();

                try
                {
                    GlueCommands.Self.TryMultipleTimes(() =>
                    {
                        // might be faster to read the entire file:
                        var contents = System.IO.File.ReadAllLines(path.FullPath);

                        var textureNameLength = "<TextureName>".Length;
                        foreach(var line in contents)
                        {
                            if(line.Contains("<TextureName>"))
                            {
                                var startIndex = line.IndexOf("<TextureName>") + textureNameLength;
                                var endIndex = line.IndexOf("</TextureName>");
                                var textureName = line.Substring(startIndex, endIndex - startIndex);
                                list.Add(directory + textureName);
                            }
                        }
                    });
                }
                catch(IOException exception)
                {
                    GlueCommands.Self.PrintError($"Error trying to read .achx referenced files:\n{exception}");
                }


                return ToolsUtilities.GeneralResponse.SuccessfulResponse;
            }
            else
            {
                return ToolsUtilities.GeneralResponse.UnsuccessfulWith("File does not exist: " + path.FullPath);
            }
        }
        else if (path.Extension == "achj")
        {
            if (path.Exists())
            {
                string contents = null;

                try
                {
                    // Only the read is retried: the file may be mid-save, but a parse error won't fix itself in 200ms.
                    GlueCommands.Self.TryMultipleTimes(() => contents = System.IO.File.ReadAllText(path.FullPath));

                    var directory = path.GetDirectoryContainingThis();
                    foreach (var textureName in GetTextureNamesInAchj(contents))
                    {
                        list.Add(directory + textureName);
                    }
                }
                catch (Exception exception) when (exception is IOException || exception is JsonException)
                {
                    GlueCommands.Self.PrintError($"Error trying to read .achj referenced files:\n{exception}");
                }

                return ToolsUtilities.GeneralResponse.SuccessfulResponse;
            }
            else
            {
                return ToolsUtilities.GeneralResponse.UnsuccessfulWith("File does not exist: " + path.FullPath);
            }
        }
        else
        {
            return ToolsUtilities.GeneralResponse.SuccessfulResponse;
        }
    }

    /// <summary>
    /// The textureName of every frame in an .achj file. The JSON counterpart of scanning an .achx for
    /// &lt;TextureName&gt;: it skips the full AnimationChainListSave load for the same reason.
    /// </summary>
    internal static List<string> GetTextureNamesInAchj(string json)
    {
        var textureNames = new List<string>();

        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind == JsonValueKind.Object &&
            document.RootElement.TryGetProperty("animationChains", out var chains) &&
            chains.ValueKind == JsonValueKind.Array)
        {
            foreach (var chain in chains.EnumerateArray())
            {
                if (chain.ValueKind == JsonValueKind.Object &&
                    chain.TryGetProperty("frames", out var frames) &&
                    frames.ValueKind == JsonValueKind.Array)
                {
                    foreach (var frame in frames.EnumerateArray())
                    {
                        if (frame.ValueKind == JsonValueKind.Object &&
                            frame.TryGetProperty("textureName", out var textureName) &&
                            textureName.ValueKind == JsonValueKind.String &&
                            !string.IsNullOrEmpty(textureName.GetString()))
                        {
                            textureNames.Add(textureName.GetString());
                        }
                    }
                }
            }
        }

        return textureNames;
    }

    private bool GetIfIsHandlingHotkeys()
    {
        return AchxManager.GetIfIsHandlingHotkeys();
    }

    public new void RefreshErrors() => base.RefreshErrors();

    private void HandleLoadedGluxEarly()
    {
        var ati = AssetTypeInfoManager.Self.TryGetAsepriteAti();
        if(ati != null)
        {
            base.AddAssetTypeInfo(ati);
        }

        // Add a Gum animation too:
        base.AddAssetTypeInfo(AssetTypeInfoManager.Self.GetGumAnimationChainListAti());

        AddAchjAssetTypeInfoIfSupported(
            GlueState.Self.CurrentGlueProject.FileVersion,
            GlueState.Self.CurrentMainProject.IsFrbSourceLinked());
    }

    internal void AddAchjAssetTypeInfoIfSupported(int fileVersion, bool isFrbSourceLinked)
    {
        if (AssetTypeInfoManager.ShouldRegisterAchjAti(fileVersion, isFrbSourceLinked))
        {
            base.AddAssetTypeInfo(AssetTypeInfoManager.Self.GetAchjAti());
        }
    }

    private void HandleUnloadedGlux()
    {
        base.UnregisterAssetTypeInfos();
    }

    private bool TryHandleDoubleClick(ITreeNode tree)
    {
        // Double-clicking an .achx falls through to Glue's default file-open handling,
        // which launches the external AnimationEditor via FileCommands.
        return false;
    }

    private void HandleItemsSelected(List<ITreeNode> list) 
    {
        var file = GlueState.Self.CurrentReferencedFileSave;

        /////////////////Early Out///////////////////
        if (file == null)
        {
            AchxManager.HideTab();
            return;
        }
        ///////////////End Early Out/////////////////

        var filePath = GlueCommands.Self.GetAbsoluteFilePath(file);

        var extension = filePath.Extension;

        switch (extension)
        {
            case "achx":
            case "atlas":
                _achxManager.ShowTab(filePath);
                break;
            default:
                AchxManager.HideTab();
                break;
        }
    }


    /// <summary>
    /// Whether a change to a file with this extension can change which animation chains a Sprite
    /// references, so the animation errors need re-evaluating.
    /// </summary>
    internal static bool AffectsAnimationErrors(string extension) => extension == "achx" || extension == "achj";

    private void HandleFileChanged(FilePath filePath, FileChangeType fileChange)
    {
        if (AffectsAnimationErrors(filePath.Extension))
        {
            this.RefreshErrors();

            if (AchxManager.AchxFilePath == filePath)
            {
                AchxManager.ForceRefreshAchx(filePath);
            }
        }
    }

    private void HandleNewFile(ReferencedFileSave newFile, AssetTypeInfo assetTypeInfo)
    {
        var extension = FileManager.GetExtension(newFile.Name);
        if(extension == "achx")
        {
            var file = GlueCommands.Self.GetAbsoluteFilePath(newFile);

            if(file.Exists())
            {
                // load it and set the project file
                var achx = AnimationChainListSave.FromFile(file.FullPath);

                var projectFile = FileManager.MakeRelative(GlueState.Self.GlueProjectFileName.FullPath, file.GetDirectoryContainingThis().FullPath);

                if(projectFile != achx.ProjectFile)
                {
                    achx.ProjectFile = projectFile;
                    achx.Save(file.FullPath);
                }
            }
        }
    }
}
