using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using FlatRedBall.AnimationEditorForms.Data;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using Newtonsoft.Json;
using OfficialPlugins.AnimationChainPlugin.Managers;
using OfficialPlugins.AnimationChainPlugin.Views;
using OfficialPlugins.Common.ViewModels;
using SpineAtlasLibrary;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels;

internal class AchxViewModel : ViewModel
{
    #region Fields/Properties

    public ZoomViewModel TopWindowZoom { get; set; }
    public ZoomViewModel BottomWindowZoom { get; set; }

    private readonly AchxManager _achxManager;
    private readonly TextureCoordinateSelectionViewModel _textureCoordinateSelectionViewModel;
    private readonly IFileCommands _fileCommands;
    private readonly IDialogCommands _dialogCommands;

    public SettingsViewModel Settings { get => Get<SettingsViewModel>(); private set => Set(value); }

    public FilePath AchxFilePath { get; set; }

    public int ResolutionWidth
    {
        get => Get<int>();
        set => Set(value);
    }

    public int ResolutionHeight
    {
        get => Get<int>();
        set => Set(value);
    }

    [DependsOn(nameof(ResolutionWidth))]
    [DependsOn(nameof(ResolutionHeight))]
    public string ResolutionDisplay => $"{ResolutionWidth}x{ResolutionHeight}";

    public ViewModel SelectedItem
    {
        get => Get<ViewModel>();
        set => Set(value);
    }

    public ObservableCollection<AnimationChainViewModel> VisibleRoot 
    { 
        get => Get<ObservableCollection<AnimationChainViewModel>>();
        private set => Set(value);
    }


    [DependsOn(nameof(SelectedItem))]
    public AnimationFrameViewModel SelectedAnimationFrame => 
        SelectedItem as AnimationFrameViewModel;


    [DependsOn(nameof(SelectedItem))]
    public ShapeViewModel SelectedShape =>
        SelectedItem as ShapeViewModel;

    /// <summary>
    /// The effective current AnimationChain, which could be directly selected or indirectly selected
    /// by having one of its children selected
    /// </summary>
    [DependsOn(nameof(SelectedShape))]
    [DependsOn(nameof(SelectedAnimationFrame))]
    public AnimationFrameViewModel? CurrentAnimationFrame
    {
        get
        {
            if(SelectedShape != null)
            {
                return SelectedShape.Parent;
            }
            else
            {
                return SelectedAnimationFrame;
            }
        }
        set
        {
            SelectedItem = value;
        }
    }

    [DependsOn(nameof(SelectedItem))]
    public AnimationChainViewModel? CurrentAnimationChain
    {
        get
        {
            var selectedItem = SelectedItem;
            if(selectedItem is AnimationChainViewModel asAnimationChainViewModel)
            {
                return asAnimationChainViewModel;
            }
            else if(selectedItem is AnimationFrameViewModel asAnimationFrameViewModel)
            {
                return asAnimationFrameViewModel.Parent;
            }
            else if(selectedItem is ShapeViewModel asShapeViewModel)
            {
                return asShapeViewModel.Parent?.Parent;
            }
            else
            {
                return null;
            }
        }
        set
        {
            SelectedItem = value;
        }
    }

    public AnimationChainListSave BackingData { get; internal set; }
    public ICommand AddAnimationCommand { get; internal set; }
    public ICommand AddFrameCommand { get; internal set; }

    public ICommand DuplicateFrameCommand { get; internal set; }
    public ICommand AdjustAllFrameTimeCommand { get; internal set; }
    public ICommand SortAnimationsAlphabeticallyCommand { get; internal set; }

    public event Action<AnimationFrameViewModel, string> FrameUpdatedByUi;

    // Event raised when an animation chain is updated by the UI,
    // such as a new frame being added or a frame being removed.
    // This is used rather than relying on the VisibleRoot because
    // VisibleRoot updates when animations are loaded. We don't want 
    // to react to properties changing when animations are loaded, only
    // when the user interacts with the UI.
    public event Action<AnimationChainViewModel, string> AnimationChainUpdatedByUi;

    public event Action<AnimationChainViewModel, NotifyCollectionChangedEventArgs> AnimationChainCollectionChanged;

    public bool IsSnappingChecked
    {
        get => _textureCoordinateSelectionViewModel.SnapChecked;
        set
        {
            _textureCoordinateSelectionViewModel.SnapChecked = value;
            NotifyPropertyChanged(nameof(IsSnappingChecked));
        }
    }

    public ushort SnappingSize
    {
        get => _textureCoordinateSelectionViewModel.CellWidth;
        set
        {
            _textureCoordinateSelectionViewModel.CellWidth = value;
            _textureCoordinateSelectionViewModel.CellHeight = value;
            NotifyPropertyChanged(nameof(SnappingSize));
        }
    }

    #endregion

    public AchxViewModel(AchxManager achxManager,
        TextureCoordinateSelectionViewModel textureCoordinateSelectionViewModel,
        IDialogCommands dialogCommands,
        IFileCommands fileCommands)
    {
        _achxManager = achxManager;
        _textureCoordinateSelectionViewModel = textureCoordinateSelectionViewModel;
        _fileCommands = fileCommands;

        _dialogCommands = dialogCommands;

        Settings = new SettingsViewModel();
        TopWindowZoom = new ZoomViewModel();
        BottomWindowZoom = new ZoomViewModel();

        TopWindowZoom.CurrentZoomPercent = 100;
        BottomWindowZoom.CurrentZoomPercent = 100;

        VisibleRoot = new ObservableCollection<AnimationChainViewModel>();
        VisibleRoot.CollectionChanged += HandleAnimationChainViewModelCollectionChanged;

        AddAnimationCommand = new CommandBase(HandleAddAnimation);
        AddFrameCommand = new CommandBase(HandleAddFrame);
        AdjustAllFrameTimeCommand = new CommandBase(HandleAdjustAllFrameTime);
        SortAnimationsAlphabeticallyCommand = new CommandBase(HandleSortAnimationsAlphabetically);
        DuplicateFrameCommand = new CommandBase(HandleDuplicateFrame);
    }

    private void HandleSortAnimationsAlphabetically()
    {
        // Sort the items by Name property
        var sorted = VisibleRoot.OrderBy(vm => vm.Name, StringComparer.OrdinalIgnoreCase).ToList();

        // Rearrange the collection to match the sorted order
        for (int i = 0; i < sorted.Count; i++)
        {
            var currentIndex = VisibleRoot.IndexOf(sorted[i]);
            if (currentIndex != i)
            {
                VisibleRoot.Move(currentIndex, i);
            }
        }
    }

    private void HandleAddAnimation()
    {
        var animationChainSave = new AnimationChainSave();
        animationChainSave.Name = "NewAnimation";
        var newAnimationChain = AddAnimationChain(animationChainSave);

        CurrentAnimationChain = newAnimationChain;

        if (this.AchxFilePath?.Extension == "atlas")
        {
            // all animations in an atlas must have exactly 1 frame:
            HandleAddFrame();
        }

    }

    private void HandleAddFrame()
    {
        var currentChain = CurrentAnimationChain;

        ////////////////////////Early out////////////////////////
        if(currentChain == null)
        {
            return;
        }
        ////////////////////////End Early Out/////////////////////

        var frame = new AnimationFrameSave();
        frame.FrameLength = .1f;
        if(this.AchxFilePath?.Extension == "atlas")
        {
            // see if any files are already referenced
        }

        currentChain.AddAnimationFrame(frame);

    }

    private void HandleDuplicateFrame()
    {
        var currentFrame = CurrentAnimationFrame;

        /////////////////////////Early out////////////////////////
        if(currentFrame == null || CurrentAnimationChain == null)
        {
            return;
        }
        /////////////////////////End Early Out/////////////////////

        var frame = FileManager.CloneObject<AnimationFrameSave>(currentFrame.BackingModel);

        AddFrameToAnimationChain(CurrentAnimationChain, frame);

    }

    private void HandleAdjustAllFrameTime()
    {
        // todo - this should create a view based on the viewmodel
        var viewModel = new AnimationChainTimeScaleViewModel(
            this.CurrentAnimationChain);

        var view = new AnimationChainTimeScaleWindow();
        view.DataContext = viewModel;

        var result = view.ShowDialog();

        if(result == true)
        {
            viewModel.ApplyToAnimation();
        }
    }

    private void HandleAnimationChainViewModelCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var shouldSave = false;
        // add any new .achx's
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            var newItems = e.NewItems;

            if (newItems != null)
            {
                foreach (AnimationChainViewModel newItem in newItems)
                {
                    // Is this already contained?
                    if (this.BackingData.AnimationChains.Contains(newItem.BackingModel) == false)
                    {
                        var index = this.VisibleRoot.IndexOf(newItem);

                        this.BackingData.AnimationChains.Insert(index, newItem.BackingModel);
                        shouldSave = true;
                    }
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            var oldItems = e.OldItems;
            if (oldItems != null)
            {
                foreach (AnimationChainViewModel oldItem in oldItems)
                {
                    this.BackingData.AnimationChains.Remove(oldItem.BackingModel);
                    shouldSave = true;
                }
            }
        }
        else if(e.Action == NotifyCollectionChangedAction.Move)
        {
            var oldIndex = e.OldStartingIndex;
            var newIndex = e.NewStartingIndex;
            var itemToMove = this.BackingData.AnimationChains[oldIndex];
            this.BackingData.AnimationChains.RemoveAt(oldIndex);
            this.BackingData.AnimationChains.Insert(newIndex, itemToMove);
            shouldSave = true;
        }

        if (shouldSave)
        {
            _achxManager.SaveCurrentAchx();
        }
    }

    public void SetFrom(AnimationChainListSave animationChainListSave, FilePath achxFilePath, int resolutionWidth, int resolutionHeight)
    {
        VisibleRoot.Clear();

        if (animationChainListSave == null) return;


        AchxFilePath = achxFilePath;
        ResolutionWidth = resolutionWidth;
        ResolutionHeight = resolutionHeight;


        foreach (var animationChain in animationChainListSave.AnimationChains)
        {
            AddAnimationChain(animationChain);
        }
    }

    public AnimationChainViewModel AddAnimationChain(AnimationChainSave animationChainSave)
    {
        return AddAnimationChainInner(animationChainSave, broadcastAddition: true);
    }

    /// <summary>
    /// Adds the argument AnimationChainSave to this view model, and optionally broadcasts that it hs been added.
    /// Broadcast should occur if the AnimationChain is added through user actions. The response to the broadcast will be at 
    /// the plugin level, which will save the file.
    /// </summary>
    /// <param name="animationChainSave">The new AnimationChainSave</param>
    /// <param name="broadcastAddition">Whether to broadcast the addition.</param>
    private AnimationChainViewModel AddAnimationChainInner(AnimationChainSave animationChainSave, bool broadcastAddition)
    {
        var newViewModel = new AnimationChainViewModel(_achxManager);
        newViewModel.SetFrom(animationChainSave, AchxFilePath, ResolutionWidth, ResolutionHeight);
        newViewModel.FrameUpdatedByUi += HandleFrameUpdatedByUi;
        newViewModel.PropertyChanged += HandlePropertyChanged;
        newViewModel.VisibleChildren.CollectionChanged += HandleAnimationChainCollectionChanged;
        VisibleRoot.Add(newViewModel);

        return newViewModel;
    }

    private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(sender == null || e?.PropertyName == null)
        {
            return;
        }
        var viewModel = (AnimationChainViewModel)sender;
        AnimationChainUpdatedByUi?.Invoke(viewModel, e.PropertyName);

        switch(e.PropertyName)
        {
            case nameof(AnimationChainViewModel.Name):
                var didChange = viewModel.ApplyTo(viewModel.BackingModel);
                if(didChange)
                {
                    _achxManager.SaveCurrentAchx();
                }
                break;
        }
    }

    private void HandleFrameUpdatedByUi(AnimationFrameViewModel frame, string property)
    {
        FrameUpdatedByUi?.Invoke(frame, property);
        _achxManager.SaveCurrentAchx();
    }

    private void HandleAnimationChainCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        AnimationChainCollectionChanged?.Invoke((AnimationChainViewModel)sender, e);



        _achxManager.SaveCurrentAchx();
    }

    public void HandleDelete()
    {
        if(SelectedShape != null)
        {
            GlueCommands.Self.DialogCommands.ShowYesNoMessageBox($"Delete {SelectedShape} in {CurrentAnimationFrame}?",
                yesAction: HandleDeleteConfirm);
        }
        if (CurrentAnimationFrame != null)
        {
            GlueCommands.Self.DialogCommands.ShowYesNoMessageBox($"Delete {CurrentAnimationFrame}?",
                yesAction: HandleDeleteConfirm);
        }
        else if(CurrentAnimationChain != null)
        {
            GlueCommands.Self.DialogCommands.ShowYesNoMessageBox($"Delete {CurrentAnimationChain}? ",
                yesAction: HandleDeleteConfirm);
        }
    }

    private void HandleDeleteConfirm()
    {
        if(SelectedShape != null)
        {
            var parent = SelectedShape.Parent;

            parent.VisibleChildren.Remove(SelectedShape);

            // need to remove it....
        }
        else if (CurrentAnimationFrame != null)
        {
            var parent = CurrentAnimationFrame.Parent;
            parent.VisibleChildren.Remove(CurrentAnimationFrame);
            CurrentAnimationFrame = null;
            _achxManager.SaveCurrentAchx();
        }
        else if(CurrentAnimationChain != null)
        {
            VisibleRoot.Remove(CurrentAnimationChain);
            CurrentAnimationChain = null;
            _achxManager.SaveCurrentAchx();
        }
    }

    internal void HandleExportClicked()
    {
        var filter = "AnimationEditor (.achx)|*.achx|Spine Atlas|*.atlas";
        var result = _dialogCommands.ShowSaveDialog(filter);

        if(result.DialogResult == System.Windows.Forms.DialogResult.OK)
        {
            var filePath = result.FilePath;

            SaveCurrentAchx(filePath);
        }
    }

    public void HandlePaste(string copiedXml, CopiedType copiedType)
    {
        /////////////early out/////////////////////
        if (string.IsNullOrEmpty(copiedXml))
        {
            return;
        }
        //////////end early out////////////////////

        switch (copiedType)
        {
            case CopiedType.AnimationChains:
                {
                    var deserialized = FileManager.XmlDeserializeFromString<AnimationChainSave>(copiedXml);

                    if (deserialized != null)
                    {
                        while (VisibleRoot.Any(item => item.Name == deserialized.Name))
                        {
                            deserialized.Name = StringFunctions.IncrementNumberAtEnd(deserialized.Name);
                        }

                        var newVm = AddAnimationChain(deserialized);

                        if (CurrentAnimationFrame != null)
                        {
                            CurrentAnimationFrame = null;
                        }
                        CurrentAnimationChain = newVm;
                    }

                }
                break;
            case CopiedType.AnimationFrames:
                {
                    var chainVmToAddTo = CurrentAnimationChain;
                    if (chainVmToAddTo != null)
                    {
                        var deserialized = FileManager.XmlDeserializeFromString<AnimationFrameSave>(copiedXml);
                        AddFrameToAnimationChain(chainVmToAddTo, deserialized);
                    }
                }

                break;
        }

    }

    private void AddFrameToAnimationChain(AnimationChainViewModel chainVmToAddTo, AnimationFrameSave animationFrame)
    {
        // add it to the backing model first, so that when it's added to the VM, the save picks up the add:
        chainVmToAddTo.BackingModel.Frames.Add(animationFrame);

        var newFrame = chainVmToAddTo.AddAnimationFrame(animationFrame);
        CurrentAnimationFrame = newFrame;
    }

    public void LoadAchx(FilePath filePath)
    {
        var previouslySelected = CurrentAnimationChain;

        AnimationChainListSave animationChainListSave = null;
        if (filePath.Exists() == true)
        {
            if (filePath.Extension == "atlas")
            {
                var converter = new AtlasConverter();

                var contents = System.IO.File.ReadAllText(filePath.FullPath);

                animationChainListSave = converter.DeserializeAtlas(contents);

                if (animationChainListSave == null)
                {
                    if (string.IsNullOrEmpty(contents))
                    {
                        // it's an empty atlas file
                        animationChainListSave = new AnimationChainListSave();
                        animationChainListSave.FileName = filePath.FullPath;

                    }
                    else
                    {
                        GlueCommands.Self.PrintError("Error loading atlas file into .achx:\n" + contents);
                    }
                }

            }
            else
            {
                try
                {
                    animationChainListSave = AnimationChainListSave.FromFile(filePath.FullPath);
                }
                catch (Exception ex)
                {
                    GlueCommands.Self.PrintError($"Error loading .achx file {filePath.FullPath}:\n{ex.Message}");
                }
            }

            if(animationChainListSave != null )
            {
                // this tool requires pixel coords:
                animationChainListSave.CoordinateType = FlatRedBall.Graphics.TextureCoordinateType.Pixel;
            }

            FilePath aesjFile = filePath.RemoveExtension() + ".aesj";

            if(aesjFile.Exists())
            {
                try
                {
                    var allText = FileManager.FromFileText(aesjFile.FullPath);

                    var deserialized = JsonConvert.DeserializeObject<AESettingsSave>(allText);

                    _textureCoordinateSelectionViewModel.SnapChecked = deserialized.SnapToGrid;
                    _textureCoordinateSelectionViewModel.CellHeight = (ushort)deserialized.GridSize;
                    _textureCoordinateSelectionViewModel.CellWidth = (ushort)deserialized.GridSize;
                }
                catch
                {
                    // no biggie
                }
            }
        }


        BackingData = animationChainListSave;
        AchxFilePath = filePath;
    }

    public void SaveCurrentAchx(FilePath? forcedFilePath = null)
    {
        // now save it:
        var animationChain = BackingData;
        var filePath = forcedFilePath ?? AchxFilePath;

        _fileCommands.IgnoreChangeOnFileUntil(
            filePath, DateTimeOffset.Now.AddSeconds(2));
        try
        {
            GlueCommands.Self.TryMultipleTimes(() =>
            {
                if (filePath.Extension == "atlas")
                {
                    //var converter = new AtlasConverter();
                    //var contents = converter.SerializeToAtlas(animationChain);
                    //System.IO.File.WriteAllText(filePath.FullPath, contents);

                    var model = BackingData;

                    var converter = new AtlasConverter();

                    var result = converter.SerializeAtlas(model, filePath.GetDirectoryContainingThis().FullPath);

                    var fileName = AchxFilePath.RemoveExtension() + ".atlas";

                    _fileCommands.SaveIfDiffers(filePath, result, ignoreNextChange: true);


                }
                else
                {
                    animationChain.Save(filePath.FullPath);
                }

                var companionFile = filePath.RemoveExtension() + ".aesj";

                var properties = new AESettingsSave()
                {
                    SnapToGrid = this.IsSnappingChecked,
                    GridSize = this.SnappingSize,
                };

                var serialized = JsonConvert.SerializeObject(properties, Formatting.Indented);
                _fileCommands.SaveIfDiffers(companionFile, serialized, ignoreNextChange: true);
            });
        }
        catch (Exception ex)
        {
            GlueCommands.Self.PrintError(ex.ToString());
        }
    }

    internal void MoveSelectionUp()
    {

        if(CurrentAnimationFrame != null)
        {
            var parent = CurrentAnimationFrame.Parent;
            var index = parent.VisibleChildren.IndexOf(CurrentAnimationFrame);

            if(index > 0)
            {
                parent.VisibleChildren.Move(index, index - 1);
                CurrentAnimationFrame = parent.VisibleChildren[index - 1];
            }
        }
        else if(CurrentAnimationChain != null)
        {
            var index = VisibleRoot.IndexOf(CurrentAnimationChain);
            if(index > 0)
            {
                VisibleRoot.Move(index, index - 1);
                CurrentAnimationChain = VisibleRoot[index - 1];
            }
        }
    }

    internal void MoveSelectionDown()
    {
        if (CurrentAnimationFrame != null)
        {
            var parent = CurrentAnimationFrame.Parent;

            var index = parent.VisibleChildren.IndexOf(CurrentAnimationFrame);
            if (index < parent.VisibleChildren.Count - 1)
            {
                parent.VisibleChildren.Move(index, index + 1);
                CurrentAnimationFrame = parent.VisibleChildren[index + 1];
            }
        }
        else if (CurrentAnimationChain != null)
        {
            var index = VisibleRoot.IndexOf(CurrentAnimationChain);
            if (index < VisibleRoot.Count - 1)
            {
                VisibleRoot.Move(index, index + 1);
                CurrentAnimationChain = VisibleRoot[index + 1];
            }
        }
    }
}
