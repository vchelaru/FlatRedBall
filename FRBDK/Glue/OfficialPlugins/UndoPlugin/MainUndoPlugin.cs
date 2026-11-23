using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using OfficialPlugins.UndoPlugin.NewFolder;
using OfficialPlugins.UndoPlugin.Views;
using OfficialPlugins.Wizard.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.UndoPlugin
{
    [Export(typeof(PluginBase))]
    public class MainUndoPlugin : PluginBase
    {
        public override string FriendlyName => "Undo Plugin";

        public override Version Version => new Version(1,0);

        UndoDisplay view;
        ViewModels.UndoViewModel ViewModel;
        readonly UndoCommands undoCommands = new UndoCommands();

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            if (GlueCommands.Self.UndoCommands == undoCommands)
            {
                GlueCommands.Self.UndoCommands = null;
            }
            return true;
        }

        public override void StartUp()
        {
            GlueCommands.Self.UndoCommands = undoCommands;

            AssignEvents();
            ViewModel = new ViewModels.UndoViewModel();
            UndoManager.UndoViewModel = ViewModel;
        }

        public override void StartUpUi()
        {
            view = new UndoDisplay();
            this.CreateAndAddTab(view, Localization.Texts.Undo, TabLocation.Bottom);
            view.DataContext = ViewModel;
        }

        private void AssignEvents()
        {
            this.ReactToCtrlKey += UndoManager.ReactToCtrlKey;
            //this.ReactToChangedNamedObjectPropertyList += UndoManager.;
            this.ReactToNamedObjectChangedValueList += UndoManager.HandleNamedObjectValueChanges;

            this.ReactToLoadedGlux += () => ViewModel?.Undos.Clear();
        }
    }
}
