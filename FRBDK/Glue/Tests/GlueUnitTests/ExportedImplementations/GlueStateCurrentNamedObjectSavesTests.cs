using System.Collections.Generic;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.ExportedImplementations;

/// <summary>
/// GitHub issue #2149: selecting an object in the running game (edit mode) sends Glue a
/// NamedObjectSave the game reported as clicked. CommandReceiver.Convert resolves it by name against
/// the loaded project and, if it can't find a match, adds a null entry to the list instead of filtering
/// it out (see also #2131/#2133, which fixed a related but different symptom of this same null entry).
/// That null flows into GlueState.CurrentNamedObjectSaves's setter, which maps every entry through
/// Find.TreeNodeByTag - a real, populated WPF tree in production, but FakeFindManager here, which never
/// resolves a NamedObjectSave tag (see its own doc comment). Either way, an unresolvable entry produces
/// a null ITreeNode in the list passed to CurrentTreeNodes, and TakeSnapshot's
/// GetCurrentNamedObjectSavesFromSelection NREs dereferencing that null node's Tag - mid-snapshot, after
/// CurrentElement has already been recomputed (and wiped to null) but before CurrentNamedObjectSaves is.
/// That leaves GlueState with CurrentElement == null and a stale, non-null CurrentNamedObjectSave from
/// whatever was selected before - exactly the combination that crashes
/// MainPropertyGridPlugin.RefreshVariables -> NamedObjectSaveVariableDataGridItem.RefreshAddContextMenuEvents
/// (Container is null) the next time a variable is edited.
/// </summary>
public class GlueStateCurrentNamedObjectSavesTests
{
    public GlueStateCurrentNamedObjectSavesTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    [Fact]
    public void CurrentNamedObjectSaves_WithUnresolvableEntry_DoesNotThrow()
    {
        try
        {
            var unresolvable = new NamedObjectSave { InstanceName = "SomeObject" };

            Should.NotThrow(() =>
                GlueState.Self.CurrentNamedObjectSaves = new List<NamedObjectSave> { unresolvable });
        }
        finally
        {
            GlueState.Self.CurrentTreeNode = null;
        }
    }

    [Fact]
    public void CurrentNamedObjectSaves_WithUnresolvableEntry_ClearsSelectionInsteadOfCorruptingIt()
    {
        try
        {
            var unresolvable = new NamedObjectSave { InstanceName = "SomeObject" };

            GlueState.Self.CurrentNamedObjectSaves = new List<NamedObjectSave> { unresolvable };

            // An unresolvable selection should behave like "nothing selected", not leave CurrentElement
            // and CurrentNamedObjectSave in a mismatched (one null, one stale-non-null) state.
            GlueState.Self.CurrentNamedObjectSave.ShouldBeNull();
            GlueState.Self.CurrentElement.ShouldBeNull();
        }
        finally
        {
            GlueState.Self.CurrentTreeNode = null;
        }
    }
}
