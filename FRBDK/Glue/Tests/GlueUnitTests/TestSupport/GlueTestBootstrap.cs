using EditorObjects.IoC;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Services;

namespace GlueUnitTests.TestSupport;

/// <summary>
/// One-time, process-wide bootstrap that mirrors the handful of static/DI registrations Glue.exe's real
/// startup (Program.cs / MainGlueWindow.cs) performs before GlueCommands.Self/GlueState.Self are usable.
/// None of this is a fake or a mock - it is the same calls production makes, just run in-process for a
/// test host instead of a live WinForms app:
///  - <see cref="Builder"/> wires up the app's lightweight DI container (GluxCommands, ElementCommands,
///    etc. all resolve through it).
///  - The legacy <see cref="Container"/> (EditorObjects.IoC) is a second, older service locator that
///    several command classes (GenerateCodeCommands, VisualStudioProject, ...) still read from directly.
///  - <see cref="FlatRedBall.Glue.ProjectManager"/>.Initialize sets up CodeProjectHelper, used by any
///    code-file-adding path (e.g. AddScreen).
///  - <see cref="FileWatchManager"/>.Initialize sets up the self-save-suppression list that
///    IgnoreNextChangeOnFile writes to.
///
/// Any test that drives production code through GlueCommands.Self/GlueState.Self (rather than calling a
/// pure static method directly) needs this. Call <see cref="EnsureInitialized"/> once per test.
/// </summary>
internal static class GlueTestBootstrap
{
    static readonly object _lock = new();
    static bool _initialized;

    public static void EnsureInitialized()
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;

            if (Builder.App == null)
            {
                new Builder().Build();
            }

            Container.Set<IGlueCommands>(GlueCommands.Self);
            Container.Set<IGlueState>(GlueState.Self);

            if (FlatRedBall.Glue.ProjectManager.CodeProjectHelper == null)
            {
                FlatRedBall.Glue.ProjectManager.Initialize();
            }

            FileWatchManager.Initialize();
        }
    }
}
