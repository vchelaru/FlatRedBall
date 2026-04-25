// Assembly-level attribute wires Avalonia.Headless to the TestAppBuilder.
// AvaloniaTestApplicationAttribute lives in Avalonia.Headless (not Avalonia.Headless.XUnit).
using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(AnimationEditor.App.Tests.TestAppBuilder))]

namespace AnimationEditor.App.Tests;

/// <summary>
/// Factory consumed by [AvaloniaTestApplication] to build the headless app.
/// Uses the real <see cref="AnimationEditor.App.App"/> so that
/// <c>AvaloniaXamlLoader.Load(this)</c> runs in App.Initialize() and registers
/// the compiled XAML resources needed by <c>new MainWindow()</c>.
/// </summary>
public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<global::AnimationEditor.App.App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

