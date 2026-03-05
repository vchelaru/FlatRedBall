using GlueFormsCore.FormHelpers;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace OfficialPlugins.TreeViewPlugin.Logic;

/// <summary>
/// Describes a single item that should appear in the tree-view right-click menu.
/// Returned by <see cref="FlatRedBall.Glue.FormHelpers.RightClickHelper.GetItemDescriptors"/>.
///
/// Tests inspect <see cref="Text"/> and <see cref="IsSeparator"/> to assert which items appear
/// for a given node type — without standing up the editor or calling Initialize().
/// </summary>
internal sealed class RightClickItemDescriptor
{
    /// <summary>Sentinel for a menu separator line.</summary>
    public static RightClickItemDescriptor Separator => new() { Text = "-", IsSeparator = true };

    /// <summary>The label that will appear in the menu. Always set, even for pre-created items.</summary>
    public string Text { get; init; } = "";

    /// <summary>True when this descriptor represents a separator line (Text == "-").</summary>
    public bool IsSeparator { get; init; }

    /// <summary>
    /// The action to invoke when the item is clicked.
    /// Null for pre-created items whose Click handler was already wired in Initialize().
    /// </summary>
    public Action? Handler { get; init; }

    /// <summary>Optional icon shown beside the menu item text.</summary>
    public Image? Image { get; init; }

    /// <summary>
    /// Reference to a pre-created <see cref="GeneralToolStripMenuItem"/> from
    /// <c>RightClickHelper.Initialize()</c>. When set, the materialization step updates
    /// <c>PreCreatedItem.Text = this.Text</c> before adding it to the menu — which handles
    /// contextual labels (e.g., "Export Entity" vs. the item's default "Export Screen").
    ///
    /// Null in test scenarios where Initialize() has not been called; tests rely on
    /// <see cref="Text"/> instead.
    /// </summary>
    public GeneralToolStripMenuItem? PreCreatedItem { get; init; }

    /// <summary>
    /// Child descriptors for a nested submenu (e.g., the "Copy Name..." submenu).
    /// When non-null, the materialization step creates a parent item and adds subitems to its
    /// DropDownItems.
    /// </summary>
    public IReadOnlyList<RightClickItemDescriptor>? SubItems { get; init; }
}
