using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.Frb2SettingsPlugin
{
    /// <summary>
    /// UI toggle for <see cref="SaveClasses.GlueProjectSave.GenerateCode"/>, the per-project opt-in
    /// for FlatRedBall 2 code generation (previously only settable by hand-editing the .gluj).
    /// </summary>
    [Export(typeof(PluginBase))]
    public class MainFrb2SettingsPlugin : EmbeddedPlugin
    {
        Frb2SettingsControl control;
        PluginTab tab;
        ToolStripMenuItem menuItem;
        readonly Frb2SettingsViewModel viewModel = new Frb2SettingsViewModel();

        bool isInitializing;

        public override void StartUp()
        {
            menuItem = base.AddMenuItemTo("FRB2 Code Generation", HandleMenuClicked, L.MenuIds.SettingsId);

            viewModel.PropertyChanged += HandleViewModelChanged;

            this.ReactToLoadedGlux += HandleLoadedGlux;
            this.ReactToUnloadedGlux += HandleUnloadedGlux;
        }

        private void HandleLoadedGlux()
        {
            menuItem.Visible = GlueState.Self.CurrentMainProject is Frb2Project;
        }

        private void HandleUnloadedGlux()
        {
            menuItem.Visible = false;
            tab?.Hide();
        }

        private void HandleMenuClicked(object sender, System.EventArgs e)
        {
            var glueProject = GlueState.Self.CurrentGlueProject;
            if (glueProject == null)
            {
                return;
            }

            if (control == null)
            {
                control = new Frb2SettingsControl();
                tab = base.CreateTab(control, "FRB2 Code Generation");
                control.DataContext = viewModel;
            }

            isInitializing = true;
            {
                viewModel.GenerateCode = glueProject.GenerateCode;
            }
            isInitializing = false;

            tab.Show();
            tab.Focus();
        }

        private void HandleViewModelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (isInitializing)
            {
                return;
            }

            var glueProject = GlueState.Self.CurrentGlueProject;
            if (glueProject == null)
            {
                return;
            }

            ApplyGenerateCodeSetting(glueProject, viewModel.GenerateCode);
        }

        /// <summary>
        /// Applies a new <see cref="GlueProjectSave.GenerateCode"/> value, saves the .gluj, and - when
        /// the setting just turned on - generates code immediately rather than requiring a project
        /// reload. Split out from <see cref="HandleViewModelChanged"/> so it can be driven directly by
        /// tests without going through the WPF checkbox/ViewModel plumbing.
        /// </summary>
        internal static void ApplyGenerateCodeSetting(GlueProjectSave glueProject, bool generateCode)
        {
            var wasEnabled = glueProject.GenerateCode;

            glueProject.GenerateCode = generateCode;

            GlueCommands.Self.GluxCommands.SaveGlujFile(Managers.TaskExecutionPreference.AddOrMoveToEnd);

            if (!wasEnabled && generateCode)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
            }
        }
    }
}
