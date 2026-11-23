using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using GlueUnitTests.Tasks;
using GlueUnitTests.TestSupport;
using OfficialPlugins.QuickActionPlugin.Views;
using Shouldly;
using Xunit;

namespace GlueUnitTests.QuickActionPlugin;

/// <summary>
/// The Quick Actions tab's "Run Wizard" button looked the wizard plugin up by its localized display name, so in
/// any language but English it found no plugin and did nothing.
/// </summary>
[Collection(nameof(TaskManagerSequentialCollection))]
public class RunWizardButtonTests : IDisposable
{
    private readonly IWizardCommands _originalWizardCommands;
    private readonly CultureInfo _originalCulture;

    public RunWizardButtonTests()
    {
        GlueTestBootstrap.EnsureInitialized();
        _originalWizardCommands = GlueCommands.Self.WizardCommands;
        _originalCulture = Localization.Texts.Culture;
    }

    public void Dispose()
    {
        GlueCommands.Self.WizardCommands = _originalWizardCommands;
        Localization.Texts.Culture = _originalCulture;
    }

    [StaTheory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    public void RunWizardButton_RunsTheWizard_InEveryLanguage(string culture)
    {
        Localization.Texts.Culture = new CultureInfo(culture);
        var wizard = new RecordingWizardCommands();
        GlueCommands.Self.WizardCommands = wizard;
        var view = new MainView();

        typeof(MainView)
            .GetMethod("RunWizard_Clicked", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(view, new object[] { view, new RoutedEventArgs() });

        wizard.RunCount.ShouldBe(1);
    }

    private class RecordingWizardCommands : IWizardCommands
    {
        public int RunCount { get; private set; }
        public void RunWizard() => RunCount++;
    }
}
