using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using AnimationEditor.Core;
using AnimationEditor.Core.CommandsAndState;
using AnimationEditor.Core.IO;
using AnimationEditor.Core.ViewModels;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using Xunit;

namespace AnimationEditor.App.Tests;

/// <summary>
/// Headless Avalonia tests for TV05 (multi-select TreeView) and TV06 (context menu items).
///
/// Each test creates a fresh MainWindow headlessly, resets singleton data state,
/// and exercises the tree view in isolation.
/// </summary>
public class HeadlessTreeViewTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a headless <see cref="MainWindow"/>, shows it, and returns it.
    /// Callers must call <see cref="Window.Close"/> when done.
    /// </summary>
    private static MainWindow CreateWindow()
    {
        // Reset data state so each test starts clean.
        // Note: we deliberately do NOT override DoOnUiThread — the window sets it
        // to InvokeAsync which is correct when running on the Avalonia UI thread.
        ProjectManager.Self.AnimationChainListSave = new AnimationChainListSave();
        ProjectManager.Self.FileName = null;
        SelectedState.Self.SelectedChain = null;
        AppCommands.Self.ConfirmAsync = (_, _) => Task.FromResult(true);
        AppCommands.Self.FileDialogService = NullFileDialogService.Instance;

        var window = new MainWindow();
        window.Show();
        return window;
    }

    /// <summary>Returns the AnimTree control from a window.</summary>
    private static TreeView GetTree(MainWindow w)
        => w.FindControl<TreeView>("AnimTree")
           ?? throw new InvalidOperationException("AnimTree control not found");

    /// <summary>
    /// Gets the <c>_treeRoots</c> ObservableCollection via the tree's ItemsSource.
    /// </summary>
    private static ObservableCollection<TreeNodeVm> GetRoots(TreeView tree)
        => (ObservableCollection<TreeNodeVm>)(tree.ItemsSource
           ?? throw new InvalidOperationException("AnimTree has no ItemsSource"));

    /// <summary>
    /// Invokes the private <c>OnTreeContextMenuOpening</c> method to populate
    /// the context menu without needing a real pointer event.
    /// </summary>
    private static void TriggerContextMenuOpening(MainWindow window)
    {
        var method = typeof(MainWindow).GetMethod(
            "OnTreeContextMenuOpening",
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("OnTreeContextMenuOpening not found via reflection");

        method.Invoke(window, [null, new CancelEventArgs()]);
    }

    private static List<string?> ContextMenuHeaders(MainWindow window)
    {
        var tree = GetTree(window);
        return tree.ContextMenu!.Items
            .OfType<MenuItem>()
            .Select(m => m.Header?.ToString())
            .ToList();
    }

    // ── TV05: Multi-select ────────────────────────────────────────────────────

    [AvaloniaFact]
    public void TreeView_SelectionMode_IsMultiple()
    {
        var window = CreateWindow();
        try
        {
            var tree = GetTree(window);
            Assert.Equal(SelectionMode.Multiple, tree.SelectionMode);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TreeView_SelectTwoNodes_BothAppearInSelectedItems()
    {
        var window = CreateWindow();
        try
        {
            var tree  = GetTree(window);
            var roots = GetRoots(tree);

            var vm1 = new TreeNodeVm { Header = "Walk", Data = new AnimationChainSave { Name = "Walk" } };
            var vm2 = new TreeNodeVm { Header = "Run",  Data = new AnimationChainSave { Name = "Run"  } };
            roots.Add(vm1);
            roots.Add(vm2);

            tree.SelectedItems!.Add(vm1);
            tree.SelectedItems.Add(vm2);

            Assert.Equal(2, tree.SelectedItems.Count);
            Assert.Contains(vm1, tree.SelectedItems!.Cast<TreeNodeVm>());
            Assert.Contains(vm2, tree.SelectedItems.Cast<TreeNodeVm>());
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TreeView_DeselectOne_LeavesSingleItemSelected()
    {
        var window = CreateWindow();
        try
        {
            var tree  = GetTree(window);
            var roots = GetRoots(tree);

            var vm1 = new TreeNodeVm { Header = "A", Data = new AnimationChainSave { Name = "A" } };
            var vm2 = new TreeNodeVm { Header = "B", Data = new AnimationChainSave { Name = "B" } };
            roots.Add(vm1);
            roots.Add(vm2);

            tree.SelectedItems!.Add(vm1);
            tree.SelectedItems.Add(vm2);
            tree.SelectedItems.Remove(vm1);

            Assert.Single(tree.SelectedItems!.Cast<TreeNodeVm>());
            Assert.Contains(vm2, tree.SelectedItems.Cast<TreeNodeVm>());
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TreeView_ClearSelectedItems_ResultsInEmptySelection()
    {
        var window = CreateWindow();
        try
        {
            var tree  = GetTree(window);
            var roots = GetRoots(tree);

            var vm1 = new TreeNodeVm { Header = "A", Data = new AnimationChainSave { Name = "A" } };
            roots.Add(vm1);
            tree.SelectedItems!.Add(vm1);
            Assert.Single(tree.SelectedItems!.Cast<TreeNodeVm>());

            tree.SelectedItems.Clear();
            Assert.Empty(tree.SelectedItems.Cast<TreeNodeVm>());
        }
        finally { window.Close(); }
    }

    // ── TV06: Context menu items ──────────────────────────────────────────────

    [AvaloniaFact]
    public void ContextMenu_NoSelection_ContainsAddAnimationChain()
    {
        var window = CreateWindow();
        try
        {
            var tree = GetTree(window);
            tree.SelectedItem = null;  // ensure nothing selected

            TriggerContextMenuOpening(window);

            Assert.Contains("Add AnimationChain", ContextMenuHeaders(window));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContextMenu_ChainSelected_ContainsDeleteChain()
    {
        var window = CreateWindow();
        try
        {
            var tree  = GetTree(window);
            var roots = GetRoots(tree);

            var chain = new AnimationChainSave { Name = "Walk" };
            var vm    = new TreeNodeVm { Header = "Walk", Data = chain };
            roots.Add(vm);
            tree.SelectedItem = vm;

            TriggerContextMenuOpening(window);

            Assert.Contains("Delete AnimationChain", ContextMenuHeaders(window));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContextMenu_ChainSelected_ContainsAddFrame()
    {
        var window = CreateWindow();
        try
        {
            var tree  = GetTree(window);
            var roots = GetRoots(tree);

            var chain = new AnimationChainSave { Name = "Run" };
            var vm    = new TreeNodeVm { Header = "Run", Data = chain };
            roots.Add(vm);
            tree.SelectedItem = vm;

            TriggerContextMenuOpening(window);

            Assert.Contains("Add Frame", ContextMenuHeaders(window));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContextMenu_ChainSelected_ContainsMoveAndFlipItems()
    {
        var window = CreateWindow();
        try
        {
            var tree  = GetTree(window);
            var roots = GetRoots(tree);

            var chain = new AnimationChainSave { Name = "Idle" };
            var vm    = new TreeNodeVm { Header = "Idle", Data = chain };
            roots.Add(vm);
            tree.SelectedItem = vm;

            TriggerContextMenuOpening(window);

            var headers = ContextMenuHeaders(window);
            Assert.Contains("Flip Horizontally", headers);
            Assert.Contains("Flip Vertically",   headers);
            Assert.Contains("^  Move Up",         headers);
            Assert.Contains("v  Move Down",        headers);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContextMenu_FrameSelected_ContainsDeleteFrame()
    {
        var window = CreateWindow();
        try
        {
            var tree  = GetTree(window);
            var roots = GetRoots(tree);

            var frame = new AnimationFrameSave { TextureName = "Tex.png", ShapeCollectionSave = new ShapeCollectionSave() };
            var vm    = new TreeNodeVm { Header = "Frame 0", Data = frame };
            roots.Add(vm);
            tree.SelectedItem = vm;

            TriggerContextMenuOpening(window);

            Assert.Contains("Delete Frame", ContextMenuHeaders(window));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContextMenu_FrameSelected_ContainsShapeItems()
    {
        var window = CreateWindow();
        try
        {
            var tree  = GetTree(window);
            var roots = GetRoots(tree);

            var frame = new AnimationFrameSave { TextureName = "Tex.png", ShapeCollectionSave = new ShapeCollectionSave() };
            var vm    = new TreeNodeVm { Header = "Frame 0", Data = frame };
            roots.Add(vm);
            tree.SelectedItem = vm;

            TriggerContextMenuOpening(window);

            var headers = ContextMenuHeaders(window);
            Assert.Contains("Add AxisAlignedRectangle", headers);
            Assert.Contains("Add Circle",               headers);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContextMenu_RectSelected_ContainsDeleteRectangle()
    {
        var window = CreateWindow();
        try
        {
            var tree  = GetTree(window);
            var roots = GetRoots(tree);

            var rect = new AxisAlignedRectangleSave();
            var vm   = new TreeNodeVm { Header = "Rect", Data = rect };
            roots.Add(vm);
            tree.SelectedItem = vm;

            TriggerContextMenuOpening(window);

            Assert.Contains("Delete Rectangle", ContextMenuHeaders(window));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContextMenu_CircleSelected_ContainsDeleteCircle()
    {
        var window = CreateWindow();
        try
        {
            var tree  = GetTree(window);
            var roots = GetRoots(tree);

            var circle = new CircleSave();
            var vm     = new TreeNodeVm { Header = "Circle", Data = circle };
            roots.Add(vm);
            tree.SelectedItem = vm;

            TriggerContextMenuOpening(window);

            Assert.Contains("Delete Circle", ContextMenuHeaders(window));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContextMenu_AlwaysContainsSortAlphabetically()
    {
        var window = CreateWindow();
        try
        {
            // Test both: no selection case
            var tree = GetTree(window);
            tree.SelectedItem = null;
            TriggerContextMenuOpening(window);
            Assert.Contains("Sort Animations Alphabetically", ContextMenuHeaders(window));
        }
        finally { window.Close(); }
    }
}
