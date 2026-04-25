using System.Collections.ObjectModel;

namespace AnimationEditor.Core.ViewModels;

/// <summary>
/// Lightweight view-model for a single node in the animation tree.
/// The <see cref="Data"/> field holds the underlying data object
/// (AnimationChainSave, AnimationFrameSave, AxisAlignedRectangleSave, CircleSave).
/// </summary>
public class TreeNodeVm
{
    public string Header { get; set; } = string.Empty;

    /// <summary>Underlying data object — AnimationChainSave, AnimationFrameSave, etc.</summary>
    public object? Data { get; set; }

    /// <summary>
    /// Whether this node is expanded in the tree view.
    /// Persisted in AESettingsSave.ExpandedNodes for chain-level nodes.
    /// </summary>
    public bool IsExpanded { get; set; }

    public ObservableCollection<TreeNodeVm> Children { get; } = new();
}
