using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.IO;
using OfficialPlugins.AnimationChainPlugin.Managers;
using OfficialPlugins.Common.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels;

internal class AchxViewModel : ViewModel
{
    #region Fields/Properties

    public ZoomViewModel TopWindowZoom { get; set; }
    public ZoomViewModel BottomWindowZoom { get; set; }

    private readonly AchxManager _achxManager;
    private readonly TextureCoordinateSelectionViewModel _textureCoordinateSelectionViewModel;

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
        TextureCoordinateSelectionViewModel textureCoordinateSelectionViewModel)
    {
        _achxManager = achxManager;
        _textureCoordinateSelectionViewModel = textureCoordinateSelectionViewModel;

        Settings = new SettingsViewModel();
        TopWindowZoom = new ZoomViewModel();
        BottomWindowZoom = new ZoomViewModel();

        TopWindowZoom.CurrentZoomPercent = 100;
        BottomWindowZoom.CurrentZoomPercent = 100;

        VisibleRoot = new ObservableCollection<AnimationChainViewModel>();
        VisibleRoot.CollectionChanged += HandleAnimationChainViewModelCollectionChanged;

        AddAnimationCommand = new CommandBase(HandleAddAnimation);
        AddFrameCommand = new CommandBase(HandleAddFrame);
    }

    private void HandleAddAnimation()
    {
        // temp - just add it to make sure it works:
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

        if(this.AchxFilePath?.Extension == "atlas")
        {
            // see if any files are already referenced
        }

        currentChain.AddAnimationFrame(frame);

    }

    private void HandleAnimationChainViewModelCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

        if(shouldSave)
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
        var newViewModel = new AnimationChainViewModel();
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
        if (CurrentAnimationFrame != null)
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
}
