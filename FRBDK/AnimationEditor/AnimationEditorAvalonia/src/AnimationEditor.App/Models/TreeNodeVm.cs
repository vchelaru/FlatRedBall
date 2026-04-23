using System.Collections.ObjectModel;

namespace AnimationEditor.App.Models;

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

    public ObservableCollection<TreeNodeVm> Children { get; } = new();
}
