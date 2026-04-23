using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using AnimationEditor.Core.DragDrop;
using AnimationEditor.Core.Models;
using AnimationEditor.App.Models;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FilePath = FlatRedBall.IO.FilePath;

namespace AnimationEditor.App;

public partial class MainWindow : Window
{
    private AppSettingsModel _appSettings = new();

    private FilePath SettingsFilePath =>
        (FilePath)(Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\AESettings.json");

    public MainWindow()
    {
        InitializeComponent();

        WireAppCommands();
        LoadSettingsFile();
        WireMenuEvents();
        WireWireframeToolbar();
        WireWireframeControl();
        WirePreviewControls();
        WireTreeView();

        Opened += OnOpened;
    }

    // ── Startup ───────────────────────────────────────────────────────────────

    private void OnOpened(object? sender, EventArgs e)
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length >= 2 && File.Exists(args[1]))
        {
            LoadAnimationFile(args[1]);
        }
        else
        {
            ProjectManager.Self.AnimationChainListSave =
                new FlatRedBall.Content.AnimationChain.AnimationChainListSave();
        }
    }

    // ── AppCommands wiring ────────────────────────────────────────────────────

    private void WireAppCommands()
    {
        AppCommands.Self.DoOnUiThread = action => Dispatcher.UIThread.InvokeAsync(action);
        AppCommands.Self.ConfirmAsync = ShowConfirmDialogAsync;

        // File dialog service
        AppCommands.Self.FileDialogService = new Services.AvaloniaFileDialogService(this);
        AppCommands.Self.SaveAsCompleted  += path =>
        {
            _appSettings.AddFile(new FilePath(path));
            SaveSettingsFile();
            RefreshRecentFiles();
            UpdateTitle();
        };

        // Tree events — fully wired (WireTreeView connects these after tree is constructed)
        AppCommands.Self.RefreshTreeViewRequested           += () => Dispatcher.UIThread.InvokeAsync(RefreshTreeView);
        AppCommands.Self.RefreshChainNodeRequested          += c  => Dispatcher.UIThread.InvokeAsync(() => RefreshChainNode(c));
        AppCommands.Self.RefreshFrameNodeRequested          += f  => Dispatcher.UIThread.InvokeAsync(() => RefreshFrameNode(f));
        AppCommands.Self.RefreshAnimationFrameDisplayRequested += () => { };
        // RefreshWireframeRequested is handled by WireframeControl directly

        ApplicationEvents.Self.AchxLoaded               += HandleAchxLoaded;
        ApplicationEvents.Self.AnimationChainsChanged    += HandleAnimationChainsChanged;
        SelectedState.Self.SelectionChanged              += HandleSelectionChanged;
    }

    // ── Wireframe toolbar wiring ──────────────────────────────────────────────

    private void WireWireframeToolbar()
    {
        TextureCombo.SelectionChanged += OnTextureComboChanged;
        MagicWandToggle.IsCheckedChanged += OnMagicWandToggled;
        SnapToGridCheck.IsCheckedChanged += OnSnapToGridChanged;
        GridSizeInput.ValueChanged += OnGridSizeChanged;
        ZoomCombo.SelectionChanged += OnZoomComboChanged;

        // Apply initial grid state
        WireframeCtrl.SetGrid(false, 16);
    }

    private void OnTextureComboChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TextureCombo.SelectedItem is string path)
            WireframeCtrl.LoadTexture(path);
    }

    private void OnMagicWandToggled(object? sender, RoutedEventArgs e)
    {
        WireframeCtrl.IsMagicWandMode = MagicWandToggle.IsChecked == true;
    }

    private void OnSnapToGridChanged(object? sender, RoutedEventArgs e)
    {
        WireframeCtrl.SetGrid(
            SnapToGridCheck.IsChecked == true,
            (int)(GridSizeInput.Value ?? 16));
    }

    private void OnGridSizeChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (SnapToGridCheck.IsChecked == true)
            WireframeCtrl.SetGrid(true, (int)(e.NewValue ?? 16));
    }

    private void OnZoomComboChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ZoomCombo.SelectedItem is ComboBoxItem item &&
            item.Content is string text)
        {
            var numStr = text.TrimEnd('%');
            if (int.TryParse(numStr, out int pct))
                WireframeCtrl.SetZoomPercent(pct);
        }
    }

    // ── WireframeControl event wiring ─────────────────────────────────────────

    private void WireWireframeControl()
    {
        WireframeCtrl.FrameRegionChanged    += OnFrameRegionChanged;
        WireframeCtrl.FrameCreatedFromRegion += OnFrameCreatedFromRegion;
    }

    private void OnFrameRegionChanged(AnimationFrameSave frame)
    {
        AppCommands.Self.RefreshTreeNode(frame);
        ApplicationEvents.Self.RaiseAnimationChainsChanged();
    }

    private void OnFrameCreatedFromRegion(int minX, int minY, int maxX, int maxY)
    {
        var chain = SelectedState.Self.SelectedChain;
        if (chain is null) return;
        if (string.IsNullOrEmpty(ProjectManager.Self.FileName)) return;

        var texPath = WireframeCtrl.LoadedTexturePath;
        if (string.IsNullOrEmpty(texPath)) return;

        var (bitmapW, bitmapH) = WireframeCtrl.BitmapSize;
        if (bitmapW == 0 || bitmapH == 0) return;

        string achxFolder = FlatRedBall.IO.FileManager.GetDirectory(ProjectManager.Self.FileName);
        string relPath = FlatRedBall.IO.FileManager.MakeRelative(texPath, achxFolder);

        var frame = new AnimationFrameSave
        {
            TextureName        = relPath,
            LeftCoordinate     = minX / (float)bitmapW,
            RightCoordinate    = maxX / (float)bitmapW,
            TopCoordinate      = minY / (float)bitmapH,
            BottomCoordinate   = maxY / (float)bitmapH,
            FrameLength        = 0.1f,
            ShapeCollectionSave = new FlatRedBall.Content.Math.Geometry.ShapeCollectionSave()
        };

        chain.Frames.Add(frame);
        AppCommands.Self.RefreshTreeNode(chain);
        SelectedState.Self.SelectedFrame = frame;
        ApplicationEvents.Self.RaiseAnimationChainsChanged();
    }

    // ── Core event handlers ───────────────────────────────────────────────────

    private void HandleAchxLoaded(string fileName)
    {
        AppCommands.Self.LoadAnimationChain(fileName);   // triggers RefreshTreeViewRequested

        _appSettings.AddFile(new FilePath(fileName));
        SaveSettingsFile();
        RefreshRecentFiles();
        UpdateTitle();
        RefreshTextureCombo();
    }

    private void HandleAnimationChainsChanged()
    {
        if (!string.IsNullOrEmpty(ProjectManager.Self.FileName))
        {
            AppCommands.Self.SaveCurrentAnimationChainList();
            UpdateTitle();
        }
    }

    private void HandleSelectionChanged()
    {
        // Sync the texture combo to the texture of the currently selected frame/chain
        Dispatcher.UIThread.InvokeAsync(SyncTextureCombo);
        // Sync tree selection
        Dispatcher.UIThread.InvokeAsync(SyncTreeSelection);
    }

    // ── Texture combo helpers ─────────────────────────────────────────────────

    /// <summary>Rebuild the texture dropdown from all frames in the loaded .achx.</summary>
    private void RefreshTextureCombo()
    {
        TextureCombo.Items.Clear();

        var acls = ProjectManager.Self.AnimationChainListSave;
        if (acls is null || string.IsNullOrEmpty(ProjectManager.Self.FileName)) return;

        string achxFolder = FlatRedBall.IO.FileManager.GetDirectory(ProjectManager.Self.FileName);

        var paths = acls.AnimationChains
            .SelectMany(c => c.Frames)
            .Where(f => !string.IsNullOrEmpty(f.TextureName))
            .Select(f => new FilePath(achxFolder + f.TextureName).Standardized)
            .Union(ProjectManager.Self.ReferencedPngs.Select(p => p.Standardized))
            .Distinct()
            .ToList();

        foreach (var p in paths)
            TextureCombo.Items.Add(p);

        if (paths.Count > 0)
        {
            TextureCombo.SelectedIndex = 0;
            WireframeCtrl.LoadTexture(paths[0]);
        }
    }

    /// <summary>Sync the combo selection to whichever texture the selected frame uses.</summary>
    private void SyncTextureCombo()
    {
        string? texPath = null;

        var frame = SelectedState.Self.SelectedFrame;
        if (frame != null && !string.IsNullOrEmpty(frame.TextureName) &&
            !string.IsNullOrEmpty(ProjectManager.Self.FileName))
        {
            string achxFolder = FlatRedBall.IO.FileManager.GetDirectory(ProjectManager.Self.FileName);
            texPath = new FilePath(achxFolder + frame.TextureName).Standardized;
        }

        if (texPath != null && TextureCombo.Items.Contains(texPath))
        {
            if (TextureCombo.SelectedItem as string != texPath)
            {
                TextureCombo.SelectedItem = texPath;
                // Selection-change event fires LoadTexture automatically
            }
        }
        else if (texPath != null)
        {
            WireframeCtrl.LoadTexture(texPath);
        }
    }

    // ── Menu wiring ───────────────────────────────────────────────────────────

    private void WireMenuEvents()
    {
        MenuNew.Click    += OnNewClick;
        MenuLoad.Click   += OnLoadClick;
        MenuSave.Click   += OnSaveClick;
        MenuSaveAs.Click += OnSaveAsClick;
        MenuAbout.Click  += OnAboutClick;

        RefreshRecentFiles();
    }

    private void RefreshRecentFiles()
    {
        MenuLoadRecent.Items.Clear();
        foreach (var file in _appSettings.RecentFiles)
        {
            var item = new MenuItem { Header = file };
            var captured = file;
            item.Click += (_, _) => LoadAnimationFile(captured);
            MenuLoadRecent.Items.Add(item);
        }
    }

    // ── File menu handlers ────────────────────────────────────────────────────

    private void OnNewClick(object? sender, RoutedEventArgs e)
    {
        ProjectManager.Self.AnimationChainListSave =
            new FlatRedBall.Content.AnimationChain.AnimationChainListSave();
        ProjectManager.Self.FileName = null;
        _ = AppCommands.Self.SaveCurrentAnimationChainListAsync();
    }

    private void OnLoadClick(object? sender, RoutedEventArgs e) => _ = LoadAsync();

    private async Task LoadAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Animation Chain",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Animation Chain") { Patterns = new[] { "*.achx" } }
            }
        });

        if (files.Count > 0)
            LoadAnimationFile(files[0].Path.LocalPath);
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (ProjectManager.Self.AnimationChainListSave is null) return;

        if (string.IsNullOrEmpty(ProjectManager.Self.FileName))
            _ = AppCommands.Self.SaveCurrentAnimationChainListAsync();
        else
        {
            AppCommands.Self.SaveCurrentAnimationChainList();
            UpdateTitle();
        }
    }

    private void OnSaveAsClick(object? sender, RoutedEventArgs e) =>
        _ = AppCommands.Self.SaveCurrentAnimationChainListAsync();

    private void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        _ = new Window
        {
            Title = "About AnimationEditor",
            Width = 320,
            Height = 130,
            Content = new TextBlock
            {
                Text = "AnimationEditor — Avalonia Port\n© FlatRedBall Contributors",
                Margin = new Avalonia.Thickness(16),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            }
        }.ShowDialog(this);
    }

    // ── Preview controls wiring ───────────────────────────────────────────────

    private void WirePreviewControls()
    {
        OnionSkinToggle.IsCheckedChanged += (_, _) =>
            PreviewCtrl.ShowOnionSkin = OnionSkinToggle.IsChecked == true;

        ShowGuidesCheck.IsCheckedChanged += (_, _) =>
            PreviewCtrl.ShowGuides = ShowGuidesCheck.IsChecked == true;

        PreviewZoomCombo.SelectionChanged += (_, _) =>
        {
            if (PreviewZoomCombo.SelectedItem is ComboBoxItem item &&
                item.Content is string text)
            {
                var numStr = text.TrimEnd('%');
                if (int.TryParse(numStr, out int pct))
                    PreviewCtrl.SetZoomPercent(pct);
            }
        };
    }

    // ── Tree view ─────────────────────────────────────────────────────────────

    private readonly ObservableCollection<TreeNodeVm> _treeRoots = new();

    private void WireTreeView()
    {
        AnimTree.ItemsSource = _treeRoots;
        DragDrop.SetAllowDrop(AnimTree, true);

        // Selection changes in the tree → SelectedState
        AnimTree.SelectionChanged += OnTreeSelectionChanged;
        DragDrop.AddDragOverHandler(AnimTree, OnTreeDragOver);
        DragDrop.AddDropHandler(AnimTree, OnTreeDrop);

        // Context menu
        var cm = new ContextMenu();
        cm.Opening += OnTreeContextMenuOpening;
        AnimTree.ContextMenu = cm;

        // "Add Chain" button under the tree
        AddChainBtn.Click += (_, _) =>
        {
            if (ProjectManager.Self.AnimationChainListSave is null)
                ProjectManager.Self.AnimationChainListSave = new AnimationChainListSave();
            AppCommands.Self.AddAnimationChain();
        };
    }

    private void OnTreeDragOver(object? sender, DragEventArgs e)
    {
        var firstFile = GetFirstDroppedFilePath(e);
        if (!string.IsNullOrEmpty(firstFile) &&
            string.Equals(Path.GetExtension(firstFile), ".png", StringComparison.OrdinalIgnoreCase))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void OnTreeDrop(object? sender, DragEventArgs e)
    {
        var firstFile = GetFirstDroppedFilePath(e);
        if (string.IsNullOrEmpty(firstFile) || string.IsNullOrEmpty(ProjectManager.Self.FileName))
            return;

        var targetNode = AnimTree.SelectedItem as TreeNodeVm;

        var targetFrame = targetNode?.Data as AnimationFrameSave;
        var targetChain = targetNode?.Data as AnimationChainSave;

        if (targetFrame is not null)
        {
            targetChain = ObjectFinder.Self.GetAnimationChainContaining(targetFrame);
        }

        var result = TextureDropProcessor.ApplyPngDrop(
            targetChain,
            targetFrame,
            firstFile,
            ProjectManager.Self.FileName,
            e.KeyModifiers.HasFlag(KeyModifiers.Control));

        if (result == TextureDropResult.NotApplied)
            return;

        if (targetFrame is not null)
        {
            AppCommands.Self.RefreshTreeNode(targetFrame);
            SelectedState.Self.SelectedFrame = targetFrame;
        }
        else if (targetChain is not null)
        {
            AppCommands.Self.RefreshTreeNode(targetChain);

            if (result == TextureDropResult.CreatedFrame)
            {
                var createdFrame = targetChain.Frames.LastOrDefault();
                if (createdFrame is not null)
                    SelectedState.Self.SelectedFrame = createdFrame;
            }
            else
            {
                SelectedState.Self.SelectedChain = targetChain;
            }
        }

        RefreshTextureCombo();
        ApplicationEvents.Self.RaiseAnimationChainsChanged();
        e.Handled = true;
    }

    private static string? GetFirstDroppedFilePath(DragEventArgs e)
    {
        var first = e.DataTransfer.Items?
            .Select(item => item.TryGetFile())
            .FirstOrDefault(file => file is not null);

        return first?.Path.LocalPath;
    }

    private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AnimTree.SelectedItem is not TreeNodeVm vm) return;

        // Suppress re-entrant loops by checking whether the value changed
        switch (vm.Data)
        {
            case AnimationChainSave chain:
                if (SelectedState.Self.SelectedChain != chain)
                    SelectedState.Self.SelectedChain = chain;
                break;
            case AnimationFrameSave frame:
                if (SelectedState.Self.SelectedFrame != frame)
                    SelectedState.Self.SelectedFrame = frame;
                break;
            case AxisAlignedRectangleSave rect:
                if (SelectedState.Self.SelectedRectangle != rect)
                    SelectedState.Self.SelectedRectangle = rect;
                break;
            case CircleSave circle:
                if (SelectedState.Self.SelectedCircle != circle)
                    SelectedState.Self.SelectedCircle = circle;
                break;
        }
    }

    // ── Tree refresh ──────────────────────────────────────────────────────────

    private void RefreshTreeView()
    {
        var acls = ProjectManager.Self.AnimationChainListSave;

        // Preserve expanded chain names
        var expanded = _treeRoots
            .Where(n => n.Data is AnimationChainSave)
            .Where(n => IsNodeExpanded(n))
            .Select(n => ((AnimationChainSave)n.Data!).Name)
            .ToHashSet();

        _treeRoots.Clear();

        if (acls is null) return;

        foreach (var chain in acls.AnimationChains)
        {
            var node = BuildChainNode(chain);
            _treeRoots.Add(node);
        }

        // Re-select to keep visual state
        SyncTreeSelection();
    }

    private void RefreshChainNode(AnimationChainSave chain)
    {
        var node = FindChainNode(chain);
        if (node is null)
        {
            var newNode = BuildChainNode(chain);
            _treeRoots.Add(newNode);
        }
        else
        {
            node.Header = chain.Name;
            node.Children.Clear();
            foreach (var frame in chain.Frames)
                node.Children.Add(BuildFrameNode(frame, chain));
        }
    }

    private void RefreshFrameNode(AnimationFrameSave frame)
    {
        var chain    = AnimationEditor.Core.ObjectFinder.Self.GetAnimationChainContaining(frame);
        var chainNode = chain is null ? null : FindChainNode(chain);
        if (chainNode is null) return;

        var frameNode = chainNode.Children
            .FirstOrDefault(n => n.Data is AnimationFrameSave f && f == frame);

        if (frameNode is null)
        {
            chainNode.Children.Add(BuildFrameNode(frame, chain!));
        }
        else
        {
            frameNode.Header = BuildFrameHeader(frame);
            // Refresh shapes
            frameNode.Children.Clear();
            if (frame.ShapeCollectionSave is not null)
            {
                foreach (var r in frame.ShapeCollectionSave.AxisAlignedRectangleSaves)
                    frameNode.Children.Add(new TreeNodeVm { Header = r.Name, Data = r });
                foreach (var c in frame.ShapeCollectionSave.CircleSaves)
                    frameNode.Children.Add(new TreeNodeVm { Header = c.Name, Data = c });
            }
        }
    }

    // ── Tree builder helpers ──────────────────────────────────────────────────

    private static TreeNodeVm BuildChainNode(AnimationChainSave chain)
    {
        var node = new TreeNodeVm { Header = chain.Name, Data = chain };
        foreach (var frame in chain.Frames)
            node.Children.Add(BuildFrameNode(frame, chain));
        return node;
    }

    private static TreeNodeVm BuildFrameNode(AnimationFrameSave frame, AnimationChainSave chain)
    {
        var node = new TreeNodeVm { Header = BuildFrameHeader(frame), Data = frame };
        if (frame.ShapeCollectionSave is not null)
        {
            foreach (var r in frame.ShapeCollectionSave.AxisAlignedRectangleSaves)
                node.Children.Add(new TreeNodeVm { Header = r.Name, Data = r });
            foreach (var c in frame.ShapeCollectionSave.CircleSaves)
                node.Children.Add(new TreeNodeVm { Header = c.Name, Data = c });
        }
        return node;
    }

    private static string BuildFrameHeader(AnimationFrameSave frame)
    {
        if (string.IsNullOrEmpty(frame.TextureName))
            return "<UNTEXTURED>";

        string name = System.IO.Path.GetFileName(frame.TextureName);
        return $"{name}";
    }

    private TreeNodeVm? FindChainNode(AnimationChainSave chain) =>
        _treeRoots.FirstOrDefault(n => n.Data is AnimationChainSave c && c == chain);

    private static bool IsNodeExpanded(TreeNodeVm _) => true; // always expand by default

    private void SyncTreeSelection()
    {
        var selFrame = SelectedState.Self.SelectedFrame;
        var selChain = SelectedState.Self.SelectedChain;

        TreeNodeVm? target = null;

        if (selFrame is not null)
        {
            var chainNode = _treeRoots
                .FirstOrDefault(n => n.Data is AnimationChainSave chain &&
                                     chain.Frames.Contains(selFrame));
            if (chainNode is not null)
                target = chainNode.Children
                    .FirstOrDefault(n => n.Data is AnimationFrameSave f && f == selFrame);
        }
        else if (selChain is not null)
        {
            target = FindChainNode(selChain);
        }

        if (target is not null && AnimTree.SelectedItem != target)
            AnimTree.SelectedItem = target;
    }

    // ── Context menu ──────────────────────────────────────────────────────────

    private void OnTreeContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (AnimTree.ContextMenu is null) return;
        AnimTree.ContextMenu.Items.Clear();

        var vm = AnimTree.SelectedItem as TreeNodeVm;

        if (vm?.Data is AxisAlignedRectangleSave rect)
        {
            AddMenuItem("Match Frame Size", () =>
            {
                var frame = SelectedState.Self.SelectedFrame;
                if (frame is not null)
                {
                    AppCommands.Self.MatchRectangleToFrame(rect, frame);
                    AppCommands.Self.RefreshAnimationFrameDisplay();
                    AppCommands.Self.SaveCurrentAnimationChainList();
                }
            });
            AddMenuItem("Delete Rectangle", () =>
                _ = AppCommands.Self.AskToDeleteRectangles(new() { rect }));
        }
        else if (vm?.Data is CircleSave circle)
        {
            AddMenuItem("Delete Circle", () =>
                _ = AppCommands.Self.AskToDeleteCircles(new() { circle }));
        }
        else if (vm?.Data is AnimationFrameSave frame2)
        {
            var chain2 = AnimationEditor.Core.ObjectFinder.Self.GetAnimationChainContaining(frame2);
            if (chain2 is not null)
            {
                AddMenuItem("^^ Move To Top",    () => AppCommands.Self.MoveFrameToTop(frame2, chain2));
                AddMenuItem("^  Move Up",         () => AppCommands.Self.MoveFrame(frame2, chain2, -1));
                AddMenuItem("v  Move Down",        () => AppCommands.Self.MoveFrame(frame2, chain2, +1));
                AddMenuItem("vv Move To Bottom",  () => AppCommands.Self.MoveFrameToBottom(frame2, chain2));
                AddSeparator();
            }
            AddMenuItem("Add AxisAlignedRectangle", () => AppCommands.Self.AddAxisAlignedRectangle(frame2));
            AddMenuItem("Add Circle",               () => AppCommands.Self.AddCircle(frame2));
            AddSeparator();
            AddMenuItem("Delete Frame", () =>
                _ = AppCommands.Self.AskToDeleteFrames(new() { frame2 }));
        }
        else if (vm?.Data is AnimationChainSave chain)
        {
            AddMenuItem("^^ Move To Top",    () => AppCommands.Self.MoveChainToTop(chain));
            AddMenuItem("^  Move Up",         () => AppCommands.Self.MoveChain(chain, -1));
            AddMenuItem("v  Move Down",        () => AppCommands.Self.MoveChain(chain, +1));
            AddMenuItem("vv Move To Bottom",  () => AppCommands.Self.MoveChainToBottom(chain));
            AddSeparator();
            AddMenuItem("Adjust Frame Time…", () => AskAdjustFrameTime(chain));
            AddMenuItem("Flip Horizontally",  () => AppCommands.Self.FlipChainHorizontally(chain));
            AddMenuItem("Flip Vertically",    () => AppCommands.Self.FlipChainVertically(chain));
            AddMenuItem("Invert Frame Order", () => AppCommands.Self.InvertFrameOrder(chain));
            AddSeparator();
            AddMenuItem("Add AnimationChain", () => AppCommands.Self.AddAnimationChain());
            AddMenuItem("Add Frame",          () => AppCommands.Self.AddFrame(chain));
            AddSeparator();
            AddMenuItem("Duplicate (original)",         () => AppCommands.Self.DuplicateChain(chain));
            AddMenuItem("Duplicate (flip horizontally)",() => AppCommands.Self.DuplicateChain(chain, flipH: true));
            AddMenuItem("Duplicate (flip vertically)",  () => AppCommands.Self.DuplicateChain(chain, flipV: true));
            AddSeparator();
            AddMenuItem("Delete AnimationChain",
                () => _ = AppCommands.Self.AskToDeleteAnimationChains(new() { chain }));
        }
        else
        {
            AddMenuItem("Add AnimationChain", () =>
            {
                if (ProjectManager.Self.AnimationChainListSave is null)
                    ProjectManager.Self.AnimationChainListSave = new AnimationChainListSave();
                AppCommands.Self.AddAnimationChain();
            });
        }

        AddSeparator();
        AddMenuItem("Sort Animations Alphabetically",
            () => AppCommands.Self.SortAnimationsAlphabetically());
    }

    private void AddMenuItem(string header, Action onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => onClick();
        AnimTree.ContextMenu!.Items.Add(item);
    }

    private void AddSeparator() =>
        AnimTree.ContextMenu!.Items.Add(new Separator());

    private void AskAdjustFrameTime(AnimationChainSave chain)
    {
        // Simple inline dialog asking for a float value
        var dialog = new Window
        {
            Title  = "Adjust All Frame Time",
            Width  = 320,
            Height = 140,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var input = new NumericUpDown
        {
            Value           = (decimal)(chain.Frames.FirstOrDefault()?.FrameLength ?? 0.1f),
            Minimum         = 0.001m,
            Maximum         = 60m,
            Increment       = 0.05m,
            FormatString    = "0.000",
            Width           = 140
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(12), Spacing = 10 };
        panel.Children.Add(new TextBlock { Text = "Frame length (seconds):" });
        panel.Children.Add(input);

        var okBtn = new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        panel.Children.Add(okBtn);
        dialog.Content = panel;

        okBtn.Click += (_, _) =>
        {
            if (input.Value.HasValue)
                AppCommands.Self.SetAllFrameLengths(chain, (float)input.Value.Value);
            dialog.Close();
        };

        _ = dialog.ShowDialog(this);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void LoadAnimationFile(string fileName)
    {
        if (!string.IsNullOrEmpty(fileName))
            ApplicationEvents.Self.CallAchxLoaded(fileName);
    }

    private void UpdateTitle()
    {
        Title = string.IsNullOrEmpty(ProjectManager.Self.FileName)
            ? "AnimationEditor"
            : $"AnimationEditor - {ProjectManager.Self.FileName}";
    }

    private void LoadSettingsFile()
    {
        try
        {
            if (SettingsFilePath.Exists())
            {
                var contents = File.ReadAllText(SettingsFilePath.FullPath);
                _appSettings = JsonConvert.DeserializeObject<AppSettingsModel>(contents)
                               ?? new AppSettingsModel();
            }
        }
        catch
        {
            _appSettings = new AppSettingsModel();
        }
    }

    private void SaveSettingsFile()
    {
        try
        {
            File.WriteAllText(SettingsFilePath.FullPath,
                JsonConvert.SerializeObject(_appSettings, Formatting.Indented));
        }
        catch (IOException)
        {
            // File in use — ignore
        }
    }

    private async Task<bool> ShowConfirmDialogAsync(string message, string title)
    {
        var tcs = new TaskCompletionSource<bool>();

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        var buttons = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        var yesBtn = new Button { Content = "Yes" };
        var noBtn  = new Button { Content = "No" };
        yesBtn.Click += (_, _) => { tcs.TrySetResult(true);  dialog.Close(); };
        noBtn.Click  += (_, _) => { tcs.TrySetResult(false); dialog.Close(); };
        buttons.Children.Add(yesBtn);
        buttons.Children.Add(noBtn);
        panel.Children.Add(buttons);

        dialog.Content = panel;
        dialog.Closed += (_, _) => tcs.TrySetResult(false);

        await dialog.ShowDialog(this);
        return await tcs.Task;
    }
}
