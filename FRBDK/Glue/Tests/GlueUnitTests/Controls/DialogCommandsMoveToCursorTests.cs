using System.Windows;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using GlueUnitTests.TestSupport;
using Shouldly;
using Xunit;

namespace GlueUnitTests.Controls;

// Covers issue #2357: a dialog shown before the main window's WPF panel exists (e.g. the "no usable .NET
// SDK" startup message) hit PresentationSource.FromVisual(null) in MoveToCursor, which threw from the
// dialog's Loaded handler and took the whole process down.
public class DialogCommandsMoveToCursorTests
{
    public DialogCommandsMoveToCursorTests()
    {
        GlueTestBootstrap.EnsureInitialized();
    }

    [StaFact]
    public void MoveToCursor_ShouldNotThrow_WhenMainWpfControlDoesNotExistYet()
    {
        var window = new Window();

        Should.NotThrow(() => new DialogCommands().MoveToCursor(window));

        window.WindowStartupLocation.ShouldBe(WindowStartupLocation.Manual);
    }
}
