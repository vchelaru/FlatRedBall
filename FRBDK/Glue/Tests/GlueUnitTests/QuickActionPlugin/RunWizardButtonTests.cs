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
        WizardRecordingPlugin.EnsureRegistered();
        _originalWizardCommands = GlueCommands.Self.WizardCommands;
        _originalCulture = Localization.Texts.Culture;
        WizardRecordingPlugin.RunCount = 0;
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
        GlueCommands.Self.WizardCommands = new WizardRecordingPlugin();
        var view = new MainView();

        typeof(MainView)
            .GetMethod("RunWizard_Clicked", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(view, new object[] { view, new RoutedEventArgs() });

        WizardRecordingPlugin.RunCount.ShouldBe(1);
    }

    /// <summary>Stands in for the New Project Wizard plugin, under its real name, both as a registered plugin and as
    /// its commands.</summary>
    private class WizardRecordingPlugin : PluginBase, IWizardCommands
    {
        private static bool _registered;

        public static int RunCount { get; set; }

        public static void EnsureRegistered()
        {
            if (!_registered)
            {
                PluginManager.RegisterPlugin(new WizardRecordingPlugin());
                _registered = true;
            }
        }

        public override string FriendlyName => "New Project Wizard";
        public override Version Version => new Version(1, 0);

        public override void StartUp() { }

        public void RunWizard() => RunCount++;
    }
}
